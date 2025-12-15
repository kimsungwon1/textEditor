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
        private FileStream _fs;
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _acc;
        private long _originalLen;
        
        List<long> _lineStartOffsets;

        private readonly MemoryStream _add = new MemoryStream(1024 * 1024);
        private readonly List<cPiece> _pieces = new List<cPiece>(1024);

        public long Length { get; private set; }
        public int LineCount => _lineStartOffsets.Count;

        public void LoadOriginal(string path)
        {
            ResetDocument();

            _fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            _originalLen = _fs.Length;

            _mmf = MemoryMappedFile.CreateFromFile(
                _fs, null, 0, MemoryMappedFileAccess.Read,
                HandleInheritability.None, false);

            _acc = _mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

            _pieces.Clear();
            _pieces.Add(new cPiece(PieceSource.Original, 0, checked((int)_originalLen))); // 원본 1조각
            Length = _originalLen;

            _lineStartOffsets = BuildLineIndex();
        }

        public void InsertUtf8(long pos, string text)
        {
            if (pos < 0) pos = 0;
            if (pos > Length) pos = Length;

            byte[] bytes = Encoding.UTF8.GetBytes(text);

            long addStart = _add.Length;
            _add.Write(bytes, 0, bytes.Length);

            // 삽입 지점의 piece를 찾아 split 후 add piece 끼워넣기
            FindPieceAt(pos, out int pieceIndex, out int innerOffset);

            var target = _pieces[pieceIndex];
            var beforeLen = innerOffset;
            var afterLen = target.Length - innerOffset;

            var newPieces = new List<cPiece>(3);

            if (beforeLen > 0)
                newPieces.Add(new cPiece(target.Source, target.Start, beforeLen));

            newPieces.Add(new cPiece(PieceSource.Add, addStart, bytes.Length));

            if (afterLen > 0)
                newPieces.Add(new cPiece(target.Source, target.Start + innerOffset, afterLen));

            _pieces.RemoveAt(pieceIndex);
            _pieces.InsertRange(pieceIndex, newPieces);

            Length += bytes.Length;
            MergeNeighborsAround(pieceIndex);

            _lineStartOffsets = BuildLineIndex();
        }

        public void Delete(long pos, int byteCount)
        {
            if (byteCount <= 0 || Length == 0) return;
            if (pos < 0) pos = 0;
            if (pos >= Length) return;

            long endPos = Math.Min(Length, pos + byteCount);

            // [pos, endPos) 범위를 piece 단위로 제거
            SplitAt(pos, out int startIdx);
            SplitAt(endPos, out int endIdx); // endPos 위치도 split

            // startIdx ~ endIdx-1 조각 제거
            int removeCount = endIdx - startIdx;
            for (int i = 0; i < removeCount; i++)
                _pieces.RemoveAt(startIdx);

            Length -= (endPos - pos);

            // 이웃 병합
            MergeNeighborsAround(Math.Max(0, startIdx - 1));

            _lineStartOffsets = BuildLineIndex();
        }

        public string ReadRangeUtf8(long pos, int byteCount)
        {
            if (byteCount <= 0 || pos < 0 || pos >= Length) return string.Empty;
            long endPos = Math.Min(Length, pos + byteCount);

            using (MemoryStream ms = new MemoryStream(byteCount))
            {
                long cur = 0;

                for (int i = 0; i < _pieces.Count; i++)
                {
                    var p = _pieces[i];
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

                return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
            }
        }

        public string GetLineUtf8(int line)
        {
            long start = GetLineStartByteOffset(line);
            long end = (line + 1 < LineCount)
                ? GetLineStartByteOffset(line + 1)
                : Length;

            int len = (int)(end - start);
            if (len <= 0) return "";

            return ReadRangeUtf8(start, len).TrimEnd('\r', '\n');
        }
        public int GetLineLength(int index)
        {
            return GetLineUtf8(index).Length;
        }
        public long GetIndex(int lineIndex, int column)
        {
            long lineStart = GetLineStartByteOffset(lineIndex);
            if (column <= 0)
                return lineStart;

            string line = GetLineUtf8(lineIndex);

            if (column >= line.Length)
                return lineStart + Encoding.UTF8.GetByteCount(line);

            string sub = line.Substring(0, column);
            return lineStart + Encoding.UTF8.GetByteCount(sub);
        }
        public long GetLineStartByteOffset(int line)
        {
            if (line < 0) return 0;
            if (line >= _lineStartOffsets.Count)
                return Length;

            return _lineStartOffsets[line];
        }
        public byte[] ReadRangeBytes(long pos, int byteCount)
        {
            if (byteCount <= 0 || pos < 0 || pos >= Length)
                return Array.Empty<byte>();

            long endPos = Math.Min(Length, pos + byteCount);

            using (MemoryStream ms = new MemoryStream(byteCount))
            {
                long cur = 0;

                foreach (var p in _pieces)
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

            // fallback (이론상 거의 안 옴)
            return 1;
        }

        public int GetNextCharByteLength(long byteOffset)
        {
            if (byteOffset >= Length)
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

            // fallback (깨진 UTF-8)
            return 1;
        }
        private List<long> BuildLineIndex()
        {
            var list = new List<long>(1024);
            list.Add(0);

            long docOffset = 0;

            foreach (var piece in _pieces)
            {
                if (piece.Source == PieceSource.Original)
                {
                    byte[] buf = new byte[8192];
                    long remain = piece.Length;
                    long off = piece.Start;

                    while (remain > 0)
                    {
                        int toRead = (int)Math.Min(buf.Length, remain);
                        _acc.ReadArray(off, buf, 0, toRead);

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
                    _add.Position = piece.Start;
                    byte[] buf = new byte[8192];
                    int remain = piece.Length;

                    while (remain > 0)
                    {
                        int read = _add.Read(buf, 0, Math.Min(buf.Length, remain));
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
        private void FindPieceAt(long pos, out int pieceIndex, out int innerOffset)
        {
            // pos는 문서 전체(바이트) 오프셋
            long cur = 0;
            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i];
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
            pieceIndex = _pieces.Count - 1;
            innerOffset = _pieces[pieceIndex].Length;
        }

        private void SplitAt(long pos, out int pieceIndexAfterSplit)
        {
            // pos 지점이 piece 중간이면 split해서 경계를 만든다.
            FindPieceAt(pos, out int idx, out int inner);
            var p = _pieces[idx];

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

            _pieces[idx] = left;
            _pieces.Insert(idx + 1, right);

            pieceIndexAfterSplit = idx + 1;
        }

        private void WritePieceBytes(Stream dst, cPiece p, int skip, int take)
        {
            if (take <= 0) return;

            if (p.Source == PieceSource.Original)
            {
                if (_acc == null) throw new InvalidOperationException("Original accessor is null.");
                byte[] buf = new byte[take];
                _acc.ReadArray(p.Start + skip, buf, 0, take);
                dst.Write(buf, 0, take);
            }
            else
            {
                long start = p.Start + skip;
                _add.Position = start;
                byte[] buf = new byte[take];
                int read = _add.Read(buf, 0, take);
                dst.Write(buf, 0, read);
            }
        }

        private void MergeNeighborsAround(int index)
        {
            // 인접 조각이 같은 소스이고, 오프셋이 연속이면 합친다
            int i = Math.Max(0, Math.Min(index, _pieces.Count - 1));

            // 왼쪽으로 병합
            while (i - 1 >= 0)
            {
                var a = _pieces[i - 1];
                var b = _pieces[i];

                if (a.Source == b.Source && a.Start + a.Length == b.Start)
                {
                    _pieces[i - 1] = new cPiece(a.Source, a.Start, a.Length + b.Length);
                    _pieces.RemoveAt(i);
                    i--;
                }
                else break;
            }

            // 오른쪽으로 병합
            while (i + 1 < _pieces.Count)
            {
                var a = _pieces[i];
                var b = _pieces[i + 1];

                if (a.Source == b.Source && a.Start + a.Length == b.Start)
                {
                    _pieces[i] = new cPiece(a.Source, a.Start, a.Length + b.Length);
                    _pieces.RemoveAt(i + 1);
                }
                else break;
            }
        }
        private void ResetDocument()
        {
            _acc?.Dispose(); _acc = null;
            _mmf?.Dispose(); _mmf = null;
            _fs?.Dispose(); _fs = null;

            _add.SetLength(0);   // ⭐ Dispose ❌, 초기화만
            _pieces.Clear();

            Length = 0;
        }
        public void Dispose()
        {
            _acc?.Dispose(); _acc = null;
            _mmf?.Dispose(); _mmf = null;
            _fs?.Dispose(); _fs = null;

            _add?.Dispose();
            _pieces.Clear();
            Length = 0;
            _originalLen = 0;
        }
    }
}
