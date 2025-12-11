using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditer
{
    public class RichTextBoxExtended : RichTextBox
    {
        public event EventHandler ScrolledToBottom;
        public event EventHandler Scroll;

        private const int WM_SETREDRAW = 0x000B;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_USER = 0x400;
        private const int SB_VERT = 1;
        private const int EM_SETSCROLLPOS = WM_USER + 222;
        private const int EM_GETSCROLLPOS = WM_USER + 221;

        FileStream _fs;
        List<long> _lineOffsets = new List<long>();
        int _totalLines = 0;

        int _visibleLines = 2000;

        [DllImport("user32.dll")]
        public static extern int GetScrollPos(IntPtr hWnd, int nBar);

        [DllImport("user32.dll")]
        private static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);

        public RichTextBoxExtended() : base()
        {
            Scroll += rtbEx_Scrolled;
        }

        public bool IsAtMaxScroll()
        {
            int minScroll;
            int maxScroll;
            GetScrollRange(this.Handle, SB_VERT, out minScroll, out maxScroll);
            Point rtfPoint = Point.Empty;
            SendMessage(this.Handle, EM_GETSCROLLPOS, 0, ref rtfPoint);

            return (rtfPoint.Y + this.ClientSize.Height >= maxScroll);
        }

        public void OpenLargeFile(string path)
        {
            _lineOffsets.Clear();
            _fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

            _lineOffsets.Add(0);

            using (StreamReader reader = new StreamReader(_fs, Encoding.UTF8, true, 4096, leaveOpen: true))
            {
                while (!reader.EndOfStream)
                {
                    reader.ReadLine();
                    _lineOffsets.Add(_fs.Position);
                }
            }

            _totalLines = _lineOffsets.Count - 1;

            LoadChunk(0);

        }

        private void LoadChunk(int startLine)
        {
            if (startLine < 0) startLine = 0;
            if (startLine > _totalLines) startLine = _totalLines - _visibleLines;

            int endLine = Math.Min(startLine + _visibleLines, _totalLines);

            StringBuilder sb = new StringBuilder(5_000_000);

            Point param = new Point();

            SendMessage(this.Handle, WM_SETREDRAW, 0, ref param);

            _fs.Seek(_lineOffsets[startLine], SeekOrigin.Begin);

            using (StreamReader reader = new StreamReader(_fs, Encoding.UTF8, true, 4096, leaveOpen: true))
            {
                for (int i = startLine; i < endLine; i++)
                {
                    string line = reader.ReadLine();
                    if (line == null) break;
                    sb.AppendLine(line);
                }
            }

            Text = sb.ToString();


            SendMessage(this.Handle, WM_SETREDRAW, 1, ref param);
            Refresh();
        }

        private int GetScrollPercent()
        {
            int pos = GetScrollPos(Handle, SB_VERT);
            return pos;
        }

        private void rtbEx_Scrolled(object sender, EventArgs e)
        {
            int percent = GetScrollPercent();
            int startLine = (int)((_totalLines - _visibleLines) * (percent / 1000.0));

            if(IsAtMaxScroll())
            {
                LoadChunk(startLine);
            }
        }

        protected virtual void OnScrolledToBottom(EventArgs e)
        {
            if (ScrolledToBottom != null)
                ScrolledToBottom(this, e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (IsAtMaxScroll())
                OnScrolledToBottom(EventArgs.Empty);

            base.OnKeyUp(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                if (Scroll != null)
                    Scroll(this, EventArgs.Empty);
                if (IsAtMaxScroll())
                    OnScrolledToBottom(EventArgs.Empty);
            }

            base.WndProc(ref m);
        }
    }
}
