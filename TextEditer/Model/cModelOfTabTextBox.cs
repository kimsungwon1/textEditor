using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace TextEditer.Model
{
    class cModelOfTabTextBox
    {
        private iTabTextBoxPresenter m_TabTextBoxPresenter;

        bool m_bSaved = true;
        bool m_bFileSystemChanged = false;
        bool m_bMonitoring = false;

        iFormMainPresenter fmParent = null;

        public cModelOfTabTextBox(iTabTextBoxPresenter presenter, int nIndex, iFormMainPresenter parent)
        {
            try
            {
                m_TabTextBoxPresenter = presenter;
                
                fmParent = parent;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        private void RtbTextBox_TextChanged(object sender, EventArgs e)
        {
            m_bSaved = false;
            // fmParent.ClearDefaultTab();
        }
        private void FileChanged_apply()
        {
            // using (StreamReader reader = new StreamReader(m_TabTextBoxPresenter.sFilePathName, Encoding.UTF8, true))
            {
                // m_TabTextBoxPresenter.sMainText = reader.ReadToEnd();
            }
        }
        private void FileChanged_NoMonitoring()
        {
            DialogResult messageResult = DialogResult.OK; ; // = MessageBox.Show($"\"{m_TabTextBoxPresenter.sFilePathName}\"\n다른 프로그램에서 파일을 변경했습니다.\n다시 읽어들이시겠습니까?", "다시 읽기", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            switch (messageResult)
            {
                case DialogResult.Yes:
                    FileChanged_apply();
                    break;
                case DialogResult.No:

                    break;
                default:
                    break;
            }
        }
        private void FileSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {
            try
            {
                m_bFileSystemChanged = true;

                // if (fmParent.GetSelectedTab() == this)
                {
                    if (!m_bMonitoring)
                    {
                        FileChanged_NoMonitoring();
                    }
                    else
                    {
                        FileChanged_apply();
                    }
                    m_bFileSystemChanged = false;
                }
            }
            catch
            {

            }
        }

        public void BeSelected()
        {
            if (m_bFileSystemChanged)
            {
                if (m_bMonitoring)
                {
                    FileChanged_apply();
                }
                else
                {
                    FileChanged_NoMonitoring();
                }
                m_bFileSystemChanged = false;
            }
            // fmParent.SetBtnMonitoringChecked(m_bMonitoring);
        }
    }
}
