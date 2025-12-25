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
        private cDocument _doc;

        private VScrollBar _vScroll;

        private TextCursor _cursor;

        private readonly Font _font = new Font("Consolas", 10);
        private readonly int _lineHeight = 16;

        private readonly Timer _caretTimer;
        private bool _caretVisible = true;

        private const int TextPaddingLeft = 4;

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

            _caretTimer = new Timer { Interval = 500 };
            _caretTimer.Tick += (_, __) =>
            {
                _caretVisible = !_caretVisible;
                Invalidate();
            };
            _caretTimer.Start();
        }

        // 파일 로드 (진입점)
        public void LoadFile(string path)
        {
            var buffer = new ITextBuffer(path);
            var lines = LineIndexer.Scan(buffer);

            _doc = new cDocument(buffer, lines);

            _cursor.Line = 0;
            _cursor.Column = 0;
            _cursor.PreferredX = 0;

            EnsureScroll();
            UpdateScrollBar();

            Invalidate();
        }

        // Scroll
        private void EnsureScroll()
        {
            if (_vScroll != null) return;

            _vScroll = new VScrollBar
            {
                Dock = DockStyle.Right,
                SmallChange = 1
            };
            _vScroll.Scroll += (_, __) => Invalidate();
            Controls.Add(_vScroll);
        }

        private void UpdateScrollBar()
        {
            if (_doc == null || _vScroll == null) return;

            int visible = Math.Max(1, ClientSize.Height / _lineHeight);
            int total = _doc.LineCount;

            int maxFirst = Math.Max(0, total - visible);

            _vScroll.Minimum = 0;
            _vScroll.LargeChange = visible;
            _vScroll.Maximum = maxFirst + visible;
            _vScroll.Value = Math.Min(_vScroll.Value, maxFirst);
        }

        // Paint
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);

            if (_doc == null || _vScroll == null)
                return;

            DrawText(e.Graphics);
            DrawCaret(e.Graphics);
        }

        private void DrawText(Graphics g)
        {
            int firstLine = _vScroll.Value;
            int visible = Math.Max(1, ClientSize.Height / _lineHeight);

            Color textColor = Color.Black;
            using (var brush = new SolidBrush(ForeColor))
            using (var sf = new StringFormat(StringFormat.GenericTypographic)
            {
                FormatFlags = StringFormatFlags.MeasureTrailingSpaces
            })
            {
                for (int i = 0; i < visible; i++)
                {
                    int lineIndex = firstLine + i;
                    if (lineIndex >= _doc.LineCount)
                        break;

                    string text = _doc.GetLineText(lineIndex);

                    g.DrawString(
                        text,
                        _font,
                        brush,
                        TextPaddingLeft,
                        i * _lineHeight,
                        sf);
                }
            }
        }

        private void DrawCaret(Graphics g)
        {
            if (!_caretVisible) return;

            int firstLine = _vScroll.Value;
            int visible = ClientSize.Height / _lineHeight;

            int screenLine = _cursor.Line - firstLine;
            if (screenLine < 0 || screenLine >= visible)
                return;

            string lineText = _doc.GetLineText(_cursor.Line);
            int col = Math.Min(_cursor.Column, lineText.Length);

            float x = TextPaddingLeft;
            if (col > 0)
            {
                string prefix = lineText.Substring(0, col);
                using (var sf = new StringFormat(StringFormat.GenericTypographic)
                {
                    FormatFlags = StringFormatFlags.MeasureTrailingSpaces
                })
                {
                    x += g.MeasureString(prefix, _font, int.MaxValue, sf).Width;
                }
            }

            float y = screenLine * _lineHeight;

            using (var b = new SolidBrush(Color.Black))
            {
                g.FillRectangle(b, x, y, 2, _lineHeight);
            }
        }
        // Mouse
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (_doc == null || _vScroll == null)
                return;

            Focus();

            int firstLine = _vScroll.Value;
            int clickedLine = firstLine + (e.Y / _lineHeight);

            if (clickedLine < 0)
                clickedLine = 0;
            if (clickedLine >= _doc.LineCount)
                clickedLine = _doc.LineCount - 1;

            int column = GetColumnFromX(clickedLine, e.X);

            _cursor.Line = clickedLine;
            _cursor.Column = column;
            _cursor.PreferredX = e.X;

            Invalidate();
        }
        private int GetColumnFromX(int lineIndex, int mouseX)
        {
            string text = _doc.GetLineText(lineIndex);
            if (string.IsNullOrEmpty(text))
                return 0;

            int x = TextPaddingLeft;

            if (mouseX <= x)
                return 0;

            using (Graphics g = CreateGraphics())
            using (var sf = new StringFormat(StringFormat.GenericTypographic)
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
                        _font,
                        int.MaxValue,
                        sf).Width;

                    if (mouseX < x + w/*x + w * 0.5f*/)
                        return i;
                }
            }

            return text.Length;
        }
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);

            if (_doc == null || _vScroll == null)
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
            if (_doc == null || _vScroll == null)
                return;

            int visible = Math.Max(1, ClientSize.Height / _lineHeight);
            int maxValue = Math.Max(0, _doc.LineCount - visible);

            int newValue = _vScroll.Value + deltaLines;

            if (newValue < 0) newValue = 0;
            if (newValue > maxValue) newValue = maxValue;

            if (newValue != _vScroll.Value)
            {
                _vScroll.Value = newValue;
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
            if (_doc == null) return;

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (_cursor.Column > 0)
                        _cursor.Column--;
                    else if (_cursor.Line > 0)
                    {
                        _cursor.Line--;
                        _cursor.Column = _doc.GetLineText(_cursor.Line).Length;
                    }
                    break;

                case Keys.Right:
                    {
                        int len = _doc.GetLineText(_cursor.Line).Length;
                        if (_cursor.Column < len)
                            _cursor.Column++;
                        else if (_cursor.Line + 1 < _doc.LineCount)
                        {
                            _cursor.Line++;
                            _cursor.Column = 0;
                        }
                    }
                    break;

                case Keys.Up:
                    if (_cursor.Line > 0)
                        _cursor.Line--;
                    break;

                case Keys.Down:
                    if (_cursor.Line + 1 < _doc.LineCount)
                        _cursor.Line++;
                    break;

                case Keys.Enter:
                    _doc.InsertNewLine(_cursor.Line, _cursor.Column);
                    _cursor.Line++;
                    _cursor.Column = 0;
                    break;

                case Keys.Back:
                    _doc.Backspace(_cursor.Line, _cursor.Column);
                    if (_cursor.Column > 0)
                        _cursor.Column--;
                    else if (_cursor.Line > 0)
                    {
                        _cursor.Line--;
                        _cursor.Column = _doc.GetLineText(_cursor.Line).Length;
                    }
                    break;

                case Keys.Delete:
                    _doc.Delete(_cursor.Line, _cursor.Column);
                    break;
            }

            EnsureCaretVisible();
            Invalidate();
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            if (_doc == null) return;

            if (!char.IsControl(e.KeyChar))
            {
                _doc.InsertChar(_cursor.Line, _cursor.Column, e.KeyChar);
                _cursor.Column++;
                Invalidate();
            }
        }

        private void EnsureCaretVisible()
        {
            if (_vScroll == null) return;

            int visible = Math.Max(1, ClientSize.Height / _lineHeight);

            if (_cursor.Line < _vScroll.Value)
                _vScroll.Value = _cursor.Line;
            else if (_cursor.Line >= _vScroll.Value + visible)
                _vScroll.Value = Math.Min(
                    _vScroll.Maximum,
                    _cursor.Line - visible + 1);
        }
    }

}
