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
        ucTabTextBox defaultTab = null;
        Dictionary<string, int> dicOpenedFilesAndIndex;

        public FormMain()
        {
            try
            {
                InitializeComponent();

                dicOpenedFilesAndIndex = new Dictionary<string, int>();

                ucTabTextBox default_Tab = new ucTabTextBox(0, this);
                default_Tab.Dock = DockStyle.Fill;
                defaultTab = default_Tab;
                defaultTabPage.Controls.Add(default_Tab);
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
        public void ClearDefaultTab()
        {
            defaultTab = null;
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

        private ucTabTextBox AddNewTab(string sTabName)
        {
            try
            {
                ucTabTextBox newTabTextBox = new ucTabTextBox(tcTabControl.TabPages.Count, this);
                newTabTextBox.Dock = DockStyle.Fill;
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
                AddNewTab("New Tab");
                ClearDefaultTab();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void SetAllTabsIndex()
        {
            try
            {
                for (int i = 0; i < tcTabControl.TabPages.Count; i++)
                {
                    ucTabTextBox perTab = GetTabByIndex(i);

                    perTab.nTabIndex = i;
                    dicOpenedFilesAndIndex[perTab.sFilePathName] = i;

                    dicOpenedFilesAndIndex.Remove(perTab.sFilePathName);
                }
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tsMenuItemClose_Click(object sender, EventArgs e)
        {
            try
            {
                ucTabTextBox currentTab = GetSelectedTab();

                int nCurrentTabIndex = currentTab.nTabIndex;

                if (!currentTab.bSaved)
                {
                    DialogResult messageResult = MessageBox.Show($"\"{currentTab.sFilePathName}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    switch (messageResult)
                    {
                        case DialogResult.Yes:
                            SaveTab(currentTab);
                            break;
                        case DialogResult.No:
                            break;
                        case DialogResult.Cancel:
                            return;
                        default:
                            return;
                    }
                }
                tcTabControl.TabPages.RemoveAt(nCurrentTabIndex);
                dicOpenedFilesAndIndex.Remove(currentTab.sFilePathName);
                SetAllTabsIndex();

                // 반드시 있는 default Tab
                if (tcTabControl.TabPages.Count == 0)
                {
                    ucTabTextBox default_Tab = new ucTabTextBox(0, this);
                    default_Tab.Dock = DockStyle.Fill;
                    defaultTab = default_Tab;

                    TabPage newTabPage = new TabPage("New Tab");
                    newTabPage.Controls.Add(default_Tab);

                    tcTabControl.TabPages.Add(newTabPage);
                }
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
                for (int i = 0; i < tcTabControl.TabPages.Count; i++)
                {
                    ucTabTextBox perTab = GetTabByIndex(i);

                    if (!perTab.bSaved)
                    {
                        DialogResult messageResult = MessageBox.Show($"\"{perTab.sFilePathName}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    }

                    dicOpenedFilesAndIndex.Remove(perTab.sFilePathName);
                }
                tcTabControl.TabPages.Clear();

                // 반드시 있는 default Tab
                ucTabTextBox default_Tab = new ucTabTextBox(0, this);
                default_Tab.Dock = DockStyle.Fill;
                defaultTab = default_Tab;

                TabPage newTabPage = new TabPage("New Tab");
                newTabPage.Controls.Add(default_Tab);

                tcTabControl.TabPages.Add(newTabPage);
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
                        int nTabIndex = 0;
                        if (dicOpenedFilesAndIndex.ContainsKey(dl.FileName))
                        {
                            return;
                        }

                        string sFileName = Path.GetFileName(dl.FileName);
                        if (defaultTab == null)
                        {
                            ucTabTextBox newTabTextBox = AddNewTab(sFileName);

                            newTabTextBox.sMainText = reader.ReadToEnd();
                            newTabTextBox.sFilePathName = dl.FileName;
                            newTabTextBox.bSaved = true;

                            nTabIndex = newTabTextBox.nTabIndex;
                        }
                        else
                        {
                            ucTabTextBox tmpDefaultTab = defaultTab;
                            defaultTab.sMainText = reader.ReadToEnd();
                            defaultTab = tmpDefaultTab;

                            defaultTab.sFilePathName = dl.FileName;
                            defaultTab.Text = sFileName;
                            defaultTab.bSaved = true;

                            nTabIndex = defaultTab.nTabIndex;
                            tcTabControl.TabPages[nTabIndex].Text = sFileName;
                            ClearDefaultTab();
                        }

                        dicOpenedFilesAndIndex.Add(dl.FileName, nTabIndex);
                    }
                }
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void SaveTab(ucTabTextBox tabToSave)
        {
            try
            {
                if (tabToSave == null)
                {
                    return;
                }

                DialogResult dlResult = DialogResult.OK;
                if (string.IsNullOrEmpty(tabToSave.sFilePathName))
                {
                    dlResult = SaveAs(tabToSave);
                }

                if (dlResult != DialogResult.OK)
                {
                    return;
                }

                using (StreamWriter streamWriter = new StreamWriter(tabToSave.sFilePathName, false, Encoding.UTF8))
                {
                    streamWriter.Write(tabToSave.sMainText);
                    ClearDefaultTab();
                    tabToSave.bSaved = true;
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
                ucTabTextBox currentTab = GetSelectedTab();
                SaveTab(currentTab);
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
                SaveAs(currentTab);
                ClearDefaultTab();
            }
            catch(Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private DialogResult SaveAs(ucTabTextBox tabToSave)
        {
            try
            {
                if (tabToSave == null)
                {
                    return DialogResult.No;
                }

                SaveFileDialog dl = new SaveFileDialog();
                dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
                dl.FilterIndex = 1;
                dl.RestoreDirectory = true;
                if (dl.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter streamWriter = new StreamWriter(dl.FileName, false, Encoding.UTF8))
                    {
                        streamWriter.Write(tabToSave.sMainText);
                        tabToSave.sFilePathName = dl.FileName;
                        tabToSave.bSaved = true;
                        dicOpenedFilesAndIndex.Add(dl.FileName, tabToSave.nTabIndex);

                        return DialogResult.OK;
                    }
                }
                return DialogResult.No;
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
            return DialogResult.None;
        }

        private void tsMenuItemSaveAll_Click(object sender, EventArgs e)
        {
            try
            {
                foreach (TabPage tpPage in tcTabControl.TabPages)
                {
                    ucTabTextBox perTab = tpPage.Controls[0] as ucTabTextBox;

                    if (perTab == null)
                    {

                    }

                    DialogResult dlResult = DialogResult.OK;
                    if (string.IsNullOrEmpty(perTab.sFilePathName))
                    {
                        dlResult = SaveAs(perTab);
                    }

                    if (dlResult != DialogResult.OK)
                    {
                        return;
                    }

                    using (StreamWriter streamWriter = new StreamWriter(perTab.sFilePathName, false, Encoding.UTF8))
                    {
                        streamWriter.Write(perTab.sMainText);
                        ClearDefaultTab();
                        perTab.bSaved = true;
                    }
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

                    System.IO.File.Move(selectedTab.sFilePathName, sFileName);

                    dicOpenedFilesAndIndex.Remove(selectedTab.sFilePathName);
                    dicOpenedFilesAndIndex.Add(sFileName, selectedTab.nTabIndex);

                    tcTabControl.TabPages[selectedTab.nTabIndex].Text = Path.GetFileNameWithoutExtension(sFileName);

                    selectedTab.sFilePathName = sFileName;
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

        private void tcTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();
                if (selectedTab == null)
                {
                    return;
                }

                selectedTab.beSelected();
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }

        private void tcTabControl_ControlAdded(object sender, ControlEventArgs e)
        {
            try
            {
                ucTabTextBox selectedTab = GetSelectedTab();
                if (selectedTab == null)
                {
                    return;
                }

                selectedTab.beSelected();
            }
            catch (Exception exception)
            {
                cLogger.Instance.AddLog(eLogType.ERROR, exception);
            }
        }
    }
}
