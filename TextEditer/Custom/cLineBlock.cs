using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditer
{
    public static class BlockPolicy
    {
        public const int MaxLines = 256;
        public const int MinLines = 64;
    }
    public struct LineSpan
    {
        public long Offset;     // 파일 내 시작 바이트
        public int ByteLength; // 개행 제외

        public LineSpan(long offset, int byteLength)
        {
            Offset = offset; ByteLength = byteLength;
        }
    }

    public sealed class cLineRef
    {
        public LineSpan FileSpan { get; private set; }

        public string EditedText { get; private set; }

        public bool IsDirty { get; private set; }

        private cLineRef() { }

        public static cLineRef FromFile(LineSpan span)
        {
            return new cLineRef
            {
                FileSpan = span,
                EditedText = null,
                IsDirty = false
            };
        }

        public static cLineRef FromEdited(string text)
        {
            return new cLineRef
            {
                FileSpan = default(LineSpan),
                EditedText = text,
                IsDirty = true
            };
        }

        public void MakeDirty(string newText)
        {
            EditedText = newText;
            FileSpan = default(LineSpan);
            IsDirty = true;
        }
    }
    public static class cLineBlockOps
    {
        public static cLineBlock Split(cLineBlock block)
        {
            int half = block.LineCount / 2;

            var newLines = block.Lines.GetRange(half, block.LineCount - half);

            block.Lines.RemoveRange(half, block.LineCount - half);
            block.Dirty = true;

            var newBlock = new cLineBlock(
                block.StartLine + block.LineCount,
                newLines
            );

            return newBlock;
        }
        public static cLineBlock Merge(cLineBlock left, cLineBlock right)
        {
            if (left.LineCount + right.LineCount > BlockPolicy.MaxLines)
                throw new InvalidOperationException("Blocks too large to merge.");

            left.Lines.AddRange(right.Lines);
            left.Dirty = true;

            return left;
        }
    }
    public sealed class cLineBlock
    {
        public int StartLine { get; set; }

        public List<cLineRef> Lines { get; private set; }

        public bool Dirty { get; set; }

        public int LineCount
        {
            get { return Lines.Count; }
        }

        public cLineBlock(int startLine, IEnumerable<cLineRef> lines)
        {
            StartLine = startLine;
            Lines = new List<cLineRef>(lines);
            Dirty = true;
        }

        public cLineRef GetLine(int innerIndex)
        {
            return Lines[innerIndex];
        }

        public void InsertLine(int innerIndex, cLineRef line)
        {
            Lines.Insert(innerIndex, line);
            Dirty = true;
        }

        public void RemoveLine(int innerIndex)
        {
            Lines.RemoveAt(innerIndex);
            Dirty = true;
        }
    }

    public sealed class cDocument
    {
        private readonly List<cLineBlock> _blocks;
        private cTextBuffer _buffer;
        public cDocument(cTextBuffer buffer, IEnumerable<LineSpan> fileLines)
        {
            _blocks = new List<cLineBlock>();

            _buffer = buffer;
            BuildInitialBlocks(fileLines);
        }
        public int LineCount
        {
            get
            {
                int count = 0;
                foreach (var b in _blocks)
                    count += b.LineCount;
                return count;
            }
        }
        private void BuildInitialBlocks(IEnumerable<LineSpan> spans)
        {
            var temp = new List<cLineRef>();
            int startLine = 0;

            foreach (var span in spans)
            {
                temp.Add(cLineRef.FromFile(span));

                if (temp.Count == BlockPolicy.MaxLines)
                {
                    _blocks.Add(new cLineBlock(startLine, temp));
                    startLine += temp.Count;
                    temp = new List<cLineRef>();
                }
            }

            if (temp.Count > 0)
            {
                _blocks.Add(new cLineBlock(startLine, temp));
            }
        }

        private string EnsureEditableText(cLineRef line)
        {
            if (line.IsDirty)
                return line.EditedText ?? string.Empty;

            string text = _buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);

            line.MakeDirty(text);
            return text;
        }

        private cLineBlock FindBlock(int globalLine, out int innerIndex)
        {
            int left = 0;
            int right = _blocks.Count - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                var block = _blocks[mid];

                if (globalLine < block.StartLine)
                {
                    right = mid - 1;
                }
                else if (globalLine >= block.StartLine + block.LineCount)
                {
                    left = mid + 1;
                }
                else
                {
                    innerIndex = globalLine - block.StartLine;
                    return block;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(globalLine));
        }
        public void InsertChar(int globalLine, int column, char ch)
        {
            if (globalLine < 0 || globalLine >= LineCount)
                return;

            int inner;
            var block = FindBlock(globalLine, out inner);
            var line = block.Lines[inner];

            string text;

            if (line.IsDirty)
            {
                text = line.EditedText ?? string.Empty;
            }
            else
            {
                // 파일 기반 줄 → 문자열로 변환
                text = _buffer.ReadUtf8(
                    line.FileSpan.Offset,
                    line.FileSpan.ByteLength);
            }

            // column 보정
            if (column < 0) column = 0;
            if (column > text.Length) column = text.Length;

            string newText = text.Insert(column, ch.ToString());

            line.MakeDirty(newText);
            block.Dirty = true;
        }
        public void InsertNewLine(int globalLine, int column)
        {
            int inner;
            var block = FindBlock(globalLine, out inner);
            var line = block.Lines[inner];

            string text = line.IsDirty ? (line.EditedText ?? string.Empty)
                                       : _buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);

            if (column < 0) column = 0;
            if (column > text.Length) column = text.Length;

            string before = text.Substring(0, column);
            string after = text.Substring(column);

            line.MakeDirty(before);
            block.Lines.Insert(inner + 1, cLineRef.FromEdited(after));
            block.Dirty = true;

            SplitIfNeeded(block);
        }

        private void RecalculateStartLines(int startBlockIndex)
        {
            int startLine = _blocks[startBlockIndex].StartLine;

            for (int i = startBlockIndex; i < _blocks.Count; i++)
            {
                _blocks[i].StartLine = startLine;
                startLine += _blocks[i].LineCount;
            }
        }
        private void SplitIfNeeded(cLineBlock block)
        {
            if (block.LineCount <= BlockPolicy.MaxLines)
                return;

            int half = block.LineCount / 2;
            var newLines = block.Lines.GetRange(half, block.LineCount - half);
            block.Lines.RemoveRange(half, block.LineCount - half);

            int idx = _blocks.IndexOf(block);
            var newBlock = new cLineBlock(block.StartLine + block.LineCount, newLines);

            _blocks.Insert(idx + 1, newBlock);
            RecalculateStartLines(idx);
        }
        private void MergeIfNeeded(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= _blocks.Count)
                return;

            var block = _blocks[blockIndex];
            if (block.LineCount >= BlockPolicy.MinLines)
                return;

            if (blockIndex + 1 < _blocks.Count)
            {
                var next = _blocks[blockIndex + 1];
                if (block.LineCount + next.LineCount <= BlockPolicy.MaxLines)
                {
                    block.Lines.AddRange(next.Lines);
                    _blocks.RemoveAt(blockIndex + 1);
                    RecalculateStartLines(blockIndex);
                }
            }
        }
        public string GetLineText(int globalLine)
        {
            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);
            cLineRef line = block.Lines[inner];

            if (line.IsDirty)
                return line.EditedText ?? string.Empty;

            return _buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);
        }
        public void Backspace(int globalLine, int column)
        {
            int inner;
            var block = FindBlock(globalLine, out inner);

            // 줄 내부
            if (column > 0)
            {
                var line = block.Lines[inner];
                string text = EnsureEditableText(line);
                line.MakeDirty(text.Remove(column - 1, 1));
                return;
            }

            // 줄 병합
            if (globalLine == 0)
                return;

            int prevInner;
            var prevBlock = FindBlock(globalLine - 1, out prevInner);

            cLineRef cur = block.Lines[inner];
            cLineRef prev = prevBlock.Lines[prevInner];

            string curText = EnsureEditableText(cur);// cur.EditedText;
            string prevText = EnsureEditableText(prev);// prev.EditedText;

            prev.MakeDirty(prevText + curText);
            block.Lines.RemoveAt(inner);

            if (block.LineCount == 0)
                _blocks.Remove(block);

            MergeIfNeeded(_blocks.IndexOf(prevBlock));
        }
        public void Delete(int globalLine, int column)
        {
            int inner;
            var block = FindBlock(globalLine, out inner);
            var line = block.Lines[inner];

            string text = EnsureEditableText(line);// line.EditedText;

            // 줄 내부
            if (!string.IsNullOrEmpty(text) && column < text.Length)
            {
                line.MakeDirty(text.Remove(column, 1));
                return;
            }

            if(column == text.Length)
            {
                // 다음 줄 병합
                if (globalLine + 1 >= LineCount)
                    return;

                int nextInner;
                cLineBlock nextBlock = FindBlock(globalLine + 1, out nextInner);
                cLineRef next = nextBlock.Lines[nextInner];

                string nextText = EnsureEditableText(next);

                line.MakeDirty(text + nextText);
                nextBlock.Lines.RemoveAt(nextInner);

                if (nextBlock.LineCount == 0)
                    _blocks.Remove(nextBlock);

                MergeIfNeeded(_blocks.IndexOf(block));
            }
            else if(column < text.Length)
            {
                string newText = text.Remove(column, 1);
                line.MakeDirty(newText);
            }
        }
    }
}
