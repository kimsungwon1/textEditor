using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace TextEditer
{
    public struct TextCursor
    {
        public int Line;
        public int Column;
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
            m_cursor.Column = 0;

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
            textBuffer.Load(path);
            m_iBuffer = textBuffer;

            m_cursor.Line = 0;
            m_cursor.Column = 0;

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
            if (m_iBuffer == null || m_vScroll == null) return;

            // 클릭 위치 -> 커서 이동(읽기 전용이므로 이동만)
            int firstLine = m_vScroll.Value;
            int line = firstLine + (e.Y / m_nLineHeight);

            if (line < 0) line = 0;
            if (line >= m_iBuffer.LineCount) line = m_iBuffer.LineCount - 1;
            if (line < 0) line = 0;

            int col = GetColumnFromX(line, e.X);
            m_cursor.Line = line;
            m_cursor.Column = col;

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

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (m_iBuffer == null || m_vScroll == null) return;

            bool changed = false;

            if(((int)e.KeyCode >= 65 && (int)e.KeyCode <= 90) || ((int)e.KeyCode >= 96 && (int)e.KeyCode <= 106))
            {
                Byte[] bytes = new Byte[1];
                bytes[0] = (Byte)e.KeyValue;
                m_iBuffer.AddBuffer(bytes, m_iBuffer.GetIndex(m_cursor.Line, m_cursor.Column));
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.Up:
                        if (m_cursor.Line > 0) { m_cursor.Line--; changed = true; }
                        break;

                    case Keys.Down:
                        if (m_cursor.Line + 1 < m_iBuffer.LineCount) { m_cursor.Line++; changed = true; }
                        break;

                    case Keys.Left:
                        if (m_cursor.Column > 0) { m_cursor.Column--; changed = true; }
                        break;

                    case Keys.Right:
                        m_cursor.Column++; changed = true;
                        break;

                    case Keys.Home:
                        m_cursor.Column = 0; changed = true;
                        break;

                    case Keys.End:
                        m_cursor.Column = m_iBuffer.GetLineLength(m_cursor.Line); changed = true;
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
                            m_cursor.Line = Math.Min(m_iBuffer.LineCount - 1, m_cursor.Line + visible);
                            changed = true;
                            break;
                        }
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
            if (m_iBuffer == null)
            {
                m_cursor.Line = 0; m_cursor.Column = 0;
                return;
            }

            if (m_cursor.Line < 0) m_cursor.Line = 0;
            if (m_cursor.Line >= m_iBuffer.LineCount) m_cursor.Line = m_iBuffer.LineCount - 1;
            if (m_cursor.Line < 0) m_cursor.Line = 0;

            int len = m_iBuffer.GetLineLength(m_cursor.Line);
            if (m_cursor.Column < 0) m_cursor.Column = 0;
            if (m_cursor.Column > len) m_cursor.Column = len;
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

        private void UpdateScrollBar()
        {
            if (m_iBuffer == null || m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int total = m_iBuffer.LineCount;

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
                    if (lineIndex >= m_iBuffer.LineCount)
                        break;

                    string line = m_iBuffer.GetLine(lineIndex);
                    g.DrawString(line, m_font, textBrush, new PointF(0, i * m_nLineHeight), m_stringFormat);
                }
            }
        }

        private void RenderCaret(Graphics g)
        {
            if (!m_bCaretVisible)
                return;
            if (m_iBuffer == null || m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int screenLine = m_cursor.Line - m_vScroll.Value;
            if (screenLine < 0 || screenLine >= visible)
                return;

            int x = GetXFromColumn(g, m_cursor.Line, m_cursor.Column);
            int y = screenLine * m_nLineHeight;

            using (SolidBrush caretBrush = new SolidBrush(m_caretColor))
            {
                g.FillRectangle(caretBrush, x, y, 2, m_nLineHeight);
            }
        }

        private int GetXFromColumn(Graphics g, int lineIndex, int column)
        {
            if (m_iBuffer == null)
                return 0;

            string line = m_iBuffer.GetLine(lineIndex) ?? string.Empty;

            if (column <= 0)
                return 0;
            if (column > line.Length) column = line.Length;

            string sub = line.Substring(0, column);

            SizeF size = g.MeasureString(sub, m_font, int.MaxValue, m_stringFormat);
            return (int)Math.Round(size.Width);
        }

        private int GetColumnFromX(int lineIndex, int x)
        {
            if (m_iBuffer == null)
                return 0;

            string text = m_iBuffer.GetLine(lineIndex) ?? string.Empty;
            if (text.Length == 0) return 0;

            // 단순/정확 버전: 1글자씩 측정 (읽기 전용 뷰어는 충분)
            // 더 빠르게 하려면 이분탐색 + prefix width 캐시로 개선 가능
            int low = 0, high = text.Length;
            using (Graphics g = CreateGraphics())
            {
                while (low < high)
                {
                    int mid = (low + high) / 2;
                    string sub = text.Substring(0, mid + 1);
                    float w = g.MeasureString(sub, m_font, int.MaxValue, m_stringFormat).Width;

                    if (w <= x) low = mid + 1;
                    else high = mid;
                }
            }
            return low;
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
