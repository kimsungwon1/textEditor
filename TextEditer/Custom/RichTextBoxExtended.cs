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
        public event EventHandler Scrolled;

        private const int WM_SETREDRAW = 0x000B;
        private const int WM_VSCROLL = 0x115;
        private const int WM_MOUSEWHEEL = 0x20A;
        private const int WM_USER = 0x400;
        private const int SB_VERT = 1;
        private const int EM_SETSCROLLPOS = WM_USER + 222;
        private const int EM_GETSCROLLPOS = WM_USER + 221;
        private const int EM_GETFIRSTVISIBLELINE = 0xCE;

        FileStream m_fstream;
        List<int> m_ListLineOffsets = new List<int>();
        int m_nTotalLines = 0;

        const int m_nVisibleLines = 2000;

        int m_nStackedLines = 0;
        int m_nStackedOffset = 0;

        [DllImport("user32.dll")]
        private static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, Int32 wMsg, Int32 wParam, ref Point lParam);

        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public RichTextBoxExtended() : base()
        {
            try
            {
                Scrolled += rtbEx_Scrolled;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        public bool IsAtMaxScroll()
        {
            try
            {
                int minScroll;
                int maxScroll;
                GetScrollRange(this.Handle, SB_VERT, out minScroll, out maxScroll);
                Point rtfPoint = Point.Empty;
                SendMessage(this.Handle, EM_GETSCROLLPOS, 0, ref rtfPoint);

                return (rtfPoint.Y + this.ClientSize.Height >= maxScroll);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return false;
            }
        }
        public bool IsAtMinScroll()
        {
            try
            {
                int nFirstLine = SendMessage(this.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);

                return nFirstLine == 0;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return false;
            }
        }

        public void OpenLargeFile(string path)
        {
            try
            {
                m_ListLineOffsets.Clear();
                m_fstream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                m_ListLineOffsets.Add(0);

                using (StreamReader reader = new StreamReader(m_fstream, Encoding.UTF8, true, 4096, leaveOpen: true))
                {
                    int nStack = 0;
                    while (!reader.EndOfStream)
                    {
                        int nCountLine = reader.ReadLine().Count();
                        nStack += nCountLine;
                        m_ListLineOffsets.Add(nStack);
                    }
                }

                m_nTotalLines = m_ListLineOffsets.Count - 1;

                LoadChunk(0);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private int GetLineIndex(int nOffset)
        {
            try
            {
                int nIndex = 0;
                foreach (int nOffsetStart in m_ListLineOffsets)
                {
                    if (nOffset < nOffsetStart)
                    {
                        return nIndex - 1;
                    }
                    nIndex++;
                }
                return nIndex - 1;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return -1;
            }
        }

        private void LoadChunk(int startLine)
        {
            try
            {
                if (startLine < 0) startLine = 0;
                if (startLine > m_nTotalLines) startLine = m_nTotalLines - m_nVisibleLines;

                int endLine = Math.Min(startLine + m_nVisibleLines, m_nTotalLines);

                StringBuilder sb = new StringBuilder(5_000_000);

                Point param = new Point();

                SendMessage(this.Handle, WM_SETREDRAW, 0, ref param);

                int nStartLineOffset = m_ListLineOffsets[startLine];

                m_fstream.Seek(nStartLineOffset, SeekOrigin.Begin);

                if (startLine != 0)
                {
                    m_nStackedOffset = nStartLineOffset;
                }

                using (StreamReader reader = new StreamReader(m_fstream, Encoding.UTF8, true, 4096, leaveOpen: true))
                {
                    for (int i = startLine; i < endLine; i++)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        sb.AppendLine(line);
                    }
                    m_nStackedLines += (endLine - startLine);
                }

                Text = sb.ToString();

                SendMessage(this.Handle, WM_SETREDRAW, 1, ref param);
                Refresh();

                int nTmpSelectedStart = SelectionStart;
                int nTmpSelectedLength = SelectionLength;

                int nStartLineIndex = GetFirstCharIndexFromLine(0);
                Select(nStartLineIndex, 0);
                ScrollToCaret();

                Select(nTmpSelectedStart, nTmpSelectedLength);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        void LoadChunk_back(int backLine)
        {
            try
            {
                int startLine = 0;
                if (backLine < m_nVisibleLines)
                {
                    startLine = 0;
                }
                else
                {
                    startLine = backLine - m_nVisibleLines;
                }

                StringBuilder sb = new StringBuilder(5_000_000);

                Point param = new Point();

                SendMessage(this.Handle, WM_SETREDRAW, 0, ref param);

                int nStartLineOffset = m_ListLineOffsets[startLine];

                m_fstream.Seek(nStartLineOffset, SeekOrigin.Begin);


                m_nStackedOffset = nStartLineOffset;


                using (StreamReader reader = new StreamReader(m_fstream, Encoding.UTF8, true, 4096, leaveOpen: true))
                {
                    for (int i = startLine; i < backLine; i++)
                    {
                        string line = reader.ReadLine();
                        if (line == null) break;
                        sb.AppendLine(line);
                    }
                    m_nStackedLines -= (backLine - startLine);
                }

                Text = sb.ToString();

                SendMessage(this.Handle, WM_SETREDRAW, 1, ref param);
                Refresh();

                int nTmpSelectedStart = SelectionStart;
                int nTmpSelectedLength = SelectionLength;

                int nStartLineIndex = GetFirstCharIndexFromLine(Lines.Length - 1);
                Select(nStartLineIndex, 0);
                ScrollToCaret();

                Select(nTmpSelectedStart, nTmpSelectedLength);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private int GetFirstVisibleLine()
        {
            try
            {
                return SendMessage(this.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return -1;
            }
        }

        private void rtbEx_Scrolled(object sender, EventArgs e)
        {
            try
            {
                if (IsAtMaxScroll())
                {
                    int startLine;
                    int nFirstVisibleLine = GetFirstVisibleLine();
                    int nFirstVisibleTextIndex = GetFirstCharIndexFromLine(nFirstVisibleLine);

                    startLine = GetLineIndex(nFirstVisibleTextIndex + m_nStackedOffset);

                    LoadChunk(startLine);
                }
                else if (IsAtMinScroll() && GetLineIndex(m_nStackedOffset) != 0)
                {
                    int backLine;
                    int nFirstVisibleLine = GetFirstVisibleLine();
                    int nFirstVisibleTextIndex = GetFirstCharIndexFromLine(nFirstVisibleLine);

                    backLine = GetLineIndex(m_nStackedOffset);

                    LoadChunk_back(backLine);
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
                {
                    int nCurrentLine = SendMessage(this.Handle, EM_GETFIRSTVISIBLELINE, 0, 0);
                    
                    if (Scrolled != null)
                        Scrolled(this, EventArgs.Empty);
                }

                base.WndProc(ref m);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
    }
}
