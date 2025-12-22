using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditer
{
    public struct TextCursor
    {
        public int Line;
        public long ByteOffset;
        public float PreferredX;
    }
    public class VirtualTextEditer : UserControl
    {
        private ITextBuffer m_buffer;
        private VScrollBar m_vScroll;

        private TextCursor m_cursor;
        private readonly Font m_font = new Font("Consolas", 10);
        private readonly int m_nLineHeight = 16;

        private readonly StringFormat m_stringFormat =
            new StringFormat(StringFormat.GenericTypographic)
            {
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces
            };

        // 커서 깜빡임(읽기 전용 뷰어도 있으면 편함)
        private readonly Timer m_caretTimer;
        private bool m_bCaretVisible = true;
        private Color m_caretColor = Color.Black; 

        const int TextPaddingLeft = 0;

        int firstVisibleLine;
        public VirtualTextEditer()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.Selectable,
                true);

            TabStop = true;

            BackColor = Color.White;
            ForeColor = Color.Black;

            m_cursor.Line = 0;
            m_cursor.ByteOffset = 0;

            m_caretTimer = new Timer { Interval = 500 };
            m_caretTimer.Tick += (_, __) =>
            {
                m_bCaretVisible = !m_bCaretVisible;
                Invalidate();
            };
            m_caretTimer.Start();
        }

        // 외부에서 호출하는 로드 함수
        public void LoadFile(string path)
        {
            // 기존 버퍼 정리
            m_buffer?.Dispose();

            ITextBuffer textBuffer = new ITextBuffer();
            textBuffer.LoadOriginal(path);
            m_buffer = textBuffer;

            m_cursor.Line = 0;
            m_cursor.ByteOffset = 0;

            firstVisibleLine = m_vScroll.Value;

            EnsureScrollCreated();
            if (m_vScroll != null)
                m_vScroll.Value = 0;

            UpdateScrollBar();
            Invalidate();
        }

        public void SaveAs(string sPath)
        {
            m_buffer.SaveAs(sPath);
        }

        public void Save()
        {
            m_buffer.Save();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (!DesignMode)
                EnsureScrollCreated();
        }

        private void EnsureScrollCreated()
        {
            if (m_vScroll != null)
                return;

            m_vScroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 1
            };
            m_vScroll.Scroll += (_, __) => Invalidate();

            Controls.Add(m_vScroll);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
            if (m_buffer == null || m_vScroll == null)
                return;

            int firstLine = m_vScroll.Value;
            int line = firstLine + (e.Y / m_nLineHeight);

            if (line < 0) line = 0;
            if (line >= m_buffer.m_nLineCount)
                line = m_buffer.m_nLineCount - 1;

            using (Graphics g = CreateGraphics())
            {
                float textX = e.X - TextPaddingLeft;
                if (textX < 0) textX = 0;

                int col = GetColumnFromX(g, m_buffer.GetLineUtf8(line), textX);
                long byteOffset = m_buffer.GetIndex(line, col);

                m_cursor.Line = line;
                m_cursor.ByteOffset = byteOffset;
                m_cursor.PreferredX = textX;
            }

            ClampCursor();
            EnsureCursorVisible();
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            if (m_vScroll == null) return;

            int lines = SystemInformation.MouseWheelScrollLines;
            if (lines <= 0) lines = 3;

            int deltaLines = e.Delta > 0 ? -lines : lines;

            int newValue = m_vScroll.Value + deltaLines;
            newValue = Math.Max(m_vScroll.Minimum, Math.Min(newValue, m_vScroll.Maximum));
            m_vScroll.Value = newValue;

            Invalidate();
        }
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar))
            {
                InsertText(ref m_cursor, e.KeyChar.ToString()); 
                e.Handled = true;
            }
        }
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (m_buffer == null || m_vScroll == null) return;

            bool changed = false;
            
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (m_cursor.Line > 0)
                    {
                        long dwLineOffset = m_cursor.ByteOffset - m_buffer.GetLineStartByteOffset(m_cursor.Line);
                        long dwLineOffsetAbove = m_buffer.GetLineStartByteOffset(m_cursor.Line) - (m_buffer.GetLineStartByteOffset(m_cursor.Line - 1) + dwLineOffset);

                        if(m_cursor.Line - 1 == 0)
                        {
                            dwLineOffset--;
                            dwLineOffsetAbove -= 2;
                        }

                        m_cursor.Line--;
                        m_cursor.ByteOffset -= (dwLineOffset + dwLineOffsetAbove);
                        changed = true;
                    }
                    break;

                case Keys.Down:
                    if (m_cursor.Line + 1 < m_buffer.m_nLineCount)
                    {
                        long dwLineOffsetUnder = Math.Min(m_cursor.ByteOffset - m_buffer.GetLineStartByteOffset(m_cursor.Line), m_buffer.GetLineStartByteOffset(m_cursor.Line + 2) - m_buffer.GetLineStartByteOffset(m_cursor.Line + 1));
                        long dwLineOffset = m_buffer.GetLineStartByteOffset(m_cursor.Line + 1) - m_cursor.ByteOffset;

                        if(m_cursor.Line == 0)
                        {
                            dwLineOffset--;
                            dwLineOffsetUnder -= 2;
                        }

                        m_cursor.Line++;
                        m_cursor.ByteOffset += (dwLineOffset + dwLineOffsetUnder);
                        changed = true;
                    }
                    break;

                case Keys.Left:
                    {
                        long lineStart = m_buffer.GetLineStartByteOffset(m_cursor.Line);

                        if (m_cursor.ByteOffset > lineStart)
                        {
                            int nByte = m_buffer.GetPreviousCharByteLength(m_cursor.Line, m_cursor.ByteOffset);
                            if (nByte > 0)
                                m_cursor.ByteOffset -= nByte;
                        }
                        else if (m_cursor.Line > 0)
                        {
                            m_cursor.Line--;
                            m_cursor.ByteOffset =
                                m_buffer.GetLineEndByteOffset(m_cursor.Line);
                        }

                        changed = true;
                        break;
                    }

                case Keys.Right:
                    {
                        long lineEnd = m_buffer.GetLineEndByteOffset(m_cursor.Line);

                        if (m_cursor.ByteOffset < lineEnd)
                        {
                            int nByte = m_buffer.GetNextCharByteLength(m_cursor.Line, m_cursor.ByteOffset);
                            if (nByte > 0)
                                m_cursor.ByteOffset += nByte;
                        }
                        else if (m_cursor.Line + 1 < m_buffer.m_nLineCount)
                        {
                            m_cursor.Line++;
                            m_cursor.ByteOffset =
                                m_buffer.GetLineStartByteOffset(m_cursor.Line);
                        }

                        changed = true;
                        break;
                    }

                case Keys.Home:
                    m_cursor.ByteOffset = 0; changed = true;
                    break;

                case Keys.End:
                    m_cursor.ByteOffset = m_buffer.GetLineLength(m_cursor.Line); changed = true;
                    break;

                case Keys.PageUp:
                    {
                        int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
                        m_cursor.Line = Math.Max(0, m_cursor.Line - visible);
                        changed = true;
                        break;
                    }

                case Keys.PageDown:
                    {
                        int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
                        m_cursor.Line = Math.Min(m_buffer.m_nLineCount - 1, m_cursor.Line + visible);
                        changed = true;
                        break;
                    }
                case Keys.Back:
                    {
                        Backspace(m_cursor.Line, ref m_cursor);
                        break;
                    }
                case Keys.Delete:
                    {
                        Delete(ref m_cursor);
                        break;
                    }
                case Keys.Enter:
                    {
                        EnterKeyPressed(m_cursor.Line, ref m_cursor);
                        break;
                    }
            }
            
            if (changed)
            {
                ClampCursor();
                EnsureCursorVisible();
                Invalidate();
            }
        }
        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                case Keys.Back:
                case Keys.Delete:
                case Keys.Enter:
                    return true;
            }

            return base.IsInputKey(keyData);
        }
        private void ClampCursor()
        {
            if (m_buffer == null || m_buffer.m_nLineCount == 0)
            {
                m_cursor.Line = 0;
                m_cursor.ByteOffset = 0;
                return;
            }

            if (m_cursor.Line < 0)
                m_cursor.Line = 0;
            else if (m_cursor.Line >= m_buffer.m_nLineCount)
                m_cursor.Line = m_buffer.m_nLineCount - 1;

            long lineStart = m_buffer.GetLineStartByteOffset(m_cursor.Line);

            long lineEnd;
            if (m_cursor.Line + 1 < m_buffer.m_nLineCount)
                lineEnd = m_buffer.GetLineStartByteOffset(m_cursor.Line + 1);
            else
                lineEnd = m_buffer.m_nLength;

            if (lineEnd < lineStart)
                lineEnd = lineStart;

            if (m_cursor.ByteOffset < lineStart)
                m_cursor.ByteOffset = lineStart;
            else if (m_cursor.ByteOffset > lineEnd)
                m_cursor.ByteOffset = lineEnd;
        }

        private void EnsureCursorVisible()
        {
            if (m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);

            if (m_cursor.Line < m_vScroll.Value)
                m_vScroll.Value = m_cursor.Line;
            else if (m_cursor.Line >= m_vScroll.Value + visible)
                m_vScroll.Value = Math.Min(m_vScroll.Maximum, m_cursor.Line - visible + 1);
        }
        protected void InsertText(ref TextCursor cursor, string text)
        {
            long pos = cursor.ByteOffset;
            byte[] bytes = Encoding.UTF8.GetBytes(text);

            m_buffer.InsertUtf8(cursor.Line, pos, text);

            cursor.ByteOffset += bytes.Length;

            cursor.Line += CountNewLines(text);

            ClampCursor();
        }
        private int CountNewLines(string s)
        {
            int c = 0;
            foreach (char ch in s)
                if (ch == '\n') c++;
            return c;
        }
        private int GetColumn(TextCursor cursor)
        {
            long lineStart = m_buffer.GetLineStartByteOffset(cursor.Line);
            long len = cursor.ByteOffset - lineStart;

            if (len <= 0) return 0;

            byte[] bytes = m_buffer.ReadRangeBytes(m_cursor.Line, lineStart, (int)len);
            return Encoding.UTF8.GetCharCount(bytes);
        }
        void Backspace(int nLine, ref TextCursor cursor)
        {
            if (cursor.ByteOffset <= 0)
                return;

            if (cursor.Line == 0 && cursor.ByteOffset == 0)
                return;

            long lineStart = m_buffer.GetLineStartByteOffset(cursor.Line);

            if(cursor.ByteOffset == lineStart)
            {
                int newlineBytes = m_buffer.GetPreviousCharByteLength(m_cursor.Line, cursor.ByteOffset);

                m_buffer.Delete(cursor.Line - 1, cursor.ByteOffset - newlineBytes, newlineBytes, true);

                cursor.Line--;
                cursor.ByteOffset -= newlineBytes;

                return;
            }

            int bytesToDelete = m_buffer.GetPreviousCharByteLength(m_cursor.Line, cursor.ByteOffset);

            m_buffer.Delete(nLine, cursor.ByteOffset - bytesToDelete, bytesToDelete);
            cursor.ByteOffset -= bytesToDelete;
        }

        public void Delete(ref TextCursor cursor)
        {
            if (cursor.ByteOffset >= m_buffer.m_nLength)
                return;

            if (m_cursor.Line + 1 >= m_buffer.m_nLineCount)
                return;

            long nextLineStart = m_buffer.GetLineStartByteOffset(cursor.Line + 1);

            int charBytesToRemove = (int)(nextLineStart - m_cursor.ByteOffset);

            int newLineBytes = GetNewlineByteLength(m_cursor.Line, m_cursor.ByteOffset);

            if (newLineBytes == 2 || newLineBytes == 1)
            {
                m_buffer.Delete(m_cursor.Line, cursor.ByteOffset, charBytesToRemove, true);

                return;
            }

            int bytes = m_buffer.GetNextCharByteLength(m_cursor.Line,cursor.ByteOffset);
            m_buffer.Delete(m_cursor.Line, cursor.ByteOffset, bytes);
        }
        public void EnterKeyPressed(int nLine, ref TextCursor cursor)
        {
            const string sNewLine = "\r";

            m_buffer.InsertUtf8(cursor.Line, cursor.ByteOffset, sNewLine);

            // m_buffer.RebuildLineIndex();

            cursor.Line++;

            cursor.ByteOffset = m_buffer.GetLineStartByteOffset(cursor.Line);
        }

        private void UpdateScrollBar()
        {
            if (m_buffer == null || m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int total = m_buffer.m_nLineCount;

            int maxFirstLine = Math.Max(0, total - visible);

            m_vScroll.Minimum = 0;
            m_vScroll.LargeChange = visible;
            m_vScroll.Maximum = maxFirstLine + m_vScroll.LargeChange - 1;

            if (m_vScroll.Value > maxFirstLine)
                m_vScroll.Value = maxFirstLine;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // 디자이너에서는 안전하게 아무 것도 안 그림
            if (DesignMode) return;

            if (m_buffer == null || m_vScroll == null)
            {
                e.Graphics.Clear(BackColor);
                using (SolidBrush b = new SolidBrush(ForeColor))
                {
                    e.Graphics.DrawString("No file loaded.", Font, b, 4, 4);
                }
                return;
            }

            RenderText(e.Graphics);
            RenderCaret(e.Graphics);
        }

        private void RenderText(Graphics g)
        {
            if (m_buffer == null || m_vScroll == null)
                return;

            g.Clear(BackColor);

            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                int firstLine = m_vScroll.Value;
                int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);

                for (int i = 0; i < visible; i++)
                {
                    int lineIndex = firstLine + i;
                    if (lineIndex >= m_buffer.m_nLineCount)
                        break;

                    string line = m_buffer.GetLineUtf8(lineIndex);

                    float x = TextPaddingLeft;
                    float y = i * m_nLineHeight;

                    g.DrawString(
                        line,
                        m_font,
                        textBrush,
                        new PointF(x, y),
                        sf
                    );
                }
            }
        }
        void RenderCaret(Graphics g)
        {
            if (m_buffer == null || m_vScroll == null)
                return;

            int firstVisibleLine = m_vScroll.Value;
            int visibleLines = ClientSize.Height / m_nLineHeight;

            int caretLineOnScreen = m_cursor.Line - firstVisibleLine;
            if (caretLineOnScreen < 0 || caretLineOnScreen >= visibleLines)
                return;

            // 1. 현재 줄 텍스트
            string lineText = m_buffer.GetLineUtf8(m_cursor.Line);

            // 2. caret column (문자 "뒤" 위치)
            int column = GetColumn(m_cursor);
            if (column < 0) column = 0;
            if (column > lineText.Length) column = lineText.Length;

            // 3. prefix 문자열
            string prefix = (column == 0) ? string.Empty : lineText.Substring(0, column);

            // 4. GDI 정확 측정
            float xOffset = 0f;
            if (prefix.Length > 0)
            {
                using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic))
                {
                    sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                    SizeF size = g.MeasureString(
                        prefix,
                        m_font,
                        int.MaxValue,
                        sf
                    );

                    xOffset = size.Width;
                }
            }

            // 5. 최종 caret 위치
            float x = TextPaddingLeft + xOffset;
            float y = caretLineOnScreen * m_nLineHeight;

            // 6. caret 그리기
            using (SolidBrush b = new SolidBrush(m_caretColor))
            {
                g.FillRectangle(b, x, y, 2f, m_nLineHeight);
            }
        }

        private int GetColumnFromX(Graphics g, string text, float x)
        {
            var bounds = BuildCaretBoundaries(g, text);

            int best = 0;
            float bestDist = Math.Abs(x - bounds[0]);

            for (int i = 1; i < bounds.Length; i++)
            {
                float d = Math.Abs(x - bounds[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }
        private List<RectangleF> MeasureCharBounds(Graphics g, string text)
        {
            List<RectangleF> result = new List<RectangleF>(text.Length);

            using (var sf = new StringFormat(StringFormat.GenericTypographic))
            {
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                const int MAX = 32;
                float xOffset = 0;

                for (int start = 0; start < text.Length; start += MAX)
                {
                    int len = Math.Min(MAX, text.Length - start);
                    string chunk = text.Substring(start, len);

                    CharacterRange[] ranges = new CharacterRange[len];
                    for (int i = 0; i < len; i++)
                        ranges[i] = new CharacterRange(i, 1);

                    sf.SetMeasurableCharacterRanges(ranges);

                    RectangleF layout = new RectangleF(0, 0, 10000, m_nLineHeight);
                    Region[] regions = g.MeasureCharacterRanges(chunk, m_font, layout, sf);

                    for (int i = 0; i < regions.Length; i++)
                    {
                        RectangleF r = regions[i].GetBounds(g);
                        r.Offset(xOffset, 0);
                        result.Add(r);
                    }

                    // 다음 블록 시작 X 보정
                    var sz = g.MeasureString(chunk, m_font, int.MaxValue, sf);
                    xOffset += sz.Width;
                }
            }

            return result;
        }
        private float[] BuildCaretBoundaries(Graphics g, string text)
        {
            var charBounds = MeasureCharBounds(g, text);
            float[] bounds = new float[text.Length + 1];

            bounds[0] = 0;
            for (int i = 0; i < charBounds.Count; i++)
                bounds[i + 1] = charBounds[i].Right;

            return bounds;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_caretTimer?.Stop();
                m_buffer?.Dispose();
                m_font?.Dispose();
                m_stringFormat?.Dispose();
            }
            base.Dispose(disposing);
        }
        private int GetNewlineByteLength(int nLine, long byteOffset)
        {
            if (byteOffset >= m_buffer.m_nLength)
                return 0;
            
            byte[] buf = m_buffer.ReadRangeBytes(nLine, byteOffset, 2);

            if (buf.Length >= 2 && buf[0] == (byte)'\r' && buf[1] == (byte)'\n')
                return 2;

            if (buf.Length >= 1 && buf[0] == (byte)'\n')
                return 1;

            return 0;
        }

    }

}
