using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TextEditer
{
    public partial class ucTabTextBox : UserControl, iTabTextBoxPresenter
    {
        bool m_bSaved = true;
        bool m_bFileSystemChanged = false;
        bool m_bMonitoring = false;

        int m_nTabIndex;

        FormMain fmParent;

        public ucTabTextBox(int nIndex, FormMain parent)
        {
            InitializeComponent();

            m_nTabIndex = nIndex;
            fmParent = parent;
        }

        public bool bSaved
        {
            get { return m_bSaved; }
            set { m_bSaved = value; }
        }
        public bool bMonitoring
        {
            get { return m_bMonitoring; }
            set { m_bMonitoring = value; }
        }
        public int nTabIndex
        {
            get { return m_nTabIndex; }
            set { m_nTabIndex = value; }
        }

        public string sFilePathName
        {
            get
            {
                return fileSystemWatcher.Path + "\\" + fileSystemWatcher.Filter;
            }
            set
            {
                fileSystemWatcher.Path = Path.GetDirectoryName(value);
                fileSystemWatcher.Filter = Path.GetFileName(value);
            }
        }
        public string sMainText
        {
            get
            {
                return rtbTextBox.Text;
            }
            set
            {
                rtbTextBox.Text = value;
            }
        }

        private void rtbTextBox_TextChanged(object sender, EventArgs e)
        {
            m_bSaved = false;
            fmParent.ClearDefaultTab();
        }

        private void fileChanged_apply()
        {
            using (StreamReader reader = new StreamReader(sFilePathName, Encoding.UTF8, true))
            {
                rtbTextBox.Text = reader.ReadToEnd();
            }
        }

        private void fileChanged_NoMonitoring()
        {
            DialogResult messageResult = MessageBox.Show($"\"{fileSystemWatcher.Path}\"\n다른 프로그램에서 파일을 변경했습니다.\n다시 읽어들이시겠습니까?", "다시 읽기", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            switch (messageResult)
            {
                case DialogResult.Yes:
                    fileChanged_apply();
                    break;
                case DialogResult.No:

                    break;
                case DialogResult.Cancel:

                    break;
                default:

                    break;
            }
        }

        private void fileSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            try
            {
                m_bFileSystemChanged = true;

                if(fmParent.GetSelectedTab() == this)
                {
                    if(!m_bMonitoring)
                    {
                        fileChanged_NoMonitoring();
                    }
                    else
                    {
                        fileChanged_apply();
                    }
                    m_bFileSystemChanged = false;
                }
            }
            catch
            {

            }
        }

        public void beSelected()
        {
            if (m_bFileSystemChanged)
            {
                if (m_bMonitoring)
                {
                    fileChanged_apply();
                }
                else
                {
                    fileChanged_NoMonitoring();
                }
                m_bFileSystemChanged = false;
            }
            fmParent.SetBtnMonitoringChecked(m_bMonitoring);
        }
    }
}
