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
        public int Column;       // 문자 단위
        public float PreferredX;
    }
    public class VirtualTextEditer : UserControl
    {
        private cDocument m_document;

        private VScrollBar m_vScroll;

        private TextCursor m_cursor;

        private readonly Font m_font = new Font("Consolas", 10);
        private readonly int m_nLineHeight = 16;

        private readonly Timer m_caretTimer;
        private bool m_bCaretVisible = true;

        private const int m_nTextPaddingLeft = 4;

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
            TabStop = true;

            m_caretTimer = new Timer { Interval = 500 };
            m_caretTimer.Tick += (_, __) =>
            {
                m_bCaretVisible = !m_bCaretVisible;
                Invalidate();
            };
            m_caretTimer.Start();
        }

        // 파일 로드 (진입점)
        public void LoadFile(string path)
        {
            cTextBuffer buffer = new cTextBuffer(path);
            IEnumerable<LineSpan> lines = cLineIndexer.Scan(buffer);

            m_document = new cDocument(buffer, lines);

            m_cursor.Line = 0;
            m_cursor.Column = 0;
            m_cursor.PreferredX = 0;

            EnsureScroll();
            UpdateScrollBar();

            Invalidate();
        }

        // Scroll
        private void EnsureScroll()
        {
            if (m_vScroll != null) return;

            m_vScroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 1
            };
            m_vScroll.Scroll += (_, __) => Invalidate();
            Controls.Add(m_vScroll);
        }

        private void UpdateScrollBar()
        {
            if (m_document == null || m_vScroll == null) return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int total = m_document.LineCount;

            int maxFirst = Math.Max(0, total - visible);

            m_vScroll.Minimum = 0;
            m_vScroll.LargeChange = visible;
            m_vScroll.Maximum = maxFirst + visible;
            m_vScroll.Value = Math.Min(m_vScroll.Value, maxFirst);
        }

        // Paint
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);

            if (m_document == null || m_vScroll == null)
                return;

            DrawText(e.Graphics);
            DrawCaret(e.Graphics);
        }

        private void DrawText(Graphics g)
        {
            int firstLine = m_vScroll.Value;
            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);

            Color textColor = Color.Black;
            using (SolidBrush brush = new SolidBrush(ForeColor))
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic)
            {
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces
            })
            {
                for (int i = 0; i < visible; i++)
                {
                    int lineIndex = firstLine + i;
                    if (lineIndex >= m_document.LineCount)
                        break;

                    string text = m_document.GetLineText(lineIndex);

                    g.DrawString(
                        text,
                        m_font,
                        brush,
                        m_nTextPaddingLeft,
                        i * m_nLineHeight,
                        sf);
                }
            }
        }

        private void DrawCaret(Graphics g)
        {
            if (!m_bCaretVisible) return;

            int firstLine = m_vScroll.Value;
            int visible = ClientSize.Height / m_nLineHeight;

            int screenLine = m_cursor.Line - firstLine;
            if (screenLine < 0 || screenLine >= visible)
                return;

            string lineText = m_document.GetLineText(m_cursor.Line);
            int col = Math.Min(m_cursor.Column, lineText.Length);

            float x = m_nTextPaddingLeft;
            if (col > 0)
            {
                string prefix = lineText.Substring(0, col);
                using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic)
                {
                    FormatFlags = StringFormatFlags.MeasureTrailingSpaces
                })
                {
                    x += g.MeasureString(prefix, m_font, int.MaxValue, sf).Width;
                }
            }

            float y = screenLine * m_nLineHeight;

            using (SolidBrush b = new SolidBrush(Color.Black))
            {
                g.FillRectangle(b, x, y, 2, m_nLineHeight);
            }
        }
        // Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (m_document == null || m_vScroll == null)
                return;

            Focus();

            int firstLine = m_vScroll.Value;
            int clickedLine = firstLine + (e.Y / m_nLineHeight);

            if (clickedLine < 0)
                clickedLine = 0;
            if (clickedLine >= m_document.LineCount)
                clickedLine = m_document.LineCount - 1;

            int column = GetColumnFromX(clickedLine, e.X);

            m_cursor.Line = clickedLine;
            m_cursor.Column = column;
            m_cursor.PreferredX = e.X;

            Invalidate();
        }
        private int GetColumnFromX(int lineIndex, int mouseX)
        {
            string text = m_document.GetLineText(lineIndex);
            if (string.IsNullOrEmpty(text))
                return 0;

            int x = m_nTextPaddingLeft;

            if (mouseX <= x)
                return 0;

            using (Graphics g = CreateGraphics())
            using (StringFormat sf = new StringFormat(StringFormat.GenericTypographic)
            {
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces
            })
            {
                string subString;
                for (int i = 0; i < text.Length; i++)
                {
                    subString = text.Substring(0, i + 1);
                    float w = g.MeasureString(
                        subString, 
                        m_font,
                        int.MaxValue,
                        sf).Width;

                    if (mouseX < x + w)
                        return i;
                }
            }

            return text.Length;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (m_document == null || m_vScroll == null)
                return;

            int linesPerNotch = SystemInformation.MouseWheelScrollLines;
            if (linesPerNotch <= 0)
                linesPerNotch = 3;

            int notches = e.Delta / 120;
            int deltaLines = -notches * linesPerNotch;

            ScrollBy(deltaLines);
        }
        private void ScrollBy(int deltaLines)
        {
            if (m_document == null || m_vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int maxValue = Math.Max(0, m_document.LineCount - visible);

            int newValue = m_vScroll.Value + deltaLines;

            if (newValue < 0) newValue = 0;
            if (newValue > maxValue) newValue = maxValue;

            if (newValue != m_vScroll.Value)
            {
                m_vScroll.Value = newValue;
                Invalidate();
            }
        }
        // Keyboard
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
                case Keys.Enter:
                case Keys.Back:
                case Keys.Delete:
                    return true;
            }
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (m_document == null) return;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (m_cursor.Column > 0)
                        m_cursor.Column--;
                    else if (m_cursor.Line > 0)
                    {
                        m_cursor.Line--;
                        m_cursor.Column = m_document.GetLineText(m_cursor.Line).Length;
                    }
                    break;

                case Keys.Right:
                    {
                        int len = m_document.GetLineText(m_cursor.Line).Length;
                        if (m_cursor.Column < len)
                            m_cursor.Column++;
                        else if (m_cursor.Line + 1 < m_document.LineCount)
                        {
                            m_cursor.Line++;
                            m_cursor.Column = 0;
                        }
                    }
                    break;

                case Keys.Up:
                    if (m_cursor.Line > 0)
                        m_cursor.Line--;
                    break;

                case Keys.Down:
                    if (m_cursor.Line + 1 < m_document.LineCount)
                        m_cursor.Line++;
                    break;

                case Keys.Enter:
                    m_document.InsertNewLine(m_cursor.Line, m_cursor.Column);
                    m_cursor.Line++;
                    m_cursor.Column = 0;
                    break;

                case Keys.Back:
                    m_document.Backspace(m_cursor.Line, m_cursor.Column);
                    if (m_cursor.Column > 0)
                        m_cursor.Column--;
                    else if (m_cursor.Line > 0)
                    {
                        m_cursor.Line--;
                        m_cursor.Column = m_document.GetLineText(m_cursor.Line).Length;
                    }
                    break;

                case Keys.Delete:
                    m_document.Delete(m_cursor.Line, m_cursor.Column);
                    break;
            }

            EnsureCaretVisible();
            Invalidate();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (m_document == null) return;

            if (!char.IsControl(e.KeyChar))
            {
                m_document.InsertChar(m_cursor.Line, m_cursor.Column, e.KeyChar);
                m_cursor.Column++;
                Invalidate();
            }
        }

        private void EnsureCaretVisible()
        {
            if (m_vScroll == null) return;

            int visible = Math.Max(1, ClientSize.Height / m_nLineHeight);

            if (m_cursor.Line < m_vScroll.Value)
                m_vScroll.Value = m_cursor.Line;
            else if (m_cursor.Line >= m_vScroll.Value + visible)
                m_vScroll.Value = Math.Min(
                    m_vScroll.Maximum,
                    m_cursor.Line - visible + 1);
        }
    }

}
