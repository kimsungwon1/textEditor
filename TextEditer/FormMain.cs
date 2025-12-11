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

namespace TextEditer
{
    public partial class FormMain : Form, iFormMainPresenter
    {
        public FormMain()
        {
            try
            {
                InitializeComponent();
                EventInitialize();

                ucTabTextBox default_Tab = new ucTabTextBox(0, this);
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

        private bool ContainsFile(string sFileName)
        {
            try
            {
                foreach (TabPage tpPage in tcTabControl.TabPages)
                {
                    ucTabTextBox perTab = tpPage.Controls[0] as ucTabTextBox;

                    if(perTab.sFileFullPath == sFileName)
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

        private ucTabTextBox GetTabOfFile(string sFileName)
        {
            try
            {
                for(int nIndex = 0; nIndex < tcTabControl.TabPages.Count; nIndex++)
                {
                    ucTabTextBox perTab = tcTabControl.TabPages[nIndex].Controls[0] as ucTabTextBox;

                    if(perTab.sFileFullPath == sFileName)
                    {
                        return perTab;
                    }
                }
                return null;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
                return null;
            }
        }

        public void SetBtnMonitoringChecked(bool bChecked)
        {
            try
            {
                tsbtnMonitoring.Checked = bChecked;
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        // full Path를 가져온 다음에 이 함수 안에서 fileName으로 나눠서 LoadData를 하도록 하라. 그래야 안에 유저컨트롤 안에 FileName이 들어간다.
        private ucTabTextBox AddNewTab(string sFileFullPath, string sFileContent)
        {
            try
            {
                ucTabTextBox newTabTextBox = new ucTabTextBox(tcTabControl.TabPages.Count, this);
                string sTabName = Path.GetFileName(sFileFullPath);
                newTabTextBox.Dock = DockStyle.Fill;
                newTabTextBox.LoadData(sFileFullPath, sFileContent);
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
                AddNewTab("New Tab", "");
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void AddDefaultTab()
        {
            if(tcTabControl.TabPages.Count == 0)
            {
                ucTabTextBox default_Tab = new ucTabTextBox(0, this);
                default_Tab.Dock = DockStyle.Fill;

                TabPage newTabPage = new TabPage("New Tab");
                newTabPage.Controls.Add(default_Tab);

                tcTabControl.TabPages.Add(newTabPage);

                Text = "New Tab";
            }
        }

        // 탭이 0개 있게 되도 디폴트 탭은 만들어주지 않는다. 그러므로 따로 호출해야 한다.
        private void CloseSelectedTab()
        {
            try
            {
                ucTabTextBox currentTab = GetSelectedTab();

                if (!currentTab.bSaved)
                {
                    DialogResult messageResult = MessageBox.Show($"\"{currentTab.sFileFullPath}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    switch (messageResult)
                    {
                        case DialogResult.Yes:
                            currentTab.SaveData();
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            return;
                        default:
                            return;
                    }
                }
                tcTabControl.TabPages.RemoveAt(tcTabControl.SelectedIndex);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
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
                    CloseSelectedTab();
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
                    using (StreamReader reader = new StreamReader(dl.FileName, Encoding.UTF8, true))
                    {
                        if (ContainsFile(dl.FileName))
                        {
                            return;
                        }

                        ucTabTextBox selectedTab = GetSelectedTab();

                        string sFileName = Path.GetFileName(dl.FileName);

                        // 만약 현재 선택된 탭 뿐이며 그 탭이 세이브 안되고 비어 있으면
                        if (tcTabControl.TabPages.Count == 1 && !selectedTab.bSaved && string.IsNullOrEmpty(selectedTab.sMainText))
                        {
                            tcTabControl.SelectedTab.Text = sFileName;
                            selectedTab.LoadData(dl.FileName, reader.ReadToEnd());

                            this.Text = selectedTab.sFileFullPath;
                        }
                        else
                        {
                            ucTabTextBox newTab = AddNewTab(sFileName, reader.ReadToEnd());

                            this.Text = newTab.sFileFullPath;
                        }
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
                currentTab.SaveData();
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
                currentTab.SaveDataAsNewName();
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

                    perTab.SaveData();
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
                dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
                dl.FilterIndex = 1;
                dl.RestoreDirectory = true;
                if (dl.ShowDialog() == DialogResult.OK)
                {
                    string sFileName = dl.FileName;

                    System.IO.File.Move(selectedTab.sFileFullPath, sFileName);

                    tcTabControl.TabPages[tcTabControl.SelectedIndex].Text = Path.GetFileNameWithoutExtension(sFileName);

                    selectedTab.sFileFullPath = sFileName;
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
                tsbtnMonitoring.Checked = !tsbtnMonitoring.Checked;

                selectedTab.bMonitoring = tsbtnMonitoring.Checked;
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

                this.Text = selectedTab.sFileFullPath;

                selectedTab.BeSelected();
                // 모니터링 버튼 checked 설정
                tsbtnMonitoring.Checked = selectedTab.bMonitoring;

                // DateTime lastWriteTime = File.GetLastWriteTime(selectedTab.sFileFullPath);
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tcTabControl_Deselected(object sender, TabControlEventArgs e)
        {
            try
            {
                ucTabTextBox deselectedTab = e.TabPage.Controls[0] as ucTabTextBox;
                if (deselectedTab == null)
                {
                    return;
                }

                deselectedTab.BeDeselected();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
    }
}
