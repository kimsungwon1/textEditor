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
        private ITextBuffer m_TextBuffer;
        private VScrollBar m_vScroll;

        private readonly int m_nLineHeight = 16;
        private readonly Font _font = new Font("Consolas", 10);
        private readonly Brush _textBrush = Brushes.White;
        private TextCursor _cursor;

        private int _verticalPixelOffset;

        Color _caretColor = Color.Black;
        private readonly StringFormat _fmt = new StringFormat(StringFormat.GenericTypographic)
        {
            FormatFlags = StringFormatFlags.MeasureTrailingSpaces
        };
        public VirtualTextEditer()
        {
            BackColor = Color.White;
            ForeColor = Color.Black;
            _cursor = new TextCursor();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            DoubleBuffered = true;

            if (!DesignMode)
            {
                m_TextBuffer = new ITextBuffer();
            }
            SetStyle(ControlStyles.Selectable, true);
            TabStop = true;
            _cursor.Line = 0;
            _cursor.Column = 0;
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (DesignMode)
                return;

            m_vScroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 1,
                LargeChange = 1
            };

            m_vScroll.Scroll += (_, __) => Invalidate();
            Controls.Add(m_vScroll);

            UpdateScrollBar();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (DesignMode || m_TextBuffer == null || m_vScroll == null)
                return;

            RenderText(e.Graphics);

            RenderCaret(e.Graphics);
        }


        private void UpdateScrollBar()
        {
            if (m_vScroll == null || m_TextBuffer == null)
                return;

            int visibleLines = Math.Max(1, ClientSize.Height / m_nLineHeight);
            int totalLines = m_TextBuffer.LineCount;

            m_vScroll.Minimum = 0;

            int max = Math.Max(0, m_TextBuffer.LineCount - visibleLines);

            m_vScroll.LargeChange = visibleLines;
            m_vScroll.Maximum = max + m_vScroll.LargeChange - 1;

            if (m_vScroll.Value > max)
                m_vScroll.Value = max;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateScrollBar();
            Invalidate();
        }
        private void InsertChar(char c)
        {
            m_TextBuffer.InsertChar(ref _cursor, c);
            UpdateScrollBar();
            EnsureCursorVisible();
            Invalidate();
        }

        private void OnDocumentChanged()
        {
            UpdateScrollBar();
            EnsureCursorVisible();
            Invalidate();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);

            // 제어 문자 무시
            if (char.IsControl(e.KeyChar))
                return;

            InsertChar(e.KeyChar);
            Invalidate();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    InsertNewLine();
                    e.Handled = true;
                    break;

                case Keys.Back:
                    Backspace();
                    e.Handled = true;
                    break;

                case Keys.Left:
                    MoveCursorLeft();
                    e.Handled = true;
                    break;

                case Keys.Right:
                    MoveCursorRight();
                    e.Handled = true;
                    break;
            }

            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();

            MoveCursorByMouse(e.Location);
            Invalidate();
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (m_vScroll == null)
                return;

            int linesToScroll = SystemInformation.MouseWheelScrollLines;
            if (linesToScroll <= 0)
                linesToScroll = 3;

            int delta = e.Delta > 0 ? -linesToScroll : linesToScroll;

            int newValue = m_vScroll.Value + delta;
            newValue = Math.Max(m_vScroll.Minimum, Math.Min(newValue, m_vScroll.Maximum));

            m_vScroll.Value = newValue;
            Invalidate();
        }

        private void MoveCursorByMouse(Point pt)
        {
            int line = m_vScroll.Value + pt.Y / m_nLineHeight;

            if (line < 0)
                line = 0;
            if (line >= m_TextBuffer.LineCount)
            {
                line = Math.Max(m_TextBuffer.LineCount - 1, 0);
            }

            int column = GetColumnFromX(line, pt.X);

            _cursor.Line = line;
            _cursor.Column = column;

            EnsureCursorVisible();
        }
        private int GetColumnFromX(int line, int x)
        {
            string text = m_TextBuffer.GetLine(line);
            int column = 0;

            using (var g = CreateGraphics())
            {
                for (int i = 1; i <= text.Length; i++)
                {
                    int width = TextRenderer.MeasureText(
                        text.Substring(0, i),
                        _font,
                        new Size(int.MaxValue, int.MaxValue),
                        TextFormatFlags.NoPadding).Width;

                    if (width >= x)
                    {
                        column = i - 1;
                        break;
                    }
                    column = i;
                }
            }
            return column;
        }

        private int GetXFromColumn(int line, int column)
        {
            string text = m_TextBuffer.GetLine(line);

            if (column <= 0)
                return 0;

            if (column > text.Length)
                column = text.Length;

            string sub = text.Substring(0, column);

            using (var g = CreateGraphics())
            {
                SizeF size = g.MeasureString(sub, _font, int.MaxValue, _fmt);
                return (int)Math.Round(size.Width);
            }
        }

        private void EnsureCursorVisible()
        {
            if (m_vScroll == null)
                return;

            int visibleLines = Math.Max(1,  ClientSize.Height / m_nLineHeight);

            if (_cursor.Line < m_vScroll.Value)
            {
                m_vScroll.Value = _cursor.Line;
            }
            else if (_cursor.Line >= m_vScroll.Value + visibleLines)
            {
                int newValue = _cursor.Line - visibleLines + 1;
                m_vScroll.Value = Math.Min(newValue, m_vScroll.Maximum);
            }
        }

        private void RenderText(Graphics g)
        {
            if (m_TextBuffer == null || m_vScroll == null)
                return;

            g.Clear(BackColor);
            using (Brush _textBrush = new SolidBrush(ForeColor))
            {
                int firstLine = m_vScroll.Value;
                int visibleLines = Math.Max(1, ClientSize.Height / m_nLineHeight);

                for (int i = 0; i < visibleLines; i++)
                {
                    int lineIndex = firstLine + i;
                    if (lineIndex >= m_TextBuffer.LineCount)
                        break;

                    string line = m_TextBuffer.GetLine(lineIndex);
                    g.DrawString(line, _font, _textBrush, new PointF(0, i * m_nLineHeight), _fmt);
                }
            }
        }
        private void RenderCaret(Graphics g)
        {
            if (m_vScroll == null)
                return;

            int visibleLines = ClientSize.Height / m_nLineHeight;

            int caretLineOnScreen = _cursor.Line - m_vScroll.Value;
            if (caretLineOnScreen < 0 || caretLineOnScreen >= visibleLines)
                return;

            int x = GetXFromColumn(_cursor.Line, _cursor.Column);
            int y = caretLineOnScreen * m_nLineHeight;

            using (SolidBrush carpetBrush = new SolidBrush(_caretColor))
            {
                g.FillRectangle(new SolidBrush(_caretColor), x, y, 2, m_nLineHeight);
            }
        }

        private void InsertNewLine()
        {
            m_TextBuffer.InsertNewLine(ref _cursor);
        }
        private void Backspace()
        {
            if(_cursor.Column == 0 && _cursor.Line == 0)
            {
                return;
            }
            m_TextBuffer.Backspace(ref _cursor);
        }
        private void MoveCursorLeft()
        {
            if (_cursor.Column > 0)
                _cursor.Column--;
            else if (_cursor.Line > 0)
            {
                _cursor.Line--;
                _cursor.Column = m_TextBuffer.GetLineLength(_cursor.Line);
            }
        }

        private void MoveCursorRight()
        {
            int len = m_TextBuffer.GetLineLength(_cursor.Line);

            if (_cursor.Column < len)
                _cursor.Column++;
            else if (_cursor.Line + 1 < m_TextBuffer.LineCount)
            {
                _cursor.Line++;
                _cursor.Column = 0;
            }
        }
        public void LoadFile(string path)
        {
            // 1. 기존 버퍼 정리
            m_TextBuffer?.Dispose();

            // 2. 새 버퍼 생성 (지금은 MMF 버퍼)
            ITextBuffer buffer = new ITextBuffer();
            buffer.LoadFromFile(path);

            m_TextBuffer = buffer;

            // 3. 커서 초기화
            _cursor.Line = 0;
            _cursor.Column = 0;

            // 4. 스크롤 초기화
            m_vScroll.Value = 0;
            UpdateScrollBar();

            // 5. 다시 그리기
            Invalidate();
        }
    }
}
