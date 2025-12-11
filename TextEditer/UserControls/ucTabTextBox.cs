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
        bool m_bMonitoring = false;
        DateTime m_dateTimeLastWrited;
        string m_sFileFullPath;
        string m_sFileName;

        public ucTabTextBox()
        {
            try
            {
                InitializeComponent();
                EventInitialize();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void EventInitialize()
        {
            this.rtbTextBox.TextChanged += new System.EventHandler(this.rtbTextBox_TextChanged);
        }

        public void LoadData(string sFilePath)
        {
            try
            {
                sFileFullPath = sFilePath;

                m_dateTimeLastWrited = File.GetLastWriteTime(sFileFullPath);
                rtbTextBox.OpenLargeFile(sFilePath); // LoadFile(sFilePath, RichTextBoxStreamType.PlainText);
                bSaved = true;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        public void SaveData()
        {
            try
            {
                if(string.IsNullOrEmpty(sFileFullPath))
                {
                    SaveDataAsNewName();
                }
                else
                {
                    using (StreamWriter streamWriter = new StreamWriter(sFileFullPath, false, Encoding.UTF8))
                    {
                        streamWriter.Write(sMainText);
                        bSaved = true;
                    }
                    m_dateTimeLastWrited = File.GetLastWriteTime(sFileFullPath);
                }
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        public void SaveDataAsNewName()
        {
            try
            {
                SaveFileDialog dl = new SaveFileDialog();
                dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
                dl.FilterIndex = 1;
                dl.RestoreDirectory = true;
                if (dl.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter streamWriter = new StreamWriter(dl.FileName, false, Encoding.UTF8))
                    {
                        streamWriter.Write(sMainText);
                        sFileFullPath = dl.FileName;
                        bSaved = true;
                    }
                    m_dateTimeLastWrited = File.GetLastWriteTime(sFileFullPath);
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        public void Close(bool bSave)
        {
            if (bSave)
            {
                SaveData();
            }
        }

        public bool bSaved
        {
            get
            {
                return m_bSaved;
            }
            set
            {
                m_bSaved = value;
            }
        }
        public bool bMonitoring
        {
            get
            {
                return m_bMonitoring;
            }
            set
            {
                m_bMonitoring = value;
            }
        }
        public DateTime dateTimeLastWrited
        {
            get
            {
                return m_dateTimeLastWrited;
            }
            set
            {
                m_dateTimeLastWrited = value;
            }
        }

        public string sFileFullPath
        {
            get
            {
                return m_sFileFullPath;
            }
            set
            {
                m_sFileFullPath = value;
                m_sFileName = Path.GetFileName(value);
            }
        }

        public string sFileName
        {
            get
            {
                return m_sFileName;
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
            try
            {
                m_bSaved = false;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private bool FileChanged_Apply()
        {
            try
            {
                Stream stream = new FileStream(sFileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using (StreamReader reader = new StreamReader(stream, true))
                {
                    rtbTextBox.Text = reader.ReadToEnd();
                }
                return true;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return false;
            }
        }

        private bool FileChanged_NoMonitoring()
        {
            bool bSuccess = false;
            try
            {
                DialogResult messageResult = MessageBox.Show($"\"{sFileFullPath}\"\n다른 프로그램에서 파일을 변경했습니다.\n다시 읽어들이시겠습니까?", "다시 읽기", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                switch (messageResult)
                {
                    case DialogResult.Yes:
                        bSuccess = FileChanged_Apply();
                        break;
                    case DialogResult.No:

                        break;
                    case DialogResult.Cancel:

                        break;
                    default:

                        break;
                }
                return bSuccess;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return false;
            }
        }

        public void CheckIfFileChanged(DateTime dtTimeLastWrited)
        {
            try
            {
                bool bFileChangeSuccess = false;
                if (m_dateTimeLastWrited != dtTimeLastWrited)
                {
                    if (m_bMonitoring)
                    {
                        bFileChangeSuccess = FileChanged_Apply();
                    }
                    else
                    {
                        bFileChangeSuccess = FileChanged_NoMonitoring();
                    }

                    if(bFileChangeSuccess)
                    {
                        m_dateTimeLastWrited = dtTimeLastWrited;
                    }
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        bool IsMoreBackgroundText()
        {
            return false;
        }
    }
}
