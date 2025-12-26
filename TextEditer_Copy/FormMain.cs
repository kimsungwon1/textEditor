using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace TextEditer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            try
            {
                InitializeComponent();
                EventInitialize();

                ucTabTextBox default_Tab = new ucTabTextBox();
                default_Tab.Dock = DockStyle.Fill;
                defaultTabPage.Controls.Add(default_Tab);
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void EventInitialize()
        {
            this.tsMenuItemNewFile.Click += new System.EventHandler(this.tsMenuItemNewFile_Click);
            this.tsMenuItemOpen.Click += new System.EventHandler(this.tsMenuItemOpen_Click);
            this.tsMenuItemSave.Click += new System.EventHandler(this.tsMenuItemSave_Click);
            this.tsMenuItemSaveAs.Click += new System.EventHandler(this.tsMenuItemSaveAs_Click);
            this.tsMenuItemSaveAll.Click += new System.EventHandler(this.tsMenuItemSaveAll_Click);
            this.tsMenuItemFileNameChange.Click += new System.EventHandler(this.tsMenuItemFileNameChange_Click);
            this.tsMenuItemClose.Click += new System.EventHandler(this.tsMenuItemClose_Click);
            this.tsMenuItemCloseAll.Click += new System.EventHandler(this.tsMenuItemCloseAll_Click);
            this.tsbtnMonitoring.Click += new System.EventHandler(this.tsbtnMonitoring_Click);
        }

        private string GetNewTabName()
        {
            int nIndex = 1;
            string sDefaultName = "New Tab";
            string sNewTabName = sDefaultName + nIndex.ToString();
            while(ContainsTabName(sNewTabName))
            {
                nIndex++;
                sNewTabName = sDefaultName + nIndex.ToString();
            }
            return sNewTabName;
        }

        private bool ContainsTabName(string sTabName)
        {
            try
            {
                foreach (TabPage tpPage in tcTabControl.TabPages)
                {
                    if (tpPage.Text == sTabName)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return true;
            }
        }

        private bool ContainsFile(string sFilePath)
        {
            try
            {
                foreach (TabPage tpPage in tcTabControl.TabPages)
                {
                    ucTabTextBox perTab = tpPage.Controls[0] as ucTabTextBox;

                    if(perTab.sFileFullPath == sFilePath)
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return true;
            }
        }

        // 완전 새로운 탭 추가
        private ucTabTextBox AddNewTab()
        {
            try
            {
                ucTabTextBox newTabTextBox = new ucTabTextBox();
                newTabTextBox.Dock = DockStyle.Fill;
                TabPage newTabPage = new TabPage();
                newTabPage.Text = GetNewTabName();
                newTabPage.Controls.Add(newTabTextBox);
                tcTabControl.TabPages.Add(newTabPage);

                tcTabControl.SelectedTab = newTabPage;

                return newTabTextBox;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return null;
            }
        }

        // full Path를 가져온 다음에 이 함수 안에서 fileName으로 나눠서 LoadData를 하도록 한다.
        private ucTabTextBox AddNewTab(string sFileFullPath)
        {
            try
            {
                ucTabTextBox newTabTextBox = new ucTabTextBox();
                string sTabName = Path.GetFileName(sFileFullPath);
                newTabTextBox.Dock = DockStyle.Fill;
                newTabTextBox.LoadData(sFileFullPath);

                TabPage newTabPage = new TabPage();
                newTabPage.Text = sTabName;
                newTabPage.Controls.Add(newTabTextBox);
                tcTabControl.TabPages.Add(newTabPage);

                tcTabControl.SelectedTab = newTabPage;

                return newTabTextBox;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
            return null;
        }

        private void tsMenuItemNewFile_Click(object sender, EventArgs e)
        {
            try
            {
                AddNewTab();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void AddDefaultTab()
        {
            try
            {
                if (tcTabControl.TabPages.Count == 0)
                {
                    AddNewTab();
                }
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        // 탭이 0개 있게 되도 디폴트 탭은 만들어주지 않는다. 그러므로 따로 호출해야 한다.
        private DialogResult CloseSelectedTab()
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();
                DialogResult messageResult = DialogResult.Yes;
                if (selectedTab.nSaved == 0)
                {
                    messageResult = MessageBox.Show($"\"{tcTabControl.SelectedTab.Text}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    switch (messageResult)
                    {
                        case DialogResult.Yes:
                            selectedTab.Close(true);
                            break;
                        case DialogResult.No:
                            selectedTab.Close(false);
                            break;
                        default:
                            return DialogResult.Cancel;
                    }
                }
                tcTabControl.TabPages.RemoveAt(tcTabControl.SelectedIndex);
                return messageResult;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);

                return DialogResult.Cancel;
            }
        }

        private void tsMenuItemClose_Click(object sender, EventArgs e)
        {
            try
            {
                CloseSelectedTab();

                // 반드시 있는 default Tab
                AddDefaultTab();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemCloseAll_Click(object sender, EventArgs e)
        {
            try
            {
                while(tcTabControl.TabPages.Count != 0)
                {
                    DialogResult result = CloseSelectedTab();
                    if(result == DialogResult.Cancel)
                    {
                        break;
                    }
                }

                // 반드시 있는 default Tab
                AddDefaultTab();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        public ucTabTextBox GetSelectedTab()
        {
            try
            {
                if (tcTabControl.SelectedTab == null)
                {
                    return null;
                }
                ucTabTextBox currentTab = tcTabControl.SelectedTab.Controls[0] as ucTabTextBox;
                return currentTab;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
            return null;
        }

        private ucTabTextBox GetTabByIndex(int nIndex)
        {
            try
            {
                ucTabTextBox currentTab = tcTabControl.TabPages[nIndex].Controls[0] as ucTabTextBox;
                return currentTab;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
            return null;
        }

        private bool IsCurrentTab_DefaultOne()
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();

                return tcTabControl.TabPages.Count == 1 && selectedTab.nSaved == 1 && true /* string.IsNullOrEmpty(selectedTab.sMainText) */ && string.IsNullOrEmpty(selectedTab.sFileFullPath);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return true;
            }
        }

        private void tsMenuItemOpen_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dl = new OpenFileDialog();
                dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
                dl.FilterIndex = 1;
                dl.RestoreDirectory = true;
                if (dl.ShowDialog() == DialogResult.OK)
                {
                    if (ContainsFile(dl.FileName))
                    {
                        return;
                    }

                    ucTabTextBox selectedTab = GetSelectedTab();

                    string sFileName = Path.GetFileName(dl.FileName);

                    // 만약 현재 선택된 탭 뿐이며 그 탭이 세이브 되고 비어 있으면
                    if (IsCurrentTab_DefaultOne())
                    {
                        tcTabControl.SelectedTab.Text = sFileName;
                        selectedTab.LoadData(dl.FileName);

                        fileSystemWatcher.Path = Path.GetDirectoryName(dl.FileName);
                        fileSystemWatcher.Filter = sFileName;

                        this.Text = tcTabControl.TabPages[0].Text;
                    }
                    else
                    {
                        ucTabTextBox newTab = AddNewTab(dl.FileName);

                        this.Text = tcTabControl.TabPages[tcTabControl.TabCount - 1].Text;
                    }
                }
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemSave_Click(object sender, EventArgs e)
        {
            try
            {
                int nTabIndex = tcTabControl.SelectedIndex;
                ucTabTextBox currentTab = GetSelectedTab();
                TabPage tabPage = tcTabControl.SelectedTab;

                currentTab.SaveData(currentTab.sFileName);

                tabPage.Text = currentTab.sFileName;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemSaveAs_Click(object sender, EventArgs e)
        {
            try
            {
                ucTabTextBox currentTab = GetSelectedTab();
                TabPage tabPage = tcTabControl.SelectedTab;

                currentTab.SaveDataAsNewName();

                tabPage.Text = currentTab.sFileName;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemSaveAll_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (TabPage tpPage in tcTabControl.TabPages)
                {
                    ucTabTextBox perTab = tpPage.Controls[0] as ucTabTextBox;

                    perTab.SaveData(perTab.sFileName);

                    tpPage.Text = perTab.sFileName;
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemFileNameChange_Click(object sender, EventArgs e)
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();
                SaveFileDialog dl = new SaveFileDialog();

                if (string.IsNullOrEmpty(selectedTab.sFileFullPath))
                {
                    return;
                }

                dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
                dl.FilterIndex = 1;
                dl.RestoreDirectory = true;
                if (dl.ShowDialog() == DialogResult.OK)
                {
                    string sFileName = dl.FileName;

                    System.IO.File.Move(selectedTab.sFileFullPath, sFileName);

                    tcTabControl.TabPages[tcTabControl.SelectedIndex].Text = Path.GetFileNameWithoutExtension(sFileName);

                    selectedTab.sFileFullPath = sFileName;

                    selectedTab.dateTimeLastWrited = File.GetLastWriteTime(selectedTab.sFileFullPath);
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsbtnMonitoring_Click(object sender, EventArgs e)
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();

                if (string.IsNullOrEmpty(selectedTab.sFileFullPath))
                {
                    return;
                }

                tsbtnMonitoring.Checked = !tsbtnMonitoring.Checked;

                if (tsbtnMonitoring.Checked)
                {
                    selectedTab.nMonitoring = 1;
                }
                else
                {
                    selectedTab.nMonitoring = 0;
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tcTabControl_Selected(object sender, TabControlEventArgs e)
        {
            try
            {
                ucTabTextBox selectedTab;
                if(e.TabPage == null || e.TabPage.Controls[0] == null)
                {
                    tsbtnMonitoring.Checked = false;
                    return;
                }

                selectedTab = e.TabPage.Controls[0] as ucTabTextBox;

                this.Text = e.TabPage.Text;

                // 모니터링 버튼 checked 설정
                tsbtnMonitoring.Checked = (selectedTab.nMonitoring == 1);

                // DateTime 설정
                if (!string.IsNullOrEmpty(selectedTab.sFileFullPath))
                {
                    DateTime dTimeLastWrited = File.GetLastWriteTime(selectedTab.sFileFullPath);

                    selectedTab.CheckIfFileChanged(dTimeLastWrited);

                    fileSystemWatcher.Path = Path.GetDirectoryName(selectedTab.sFileFullPath);
                    fileSystemWatcher.Filter = selectedTab.sFileName;
                }
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private int m_nHandling = 0;

        private void fileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (Interlocked.Exchange(ref m_nHandling, 1) == 1)
                return;
            BeginInvoke(new Action(() =>
            {
                try
                {
                    HandleFileChanged(e.FullPath);
                }
                finally
                {
                    Interlocked.Exchange(ref m_nHandling, 0);
                }
            }));
            /*try
            {
                fileSystemWatcher.EnableRaisingEvents = false;

                ucTabTextBox selectedTab = GetSelectedTab();

                // DateTime 설정
                DateTime dTimeLastWrited = File.GetLastWriteTime(selectedTab.sFileFullPath);

                selectedTab.CheckIfFileChanged(dTimeLastWrited);
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
            finally
            {
                fileSystemWatcher.EnableRaisingEvents = true;
            }*/
        }
        private void HandleFileChanged(string sPath)
        {
            ucTabTextBox selectedTab = GetSelectedTab();

            if (selectedTab == null)
                return;

            DateTime lastWrite = File.GetLastWriteTime(sPath);

            selectedTab.CheckIfFileChanged(lastWrite);
        }
    }
}
