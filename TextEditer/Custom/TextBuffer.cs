using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditer
{
    public class ITextBuffer : IDisposable
    {
        private FileStream m_fileStream;
        private MemoryMappedFile m_memoryMappedFile;
        private MemoryMappedViewAccessor m_memoryMappedViewAccessor;
        private long m_dwOriginalLen;
        
        List<long> m_listLineStartOffsets;

        private readonly MemoryStream m_memoryStream_ofAddPiece = new MemoryStream(1024 * 1024);
        private readonly List<cPiece> m_listPieces = new List<cPiece>(1024);

        public long m_nLength { get; private set; }
        public int m_nLineCount => m_listLineStartOffsets.Count;

        public void LoadOriginal(string sPath)
        {
            ResetDocument();

            m_fileStream = new FileStream(sPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            m_dwOriginalLen = m_fileStream.Length;

            m_memoryMappedFile = MemoryMappedFile.CreateFromFile(
                m_fileStream, null, 0, MemoryMappedFileAccess.Read,
                HandleInheritability.None, false);

            m_memoryMappedViewAccessor = m_memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            m_listPieces.Clear();
            m_listPieces.Add(new cPiece(PieceSource.Original, 0, checked((int)m_dwOriginalLen))); // 원본 1조각
            m_nLength = m_dwOriginalLen;

            m_listLineStartOffsets = BuildLineIndex();
        }

        public void InsertUtf8(int nLine, long dwPos, string sText)
        {
            if (dwPos < 0) dwPos = 0;
            if (dwPos > m_nLength) dwPos = m_nLength;

            byte[] bytes = Encoding.UTF8.GetBytes(sText);

            long dwAddStart = m_memoryStream_ofAddPiece.Length;
            m_memoryStream_ofAddPiece.Write(bytes, 0, bytes.Length);

            // 삽입 지점의 piece를 찾아 split 후 add piece 끼워넣기
            FindPieceAt(dwPos, out int pieceIndex, out int innerOffset);

            cPiece target = m_listPieces[pieceIndex];
            int beforeLen = innerOffset;
            int afterLen = target.Length - innerOffset;

            List<cPiece> newPieces = new List<cPiece>(3);

            if (beforeLen > 0)
                newPieces.Add(new cPiece(target.Source, target.Start, beforeLen));

            newPieces.Add(new cPiece(PieceSource.Add, dwAddStart, bytes.Length));

            if (afterLen > 0)
                newPieces.Add(new cPiece(target.Source, target.Start + innerOffset, afterLen));

            m_listPieces.RemoveAt(pieceIndex);
            m_listPieces.InsertRange(pieceIndex, newPieces);

            m_nLength += bytes.Length;
            MergeNeighborsAround(pieceIndex);

            RealignLineOffset(nLine, bytes.Length);
            // m_listLineStartOffsets = BuildLineIndex();
        }

        public void Delete(int nLine, long dwPos, int nByteCount, bool bLinesUpdate = false)
        {
            if (nByteCount <= 0 || m_nLength == 0) return;
            if (dwPos < 0) dwPos = 0;
            if (dwPos >= m_nLength) return;

            long dwEndPos = Math.Min(m_nLength, dwPos + nByteCount);

            // [pos, endPos) 범위를 piece 단위로 제거
            SplitAt(dwPos, out int startIdx);
            SplitAt(dwEndPos, out int endIdx); // endPos 위치도 split

            // startIdx ~ endIdx-1 조각 제거
            int removeCount = endIdx - startIdx;
            for (int i = 0; i < removeCount; i++)
                m_listPieces.RemoveAt(startIdx);

            m_nLength -= (dwEndPos - dwPos);

            // 이웃 병합
            MergeNeighborsAround(Math.Max(0, startIdx - 1));

            if (bLinesUpdate)
            {
                m_listLineStartOffsets = BuildLineIndex();
            }
            else
            {
                RealignLineOffset(nLine, -removeCount);
            }
            // m_listLineStartOffsets = BuildLineIndex();
        }

        public string ReadRangeUtf8(long dwPos, int nByteCount)
        {
            if (nByteCount <= 0 || dwPos < 0 || dwPos >= m_nLength) return string.Empty;
            long dwEndPos = Math.Min(m_nLength, dwPos + nByteCount);

            using (MemoryStream memoryStream = new MemoryStream(nByteCount))
            {
                long dwCurrent = 0;

                for (int i = 0; i < m_listPieces.Count; i++)
                {
                    cPiece p = m_listPieces[i];
                    long dwNext = dwCurrent + p.Length;

                    if (dwNext <= dwPos) { dwCurrent = dwNext; continue; }
                    if (dwCurrent >= dwEndPos) break;

                    long segStartInDoc = Math.Max(dwCurrent, dwPos);
                    long segEndInDoc = Math.Min(dwNext, dwEndPos);

                    int take = (int)(segEndInDoc - segStartInDoc);
                    int skip = (int)(segStartInDoc - dwCurrent);

                    WritePieceBytes(memoryStream, p, skip, take);
                    dwCurrent = dwNext;
                }

                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        public string GetLineUtf8(int nLine)
        {
            long dwStart = GetLineStartByteOffset(nLine);
            long dwEnd = (nLine + 1 < m_nLineCount)
                ? GetLineStartByteOffset(nLine + 1)
                : m_nLength;

            int nLen = (int)(dwEnd - dwStart);
            if (nLen <= 0) return "";

            return ReadRangeUtf8(dwStart, nLen).TrimEnd('\r', '\n');
        }
        public int GetLineLength(int nIndex)
        {
            return GetLineUtf8(nIndex).Length;
        }
        public long GetIndex(int nLineIndex, int nColumn)
        {
            long dwLineStart = GetLineStartByteOffset(nLineIndex);
            if (nColumn <= 0)
                return dwLineStart;

            string sLine = GetLineUtf8(nLineIndex);

            if (nColumn >= sLine.Length)
                return dwLineStart + Encoding.UTF8.GetByteCount(sLine);

            string sSubLine = sLine.Substring(0, nColumn);
            return dwLineStart + Encoding.UTF8.GetByteCount(sSubLine);
        }
        public long GetLineStartByteOffset(int line)
        {
            if (line < 0) return 0;
            if (line >= m_listLineStartOffsets.Count)
                return m_nLength;

            return m_listLineStartOffsets[line];
        }
        public long GetLineEndByteOffset(int line)
        {
            if (line + 1 < m_nLineCount)
                return GetLineStartByteOffset(line + 1);
            return m_nLength;
        }
        public byte[] ReadRangeBytes(long pos, int byteCount)
        {
            if (byteCount <= 0 || pos < 0 || pos >= m_nLength)
                return Array.Empty<byte>();

            long endPos = Math.Min(m_nLength, pos + byteCount);

            using (MemoryStream ms = new MemoryStream(byteCount))
            {
                long cur = 0;

                foreach (var p in m_listPieces)
                {
                    long next = cur + p.Length;

                    if (next <= pos) { cur = next; continue; }
                    if (cur >= endPos) break;

                    long segStartInDoc = Math.Max(cur, pos);
                    long segEndInDoc = Math.Min(next, endPos);

                    int take = (int)(segEndInDoc - segStartInDoc);
                    int skip = (int)(segStartInDoc - cur);

                    WritePieceBytes(ms, p, skip, take);
                    cur = next;
                }

                return ms.ToArray();
            }
        }
        public int GetPreviousCharByteLength(long byteOffset)
        {
            if (byteOffset <= 0)
                return 0;

            int maxLookback = (int)Math.Min(4, byteOffset);
            byte[] buf = ReadRangeBytes(byteOffset - maxLookback, maxLookback);

            for (int i = buf.Length - 1; i >= 0; i--)
            {
                byte b = buf[i];

                // UTF-8 시작 바이트 판별
                if ((b & 0b1000_0000) == 0)          
                    return buf.Length - i;

                if ((b & 0b1100_0000) == 0b1100_0000)
                    return buf.Length - i;
            }
            
            return 1;
        }

        public int GetNextCharByteLength(long byteOffset)
        {
            if (byteOffset >= m_nLength)
                return 0;

            // 최대 4바이트면 UTF-8 문자 하나 충분
            byte[] buf = ReadRangeBytes(byteOffset, 4);
            if (buf.Length == 0)
                return 0;

            byte b = buf[0];

            // ASCII (0xxxxxxx)
            if ((b & 0b1000_0000) == 0)
                return 1;

            // 110xxxxx → 2바이트
            if ((b & 0b1110_0000) == 0b1100_0000)
                return Math.Min(2, buf.Length);

            // 1110xxxx → 3바이트
            if ((b & 0b1111_0000) == 0b1110_0000)
                return Math.Min(3, buf.Length);

            // 11110xxx → 4바이트
            if ((b & 0b1111_1000) == 0b1111_0000)
                return Math.Min(4, buf.Length);
            
            return 1;
        }
        public byte[] BuildFullText()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                foreach (var piece in m_listPieces)
                {
                    if (piece.Length <= 0)
                        continue;

                    if (piece.Source == PieceSource.Original)
                    {
                        byte[] buffer = new byte[piece.Length];
                        m_memoryMappedViewAccessor.ReadArray(
                            piece.Start,
                            buffer,
                            0,
                            piece.Length
                        );

                        ms.Write(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        m_memoryStream_ofAddPiece.Position = piece.Start;

                        byte[] buffer = new byte[piece.Length];
                        m_memoryStream_ofAddPiece.Read(buffer, 0, buffer.Length);

                        ms.Write(buffer, 0, buffer.Length);
                    }
                }

                return ms.ToArray();
            }
        }
        
        public void Reset()
        {
            m_memoryMappedViewAccessor?.Dispose();
            m_memoryMappedFile?.Dispose();
            m_fileStream?.Dispose();

            m_memoryMappedViewAccessor = null;
            m_memoryMappedFile = null;
            m_fileStream = null;
        }

        public void ResetWithNewContent(string sPath, byte[] fullText)
        {
            m_memoryMappedViewAccessor?.Dispose();
            m_memoryMappedFile?.Dispose();
            m_fileStream?.Dispose();

            m_fileStream = new FileStream(
                sPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.ReadWrite
            );

            m_fileStream.Write(fullText, 0, fullText.Length);
            m_fileStream.Flush();

            m_dwOriginalLen = fullText.Length;

            m_memoryMappedFile = MemoryMappedFile.CreateFromFile(
                m_fileStream,
                null,
                m_dwOriginalLen,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false
            );

            m_memoryMappedViewAccessor =
                m_memoryMappedFile.CreateViewAccessor(0, m_dwOriginalLen);

            m_memoryStream_ofAddPiece.SetLength(0);

            m_listPieces.Clear();
            m_listPieces.Add(new cPiece(PieceSource.Original, 0, (int)m_dwOriginalLen));

            BuildLineIndex();
        }
        public void RebuildLineIndex()
        {
            m_listLineStartOffsets = BuildLineIndex();
        }
        private List<long> BuildLineIndex()
        {
            List<long> list = new List<long>(1024);
            list.Add(0);

            long docOffset = 0;

            foreach (cPiece piece in m_listPieces)
            {
                if (piece.Source == PieceSource.Original)
                {
                    byte[] buf = new byte[8192];
                    long remain = piece.Length;
                    long off = piece.Start;

                    while (remain > 0)
                    {
                        int toRead = (int)Math.Min(buf.Length, remain);
                        m_memoryMappedViewAccessor.ReadArray(off, buf, 0, toRead);

                        for (int i = 0; i < toRead; i++)
                        {
                            docOffset++;
                            if (buf[i] == (byte)'\n')
                                list.Add(docOffset);
                        }

                        off += toRead;
                        remain -= toRead;
                    }
                }
                else
                {
                    m_memoryStream_ofAddPiece.Position = piece.Start;
                    byte[] buf = new byte[8192];
                    int remain = piece.Length;

                    while (remain > 0)
                    {
                        int read = m_memoryStream_ofAddPiece.Read(buf, 0, Math.Min(buf.Length, remain));
                        if (read <= 0)
                            break;

                        for (int i = 0; i < read; i++)
                        {
                            docOffset++;
                            if (buf[i] == (byte)'\n')
                                list.Add(docOffset);
                        }
                        remain -= read;
                    }
                }
            }

            return list;
        }
        private void RealignLineOffset(int line, int addedBytes)
        {
            for(int i = line + 1; i < m_listLineStartOffsets.Count; i++)
            {
                m_listLineStartOffsets[i] += addedBytes;
            }
        }
        private void FindPieceAt(long pos, out int pieceIndex, out int innerOffset)
        {
            // pos는 문서 전체(바이트) 오프셋
            long cur = 0;
            for (int i = 0; i < m_listPieces.Count; i++)
            {
                var p = m_listPieces[i];
                long next = cur + p.Length;
                if (pos < next)
                {
                    pieceIndex = i;
                    innerOffset = (int)(pos - cur);
                    return;
                }
                cur = next;
            }

            // pos == Length (끝)인 경우: 마지막 piece 끝으로 처리
            pieceIndex = m_listPieces.Count - 1;
            innerOffset = m_listPieces[pieceIndex].Length;
        }

        private void SplitAt(long pos, out int pieceIndexAfterSplit)
        {
            // pos 지점이 piece 중간이면 split해서 경계를 만든다.
            FindPieceAt(pos, out int idx, out int inner);
            var p = m_listPieces[idx];

            if (inner == 0)
            {
                pieceIndexAfterSplit = idx;
                return;
            }
            if (inner == p.Length)
            {
                pieceIndexAfterSplit = idx + 1;
                return;
            }

            var left = new cPiece(p.Source, p.Start, inner);
            var right = new cPiece(p.Source, p.Start + inner, p.Length - inner);

            m_listPieces[idx] = left;
            m_listPieces.Insert(idx + 1, right);

            pieceIndexAfterSplit = idx + 1;
        }

        private void WritePieceBytes(Stream dst, cPiece p, int skip, int take)
        {
            if (take <= 0) return;

            if (p.Source == PieceSource.Original)
            {
                if (m_memoryMappedViewAccessor == null)
                    return;
                byte[] buf = new byte[take];
                m_memoryMappedViewAccessor.ReadArray(p.Start + skip, buf, 0, take);
                dst.Write(buf, 0, take);
            }
            else
            {
                long start = p.Start + skip;
                m_memoryStream_ofAddPiece.Position = start;
                byte[] buf = new byte[take];
                int read = m_memoryStream_ofAddPiece.Read(buf, 0, take);
                dst.Write(buf, 0, read);
            }
        }

        private void MergeNeighborsAround(int index)
        {
            // 인접 조각이 같은 소스이고, 오프셋이 연속이면 합친다
            int i = Math.Max(0, Math.Min(index, m_listPieces.Count - 1));

            // 왼쪽으로 병합
            while (i - 1 >= 0)
            {
                var a = m_listPieces[i - 1];
                var b = m_listPieces[i];

                if (a.Source == b.Source && a.Start + a.Length == b.Start)
                {
                    m_listPieces[i - 1] = new cPiece(a.Source, a.Start, a.Length + b.Length);
                    m_listPieces.RemoveAt(i);
                    i--;
                }
                else break;
            }

            // 오른쪽으로 병합
            while (i + 1 < m_listPieces.Count)
            {
                var a = m_listPieces[i];
                var b = m_listPieces[i + 1];

                if (a.Source == b.Source && a.Start + a.Length == b.Start)
                {
                    m_listPieces[i] = new cPiece(a.Source, a.Start, a.Length + b.Length);
                    m_listPieces.RemoveAt(i + 1);
                }
                else break;
            }
        }
        private void ResetDocument()
        {
            m_memoryMappedViewAccessor?.Dispose(); m_memoryMappedViewAccessor = null;
            m_memoryMappedFile?.Dispose(); m_memoryMappedFile = null;
            m_fileStream?.Dispose(); m_fileStream = null;

            m_memoryStream_ofAddPiece.SetLength(0);   // ⭐ Dispose ❌, 초기화만
            m_listPieces.Clear();

            m_nLength = 0;
        }
        public void Dispose()
        {
            m_memoryMappedViewAccessor?.Dispose(); m_memoryMappedViewAccessor = null;
            m_memoryMappedFile?.Dispose(); m_memoryMappedFile = null;
            m_fileStream?.Dispose(); m_fileStream = null;

            m_memoryStream_ofAddPiece?.Dispose();
            m_listPieces.Clear();
            m_nLength = 0;
            m_dwOriginalLen = 0;
        }
    }
}
