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
using System.Threading;

namespace TextEditer
{
    public partial class ucTabTextBox : UserControl, iTabTextBoxPresenter
    {
        int m_nSaved = 1;
        int m_nMonitoring = 0;
        DateTime m_dateTimeLastWrited;
        string m_sFileFullPath;
        string m_sFileName;

        public ucTabTextBox()
        {
            try
            {
                InitializeComponent();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        public void LoadData(string sFilePath)
        {
            try
            {
                sFileFullPath = sFilePath;

                m_dateTimeLastWrited = File.GetLastWriteTime(sFileFullPath);
                textEditer.LoadFile(sFilePath);

                nSaved = 1;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        public void SaveData(string sFilePath)
        {
            try
            {
                if(string.IsNullOrEmpty(sFilePath))
                {
                    SaveDataAsNewName();
                }
                else
                {
                    // textEditer.Save();
                    m_dateTimeLastWrited = File.GetLastWriteTime(sFilePath);
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
                    sFileFullPath = dl.FileName;
                    nSaved = 1;
                    // textEditer.SaveAs(dl.FileName);
                    
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
                SaveData(sFileFullPath);
            }
        }

        public int nSaved
        {
            get
            {
                return m_nSaved;
            }
            set
            {
                m_nSaved = value;
            }
        }
        public int nMonitoring
        {
            get
            {
                return m_nMonitoring;
            }
            set
            {
                m_nMonitoring = value;
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
        
        private bool FileChanged_Apply()
        {
            try
            {
                // Stream stream = new FileStream(sFileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                // using (StreamReader reader = new StreamReader(stream, true))
                // {
                //     rtbTextBox.Text = reader.ReadToEnd();
                // }
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
                    if (Interlocked.CompareExchange(ref m_nMonitoring, 1, 1) == 1)
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
