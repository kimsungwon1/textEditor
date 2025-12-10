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
        bool m_bSaved = false;
        bool m_bFileSystemChanged = false;
        bool m_bMonitoring = false;

        FormMain fmParent;

        public ucTabTextBox(int nIndex, FormMain parent)
        {
            try
            {
                InitializeComponent();
                EventInitialize();
                
                fmParent = parent;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void EventInitialize()
        {
            this.rtbTextBox.TextChanged += new System.EventHandler(this.rtbTextBox_TextChanged);
            this.fileSystemWatcher.Changed += new System.IO.FileSystemEventHandler(this.fileSystemWatcher_Changed);
        }

        public void LoadData(string sFilePath, string sFileContent)
        {
            sFilePathName = sFilePath;
            sMainText = sFileContent;
            bSaved = true;
        }
        public void SaveData()
        {
            try
            {
                if(string.IsNullOrEmpty(sFilePathName))
                {
                    SaveDataAsNewName();
                }
                else
                {
                    using (StreamWriter streamWriter = new StreamWriter(sFilePathName, false, Encoding.UTF8))
                    {
                        streamWriter.Write(sMainText);
                        bSaved = true;
                    }
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
                        sFilePathName = dl.FileName;
                        bSaved = true;
                    }
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
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
            try
            {
                m_bSaved = false;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void fileChanged_apply()
        {
            try
            {
                using (StreamReader reader = new StreamReader(sFilePathName, Encoding.UTF8, true))
                {
                    rtbTextBox.Text = reader.ReadToEnd();
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void fileChanged_NoMonitoring()
        {
            try
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
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
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
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        public void BeSelected()
        {
            try
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
                // fileSystemWatcher.EnableRaisingEvents = true;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        public void BeDeselected()
        {
            try
            {
                // fileSystemWatcher.EnableRaisingEvents = false;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
    }
}
