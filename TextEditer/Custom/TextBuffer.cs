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
        private Dictionary<int, List<cPiece>> m_dicPiecesAtLine;


        public long m_nLength { get; private set; }
        public int m_nLineCount => m_listLineStartOffsets.Count; // m_blocks.GetLineCount();// m_listLineStartOffsets.Count;

        private string m_currentPath;
        private string m_snapshotPath;

        public ITextBuffer()
        {
            // m_blocks = new cLineIndexBlocks(32);
            m_dicPiecesAtLine = new Dictionary<int, List<cPiece>>();
            m_listLineStartOffsets = new List<long>();
        }

        public void LoadOriginal(string sPath)
        {
            ResetDocument();
            m_currentPath = sPath;

            byte[] data = File.ReadAllBytes(sPath);

            CreateSnapshotFromBytes(data);

            RebuildLineIndex();
        }
        private void CreateSnapshotFromBytes(byte[] data)
        {
            CloseSnapshotHandles();
            if (!string.IsNullOrEmpty(m_snapshotPath))
            {
                File.Delete(m_snapshotPath);
                m_snapshotPath = null;
            }

            m_snapshotPath = Path.GetTempFileName();
            File.WriteAllBytes(m_snapshotPath, data);

            m_fileStream = new FileStream(
                m_snapshotPath,
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.ReadWrite | FileShare.Delete
            );

            m_dwOriginalLen = data.LongLength;

            m_memoryMappedFile = MemoryMappedFile.CreateFromFile(
                m_fileStream,
                null,
                m_dwOriginalLen,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false
            );

            m_memoryMappedViewAccessor = m_memoryMappedFile.CreateViewAccessor(0, m_dwOriginalLen);

            m_dicPiecesAtLine.Clear();

            m_nLength = m_dwOriginalLen;
        }
        public void Save()
        {
            if (string.IsNullOrEmpty(m_currentPath))
                throw new InvalidOperationException("No current file path.");

            byte[] data = BuildFullText();

            string tmp = m_currentPath + ".tmp";
            File.WriteAllBytes(tmp, data);

            if (File.Exists(m_currentPath))
                File.Replace(tmp, m_currentPath, null);
            else
                File.Move(tmp, m_currentPath);

            CreateSnapshotFromBytes(data);
        }
        public void SaveAs(string newPath)
        {
            m_currentPath = newPath;
            Save();
        }

        public void InsertUtf8(int nLine, long dwPos, string sText)
        {
            if (dwPos < 0) dwPos = 0;
            if (dwPos > m_nLength) dwPos = m_nLength;

            byte[] bytes = Encoding.UTF8.GetBytes(sText);

            int lineBreakDeletedCount = 0;
            for (int i = 0; i < sText.Length; i++)
            {
                if (sText[i] == '\n')
                {
                    lineBreakDeletedCount++;
                }
            }
            cPieceToAdd newPiece = new cPieceToAdd(dwPos, sText, lineBreakDeletedCount);
            // 삽입 지점의 piece를 찾아 split 후 add piece 끼워넣기
            cPiece foundPiece = FindPieceAt(nLine, dwPos);
            bool bMasterAlive = true;
            bool bNewOneAlive = true;

            if (foundPiece != null && foundPiece.IsAdd)
            {
                foundPiece.Merge(newPiece, dwPos - foundPiece.Start, out bMasterAlive, out bNewOneAlive);
            }

            if (!bMasterAlive)
            {
                DeletePiece(nLine, foundPiece);
            }
            if(bNewOneAlive)
            {
                AddPiece(nLine, newPiece);
            }

            ShiftPiecesAfter(nLine, dwPos, bytes.Length);

            m_nLength += bytes.Length;
        }
        
        public void Delete(int nLine, long dwPos, int nByteCount, bool bLinesUpdate = false)
        {
            if (nByteCount <= 0 || m_nLength == 0) return;
            if (dwPos < 0) dwPos = 0;
            if (dwPos >= m_nLength) return;

            long dwEndPos = Math.Min(m_nLength, dwPos + nByteCount);

            // lineDeletedCount 계산.
            int lineBreakDeletedCount = 0;

            cPieceToRemove newPiece = new cPieceToRemove(dwPos, nByteCount, lineBreakDeletedCount);
            cPiece foundPiece = FindPieceAt(nLine, dwPos);
            bool bMasterAlive = true;
            bool bNewOneAlive = true;

            if(foundPiece != null)
            {
                foundPiece.Merge(newPiece, dwPos - foundPiece.Start, out bMasterAlive, out bNewOneAlive);
            }

            if (!bMasterAlive)
            {
                DeletePiece(nLine, foundPiece);
            }
            if (bNewOneAlive)
            {
                AddPiece(nLine, newPiece);
            }

            ShiftPiecesAfter(nLine, dwPos, -nByteCount);

            m_nLength -= nByteCount;
        }
        private cPiece FindPieceAt(int lineIndex, long dwPos)
        {
            long lineStartBytes = GetLineStartByteOffset(lineIndex);
            int index = 0;
            if (!m_dicPiecesAtLine.ContainsKey(lineIndex))
            {
                return null;
            }
            foreach (cPiece piece in m_dicPiecesAtLine[lineIndex])
            {
                int nLength;
                if (piece.IsAdd)
                {
                    cPieceToAdd pieceToAdd = (cPieceToAdd)piece;
                    nLength = pieceToAdd.Content.Length;
                }
                else
                {
                    cPieceToRemove pieceToRemove = (cPieceToRemove)piece;
                    nLength = pieceToRemove.CountToRemove;
                }
                if (piece.Start <= dwPos && dwPos < piece.Start + nLength)
                {
                    return piece;
                }
                index++;
            }

            return null;
        }
        private void DeletePiece(int nLine, cPiece piece)
        {
            m_dicPiecesAtLine[nLine].Remove(piece);
        }
        private void AddPiece(int nLine, cPiece piece)
        {
            if (!m_dicPiecesAtLine.ContainsKey(nLine))
            {
                m_dicPiecesAtLine[nLine] = new List<cPiece>();
            }
            m_dicPiecesAtLine[nLine].Add(piece);
        }
        private void ShiftPiecesAfter(int fromLine, long fromOffset, long delta)
        {
            // fromLine 이후의 모든 라인
            foreach (var kv in m_dicPiecesAtLine)
            {
                int line = kv.Key;
                if (line < fromLine)
                    continue;

                foreach (var piece in kv.Value)
                {
                    if (line == fromLine)
                    {
                        if (piece.Start > fromOffset)
                            piece.Start += delta;
                    }
                    else
                    {
                        piece.Start += delta;
                    }
                }
            }
        }
        public int GetSumOffsetTillLine(int originalLine, out int afterLine)
        {
            int addOffset = 0;
            foreach (var item in m_dicPiecesAtLine)
            {
                if (item.Key < originalLine)
                {
                    foreach (cPiece piece in item.Value)
                    {
                        if (piece.IsAdd)
                        {
                            cPieceToAdd pieceToAdd = (cPieceToAdd)piece;
                            addOffset += pieceToAdd.Content.Length;
                            if (piece.LineBreaks > 0)
                            {
                                originalLine -= piece.LineBreaks;
                            }
                        }
                        else
                        {
                            cPieceToRemove pieceToRemove = (cPieceToRemove)piece;
                            addOffset -= pieceToRemove.CountToRemove;
                            if (piece.LineBreaks > 0)
                            {
                                originalLine += piece.LineBreaks;
                            }
                        }
                    }
                }
            }
            afterLine = originalLine;
            return addOffset;
        }
        public long GetLineStartByteOffset(int line)
        {
            int afterLine;
            int addOffset = GetSumOffsetTillLine(line, out afterLine);
            if (afterLine < 0) return 0;

            if (afterLine >= m_listLineStartOffsets.Count)
                return m_nLength + addOffset;

            return m_listLineStartOffsets[afterLine] + addOffset;
        }
        public string GetLineUtf8(int nLine)
        {
            long dwStart = GetLineStartByteOffset(nLine);
            long dwEnd = (nLine + 1 < m_nLineCount)
                ? GetLineStartByteOffset(nLine + 1)
                : m_nLength;

            int nLen = (int)(dwEnd - dwStart);
            if (nLen <= 0) return "";

            return ReadRangeUtf8(nLine, dwStart, nLen).TrimEnd('\r', '\n');
        }
        public string ReadRangeUtf8(int nLine, long dwPos, int nByteCount)
        {
            if (nByteCount <= 0 || dwPos < 0 || dwPos >= m_nLength) return string.Empty;
            long dwEndPos = Math.Min(m_nLength, dwPos + nByteCount);
            int sumOffsets = GetSumOffsetTillLine(nLine, out int afterLine);

            using (MemoryStream ms = new MemoryStream(nByteCount))
            {
                long docPos = dwPos;   // 문서 기준
                long srcPos = dwPos - sumOffsets;   // 원본 기준

                if (m_dicPiecesAtLine.TryGetValue(nLine, out List<cPiece> pieces))
                {
                    foreach (cPiece piece in pieces.OrderBy(p => p.Start))
                    {
                        if (piece.Start >= dwEndPos)
                            break;

                        // piece 전까지의 원본 구간
                        if (docPos < piece.Start)
                        {
                            int toRead = (int)Math.Min(
                                piece.Start - docPos,
                                dwEndPos - docPos
                            );

                            byte[] buf = new byte[toRead];
                            m_memoryMappedViewAccessor.ReadArray(srcPos, buf, 0, toRead);
                            ms.Write(buf, 0, toRead);

                            docPos += toRead;
                            srcPos += toRead;
                        }

                        if (docPos >= dwEndPos)
                            break;

                        // Add Piece
                        if (piece.IsAdd)
                        {
                            cPieceToAdd add = piece as cPieceToAdd;
                            byte[] addBytes = Encoding.UTF8.GetBytes(add.Content);

                            int writable = (int)Math.Min(
                                addBytes.Length,
                                dwEndPos - docPos
                            );

                            ms.Write(addBytes, 0, writable);
                            docPos += writable;
                        }
                        // Remove Piece
                        else
                        {
                            cPieceToRemove rem = piece as cPieceToRemove;

                            int skip = (int)Math.Min(
                                rem.CountToRemove,
                                dwEndPos - docPos
                            );

                            srcPos += skip;
                            docPos += skip;
                        }
                    }
                }

                // 남은 원본
                if (docPos < dwEndPos)
                {
                    int remain = (int)(dwEndPos - docPos);
                    byte[] buf = new byte[remain];
                    m_memoryMappedViewAccessor.ReadArray(srcPos, buf, 0, remain);
                    ms.Write(buf, 0, remain);
                }

                return Encoding.UTF8.GetString(ms.ToArray());
            }
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


        public long GetLineEndByteOffset(int line)
        {
            if (line + 1 < m_nLineCount)
                return GetLineStartByteOffset(line + 1);
            return m_nLength;
        }

        public byte[] ReadRangeBytes(int nLine, long pos, int byteCount)
        {
            if (byteCount <= 0 || pos < 0 || pos >= m_nLength)
                return Array.Empty<byte>();

            long endPos = Math.Min(m_nLength, pos + byteCount);

            using (MemoryStream ms = new MemoryStream(byteCount))
            {
                long docPos = pos; // 문서 기준 위치
                long srcPos = pos; // 원본(MMF) 기준 위치

                List<cPiece> pieces = null;
                m_dicPiecesAtLine.TryGetValue(nLine, out pieces);

                if (pieces != null && pieces.Count > 0)
                {
                    foreach (var piece in pieces.OrderBy(p => p.Start))
                    {
                        if (piece.Start >= endPos)
                            break;

                        // piece 시작 전의 원본 구간
                        if (docPos < piece.Start)
                        {
                            int toRead = (int)Math.Min(
                                piece.Start - docPos,
                                endPos - docPos
                            );

                            byte[] buf = new byte[toRead];
                            m_memoryMappedViewAccessor.ReadArray(srcPos, buf, 0, toRead);
                            ms.Write(buf, 0, toRead);

                            docPos += toRead;
                            srcPos += toRead;
                        }

                        if (docPos >= endPos)
                            break;

                        // Add Piece
                        if (piece.IsAdd)
                        {
                            var add = (cPieceToAdd)piece;
                            byte[] addBytes = Encoding.UTF8.GetBytes(add.Content);

                            int writable = (int)Math.Min(
                                addBytes.Length,
                                endPos - docPos
                            );

                            ms.Write(addBytes, 0, writable);
                            docPos += writable;
                        }
                        // Remove Piece
                        else
                        {
                            var rem = (cPieceToRemove)piece;

                            int skip = (int)Math.Min(
                                rem.CountToRemove,
                                endPos - docPos
                            );

                            srcPos += skip;
                            docPos += skip;
                        }
                    }
                }

                // 남은 원본 영역
                if (docPos < endPos)
                {
                    int remain = (int)(endPos - docPos);
                    byte[] buf = new byte[remain];
                    m_memoryMappedViewAccessor.ReadArray(srcPos, buf, 0, remain);
                    ms.Write(buf, 0, remain);
                }

                return ms.ToArray();
            }
        }
        public int GetPreviousCharByteLength(int nLine, long byteOffset)
        {
            if (byteOffset <= 0)
                return 0;

            int maxLookback = (int)Math.Min(4, byteOffset);
            byte[] buf = ReadRangeBytes(nLine, byteOffset - maxLookback, maxLookback);

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

        public int GetNextCharByteLength(int nLine, long byteOffset)
        {
            if (byteOffset >= m_nLength)
                return 0;

            // 최대 4바이트면 UTF-8 문자 하나 충분
            byte[] buf = ReadRangeBytes(nLine, byteOffset, 4);
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

            BuildLineIndex();
        }
        public void RebuildLineIndex()
        {
            m_listLineStartOffsets = BuildLineIndex();
        }
        private List<long> BuildLineIndex()
        {
            List<long> lineOffsets = new List<long>(1024);

            lineOffsets.Add(0);

            const int BUFFER_SIZE = 64 * 1024;
            byte[] buffer = new byte[BUFFER_SIZE];

            long filePos = 0;
            long length = m_nLength;

            while (filePos < length)
            {
                int toRead = (int)Math.Min(BUFFER_SIZE, length - filePos);

                m_memoryMappedViewAccessor.ReadArray(
                    filePos,
                    buffer,
                    0,
                    toRead
                );

                for (int i = 0; i < toRead; i++)
                {
                    byte b = buffer[i];

                    if (b == (byte)'\n')
                    {
                        // 다음 줄 시작 위치
                        long nextLineOffset = filePos + i + 1;
                        if (nextLineOffset < length)
                        {
                            lineOffsets.Add(nextLineOffset);
                        }
                    }
                }

                filePos += toRead;
            }

            return lineOffsets;
        }        

        private void ResetDocument()
        {
            m_memoryMappedViewAccessor?.Dispose(); m_memoryMappedViewAccessor = null;
            m_memoryMappedFile?.Dispose(); m_memoryMappedFile = null;
            m_fileStream?.Dispose(); m_fileStream = null;

            m_nLength = 0;
        }
        private void CloseSnapshotHandles()
        {
            m_memoryMappedViewAccessor?.Dispose(); m_memoryMappedViewAccessor = null;
            m_memoryMappedFile?.Dispose(); m_memoryMappedFile = null;
            m_fileStream?.Dispose(); m_fileStream = null;

            m_dwOriginalLen = 0;
        }
        public void Dispose()
        {
            m_memoryMappedViewAccessor?.Dispose(); m_memoryMappedViewAccessor = null;
            m_memoryMappedFile?.Dispose(); m_memoryMappedFile = null;
            m_fileStream?.Dispose(); m_fileStream = null;

            m_nLength = 0;
            m_dwOriginalLen = 0;
        }
    }
}
