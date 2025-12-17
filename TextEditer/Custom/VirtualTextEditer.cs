using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace TextEditer
{
    public struct TextCursor
    {
        public int Line;
        public long ByteOffset;
    }
    public class VirtualTextEditer : UserControl
    {
        private ITextBuffer m_iBuffer;
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

        // ✅ 외부에서 호출하는 로드 함수
        public void LoadFile(string path)
        {
            // 기존 버퍼 정리
            m_iBuffer?.Dispose();

            ITextBuffer textBuffer = new ITextBuffer();
            textBuffer.LoadOriginal(path);
            m_iBuffer = textBuffer;

            m_cursor.Line = 0;
            m_cursor.ByteOffset = 0;

            firstVisibleLine = m_vScroll.Value;

            EnsureScrollCreated();
            if (m_vScroll != null)
                m_vScroll.Value = 0;

            UpdateScrollBar();
            Invalidate();
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

            if (m_iBuffer == null || m_vScroll == null)
                return;

            int firstLine = m_vScroll.Value;
            int line = firstLine + (e.Y / m_nLineHeight);

            if (line < 0) line = 0;
            if (line >= m_iBuffer.m_nLineCount)
                line = m_iBuffer.m_nLineCount - 1;

            // ⭐ 텍스트 시작 X 보정
            int textX = e.X - TextPaddingLeft;
            if (textX < 0) textX = 0;

            int col = GetColumnFromX(line, textX);
            long byteOffset = m_iBuffer.GetIndex(line, col);

            m_cursor.Line = line;
            m_cursor.ByteOffset = byteOffset;

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
            if (m_iBuffer == null || m_vScroll == null) return;

            bool changed = false;
            
            
            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (m_cursor.Line > 0) { m_cursor.Line--; changed = true; }
                    break;

                case Keys.Down:
                    if (m_cursor.Line + 1 < m_iBuffer.m_nLineCount) { m_cursor.Line++; changed = true; }
                    break;

                case Keys.Left:
                    {
                        int b = m_iBuffer.GetPreviousCharByteLength(m_cursor.ByteOffset);
                        if (b > 0)
                            m_cursor.ByteOffset -= b;
                        changed = true;
                        break;
                    }

                case Keys.Right:
                    {
                        int b = m_iBuffer.GetNextCharByteLength(m_cursor.ByteOffset);
                        if (b > 0)
                            m_cursor.ByteOffset += b;
                        changed = true;
                        break;
                    }

                case Keys.Home:
                    m_cursor.ByteOffset = 0; changed = true;
                    break;

                case Keys.End:
                    m_cursor.ByteOffset = m_iBuffer.GetLineLength(m_cursor.Line); changed = true;
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
                        m_cursor.Line = Math.Min(m_iBuffer.m_nLineCount - 1, m_cursor.Line + visible);
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
                        Delete(m_cursor.Line, ref m_cursor);
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

        private void ClampCursor()
        {
            if (m_iBuffer == null || m_iBuffer.m_nLineCount == 0)
            {
                m_cursor.Line = 0;
                m_cursor.ByteOffset = 0;
                return;
            }

            // 1. Line 클램프
            if (m_cursor.Line < 0)
                m_cursor.Line = 0;
            else if (m_cursor.Line >= m_iBuffer.m_nLineCount)
                m_cursor.Line = m_iBuffer.m_nLineCount - 1;

            // 2. 해당 줄의 바이트 범위 구하기
            long lineStart = m_iBuffer.GetLineStartByteOffset(m_cursor.Line);

            long lineEnd;
            if (m_cursor.Line + 1 < m_iBuffer.m_nLineCount)
                lineEnd = m_iBuffer.GetLineStartByteOffset(m_cursor.Line + 1);
            else
                lineEnd = m_iBuffer.m_nLength;

            // CR/LF 중 LF 제외하고 싶으면 여기서 -1 처리 가능
            if (lineEnd < lineStart)
                lineEnd = lineStart;

            // 3. ByteOffset 클램프 (⭐ 핵심)
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

            m_iBuffer.InsertUtf8(cursor.Line, pos, text);

            cursor.ByteOffset += bytes.Length;// Encoding.UTF8.GetByteCount(text);

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
            long lineStart = m_iBuffer.GetLineStartByteOffset(cursor.Line);
            long len = cursor.ByteOffset - lineStart;

            if (len <= 0) return 0;

            byte[] bytes = m_iBuffer.ReadRangeBytes(lineStart, (int)len);
            return Encoding.UTF8.GetCharCount(bytes);
        }
        void Backspace(int nLine, ref TextCursor cursor)
        {
            if (cursor.ByteOffset <= 0)
                return;

            int bytesToDelete = m_iBuffer.GetPreviousCharByteLength(cursor.ByteOffset);

            m_iBuffer.Delete(nLine, cursor.ByteOffset - bytesToDelete, bytesToDelete);
            cursor.ByteOffset -= bytesToDelete;
        }

        public void Delete(int nLine, ref TextCursor cursor)
        {
            if (cursor.ByteOffset >= m_iBuffer.m_nLength)
                return;

            int bytes = m_iBuffer.GetNextCharByteLength(cursor.ByteOffset);
            m_iBuffer.Delete(nLine, cursor.ByteOffset, bytes);
        }
        private void UpdateScrollBar()
        {
            if (m_iBuffer == null || m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int total = m_iBuffer.m_nLineCount;

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

            if (m_iBuffer == null || m_vScroll == null)
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
            if (m_iBuffer == null || m_vScroll == null)
                return;

            g.Clear(BackColor);

            using (SolidBrush textBrush = new SolidBrush(ForeColor))
            {
                int firstLine = m_vScroll.Value;
                int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);

                // 스크롤바 영역만큼 우측 여백(가려짐 방지)
                int rightPadding = m_vScroll.Width;

                for (int i = 0; i < visible; i++)
                {
                    int lineIndex = firstLine + i;
                    if (lineIndex >= m_iBuffer.m_nLineCount)
                        break;

                    string line = m_iBuffer.GetLineUtf8(lineIndex);
                    // g.DrawString(line, m_font, textBrush, new PointF(TextPaddingLeft, i * m_nLineHeight), m_stringFormat);
                    TextRenderer.DrawText(
                        g,
                        line,
                        m_font,
                        new Point(TextPaddingLeft, i * m_nLineHeight),
                        ForeColor,
                        TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
                    );
                }
            }
        }
        void RenderCaret(Graphics g)
        {
            if (m_iBuffer == null || m_vScroll == null) return;

            int firstVisibleLine = m_vScroll.Value;
            int visibleLines = ClientSize.Height / m_nLineHeight;

            int caretLineOnScreen = m_cursor.Line - firstVisibleLine;
            if (caretLineOnScreen < 0 || caretLineOnScreen >= visibleLines)
                return;

            int column = GetColumn(m_cursor);

            string lineText = m_iBuffer.GetLineUtf8(m_cursor.Line);
            if (column < 0) column = 0;
            if (column > lineText.Length) column = lineText.Length;

            int[] bounds = BuildCaretBoundaries(lineText);
            int x = TextPaddingLeft + bounds[column];
            int y = caretLineOnScreen * m_nLineHeight;

            using (SolidBrush b = new SolidBrush(m_caretColor))
                g.FillRectangle(b, x, y, 2, m_nLineHeight);
        }

        private int GetColumnFromX(int lineIndex, int x)
        {
            string text = m_iBuffer.GetLineUtf8(lineIndex);
            if (string.IsNullOrEmpty(text))
                return 0;

            int[] bounds = BuildCaretBoundaries(text);

            // x가 어느 경계에 가장 가까운지 찾는다
            int best = 0;
            int bestDist = Math.Abs(x - bounds[0]);

            for (int i = 1; i < bounds.Length; i++)
            {
                int d = Math.Abs(x - bounds[i]);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = i;
                }
            }
            return best;
        }
        private int MeasurePrefixWidth(string text, int charCount)
        {
            if (charCount <= 0) return 0;
            if (charCount > text.Length) charCount = text.Length;

            // prefix substring 폭을 잰다 (TextRenderer 기반)
            string sub = text.Substring(0, charCount);
            var sz = TextRenderer.MeasureText(
                sub, m_font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
            );
            return sz.Width;
        }
        private int[] BuildCaretBoundaries(string text)
        {
            int n = text.Length;
            int[] bounds = new int[n + 1];

            bounds[0] = 0;
            for (int i = 1; i <= n; i++)
            {
                string sub = text.Substring(0, i);
                bounds[i] = TextRenderer.MeasureText(
                    sub,
                    m_font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.NoClipping
                ).Width;
            }
            return bounds;
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_caretTimer?.Stop();
                m_iBuffer?.Dispose();
                m_font?.Dispose();
                m_stringFormat?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
