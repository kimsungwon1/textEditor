using System;
using System.Collections.Generic;
using System.IO;
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
    public sealed class cLineBlock
    {
        public int m_nStartLine { get; set; }

        public List<cLineRef> m_listLines { get; private set; }

        public bool m_bDirty { get; set; }

        public int LineCount
        {
            get { return m_listLines.Count; }
        }

        public cLineBlock(int startLine, IEnumerable<cLineRef> lines)
        {
            m_nStartLine = startLine;
            m_listLines = new List<cLineRef>(lines);
            m_bDirty = true;
        }
    }

    public sealed class cDocument
    {
        private readonly List<cLineBlock> m_listBlocks;
        private cTextBuffer m_buffer;
        public cDocument(cTextBuffer buffer, IEnumerable<LineSpan> fileLines)
        {
            m_listBlocks = new List<cLineBlock>();

            m_buffer = buffer;
            BuildInitialBlocks(fileLines);
        }
        public int LineCount
        {
            get
            {
                int count = 0;
                foreach (cLineBlock b in m_listBlocks)
                    count += b.LineCount;
                return count;
            }
        }
        private void BuildInitialBlocks(IEnumerable<LineSpan> spans)
        {
            List<cLineRef> temp = new List<cLineRef>();
            int startLine = 0;

            foreach (LineSpan span in spans)
            {
                temp.Add(cLineRef.FromFile(span));

                if (temp.Count == BlockPolicy.MaxLines)
                {
                    m_listBlocks.Add(new cLineBlock(startLine, temp));
                    startLine += temp.Count;
                    temp = new List<cLineRef>();
                }
            }

            if (temp.Count > 0)
            {
                m_listBlocks.Add(new cLineBlock(startLine, temp));
            }
        }

        private string EnsureEditableText(cLineRef line)
        {
            if (line.IsDirty)
                return line.EditedText ?? string.Empty;

            string text = m_buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);

            line.MakeDirty(text);
            return text;
        }

        private cLineBlock FindBlock(int globalLine, out int innerIndex)
        {
            int left = 0;
            int right = m_listBlocks.Count - 1;

            while (left <= right)
            {
                int mid = (left + right) / 2;
                cLineBlock block = m_listBlocks[mid];

                if (globalLine < block.m_nStartLine)
                {
                    right = mid - 1;
                }
                else if (globalLine >= block.m_nStartLine + block.LineCount)
                {
                    left = mid + 1;
                }
                else
                {
                    innerIndex = globalLine - block.m_nStartLine;
                    return block;
                }
            }

            innerIndex = 0;
            return null;
        }
        public void InsertChar(int globalLine, int column, char ch)
        {
            if (globalLine < 0 || globalLine >= LineCount)
                return;

            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);
            if (block == null)
            {
                return;
            }
            cLineRef line = block.m_listLines[inner];

            string text;

            if (line.IsDirty)
            {
                text = line.EditedText ?? string.Empty;
            }
            else
            {
                // 파일 기반 줄 → 문자열로 변환
                text = m_buffer.ReadUtf8(
                    line.FileSpan.Offset,
                    line.FileSpan.ByteLength);
            }

            // column 보정
            if (column < 0) column = 0;
            if (column > text.Length) column = text.Length;

            string newText = text.Insert(column, ch.ToString());

            line.MakeDirty(newText);
            block.m_bDirty = true;
        }
        public void InsertNewLine(int globalLine, int column)
        {
            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);
            if(block == null)
            {
                return;
            }
            cLineRef line = block.m_listLines[inner];

            string text = line.IsDirty ? (line.EditedText ?? string.Empty) : m_buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);

            if (column < 0) column = 0;
            if (column > text.Length) column = text.Length;

            string before = text.Substring(0, column);
            string after = text.Substring(column);

            line.MakeDirty(before);
            block.m_listLines.Insert(inner + 1, cLineRef.FromEdited(after));
            block.m_bDirty = true;

            SplitIfNeeded(block);
        }

        private void RecalculateStartLines(int startBlockIndex)
        {
            int startLine = m_listBlocks[startBlockIndex].m_nStartLine;

            for (int i = startBlockIndex; i < m_listBlocks.Count; i++)
            {
                m_listBlocks[i].m_nStartLine = startLine;
                startLine += m_listBlocks[i].LineCount;
            }
        }
        private void SplitIfNeeded(cLineBlock block)
        {
            if (block.LineCount <= BlockPolicy.MaxLines)
                return;

            int half = block.LineCount / 2;
            List<cLineRef> newLines = block.m_listLines.GetRange(half, block.LineCount - half);
            block.m_listLines.RemoveRange(half, block.LineCount - half);

            int idx = m_listBlocks.IndexOf(block);
            cLineBlock newBlock = new cLineBlock(block.m_nStartLine + block.LineCount, newLines);

            m_listBlocks.Insert(idx + 1, newBlock);
            RecalculateStartLines(idx);
        }
        private void MergeIfNeeded(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= m_listBlocks.Count)
                return;

            cLineBlock block = m_listBlocks[blockIndex];
            if (block.LineCount >= BlockPolicy.MinLines)
                return;

            if (blockIndex + 1 < m_listBlocks.Count)
            {
                cLineBlock next = m_listBlocks[blockIndex + 1];
                if (block.LineCount + next.LineCount <= BlockPolicy.MaxLines)
                {
                    block.m_listLines.AddRange(next.m_listLines);
                    m_listBlocks.RemoveAt(blockIndex + 1);
                    RecalculateStartLines(blockIndex);
                }
            }
        }
        public string GetLineText(int globalLine)
        {
            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);
            if (block == null)
            {
                return "";
            }
            cLineRef line = block.m_listLines[inner];

            if (line.IsDirty)
                return line.EditedText ?? string.Empty;

            return m_buffer.ReadUtf8(line.FileSpan.Offset, line.FileSpan.ByteLength);
        }
        public void Backspace(int globalLine, int column)
        {
            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);

            // 줄 내부
            if (column > 0)
            {
                cLineRef line = block.m_listLines[inner];
                string text = EnsureEditableText(line);
                line.MakeDirty(text.Remove(column - 1, 1));
                return;
            }

            // 줄 병합
            if (globalLine == 0)
                return;

            int prevInner;
            cLineBlock prevBlock = FindBlock(globalLine - 1, out prevInner);

            cLineRef cur = block.m_listLines[inner];
            cLineRef prev = prevBlock.m_listLines[prevInner];

            string curText = EnsureEditableText(cur);// cur.EditedText;
            string prevText = EnsureEditableText(prev);// prev.EditedText;

            prev.MakeDirty(prevText + curText);
            block.m_listLines.RemoveAt(inner);

            if (block.LineCount == 0)
                m_listBlocks.Remove(block);

            MergeIfNeeded(m_listBlocks.IndexOf(prevBlock));
        }
        public void Delete(int globalLine, int column)
        {
            int inner;
            cLineBlock block = FindBlock(globalLine, out inner);
            cLineRef line = block.m_listLines[inner];

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
                cLineRef next = nextBlock.m_listLines[nextInner];

                string nextText = EnsureEditableText(next);

                line.MakeDirty(text + nextText);
                nextBlock.m_listLines.RemoveAt(nextInner);

                if (nextBlock.LineCount == 0)
                    m_listBlocks.Remove(nextBlock);

                MergeIfNeeded(m_listBlocks.IndexOf(block));
            }
            else if(column < text.Length)
            {
                string newText = text.Remove(column, 1);
                line.MakeDirty(newText);
            }
        }
        
        public void Dispose()
        {
            m_buffer.Dispose();
        }
    }
}
