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
    public partial class FormMain : Form
    {
        ucTabTextBox defaultTab = null;
        Dictionary<string, int> dicOpenedFilesAndIndex;

        public FormMain()
        {
            InitializeComponent();

            dicOpenedFilesAndIndex = new Dictionary<string, int>();

            ucTabTextBox default_Tab = new ucTabTextBox(0, this);
            default_Tab.Dock = DockStyle.Fill;
            defaultTab = default_Tab;
            defaultTabPage.Controls.Add(default_Tab);
        }
        public void ClearDefaultTab()
        {
            defaultTab = null;
        }

        private ucTabTextBox AddNewTab(string sTabName)
        {
            ucTabTextBox newTabTextBox = new ucTabTextBox(tcTabControl.TabPages.Count - 1, this);
            newTabTextBox.Dock = DockStyle.Fill;
            TabPage newTabPage = new TabPage();
            newTabPage.Text = sTabName;
            newTabPage.Controls.Add(newTabTextBox);
            tcTabControl.TabPages.Add(newTabPage);

            tcTabControl.SelectedTab = newTabPage;

            return newTabTextBox;
        }

        private void tsMenuItemNewFile_Click(object sender, EventArgs e)
        {
            AddNewTab("New Tab");
            ClearDefaultTab();
        }

        private void tsMenuItemClose_Click(object sender, EventArgs e)
        {
            ucTabTextBox currentTab = GetSelectedTab();

            int nCurrentTabIndex = currentTab.nTabIndex;

            if(!currentTab.bSaved)
            {
                DialogResult messageResult = MessageBox.Show($"\"{currentTab.sFilePathName}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                switch(messageResult)
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

            if(tcTabControl.TabPages.Count == 0)
            {
                ucTabTextBox default_Tab = new ucTabTextBox(0, this);
                default_Tab.Dock = DockStyle.Fill;
                defaultTab = default_Tab;

                TabPage newTabPage = new TabPage("New Tab");
                newTabPage.Controls.Add(default_Tab);

                tcTabControl.TabPages.Add(newTabPage);
            }
        }

        private void tsMenuItemCloseAll_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < tcTabControl.TabPages.Count; i++)
            {
                ucTabTextBox perTab = GetTabByIndex(i);

                if(!perTab.bSaved)
                {
                    DialogResult messageResult = MessageBox.Show($"\"{perTab.sFilePathName}\"에 대한 변경 내용을 저장할까요?", "저장", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                }

                dicOpenedFilesAndIndex.Remove(perTab.sFilePathName);
            }
            tcTabControl.TabPages.Clear();

            ucTabTextBox default_Tab = new ucTabTextBox(0, this);
            default_Tab.Dock = DockStyle.Fill;
            defaultTab = default_Tab;

            TabPage newTabPage = new TabPage();
            newTabPage.Controls.Add(default_Tab);

            tcTabControl.TabPages.Add(newTabPage);
        }

        private ucTabTextBox GetSelectedTab()
        {
            ucTabTextBox currentTab = tcTabControl.SelectedTab.Controls[0] as ucTabTextBox;
            return currentTab;
        }

        private ucTabTextBox GetTabByIndex(int nIndex)
        {
            ucTabTextBox currentTab = tcTabControl.TabPages[nIndex].Controls[0] as ucTabTextBox;
            return currentTab;
        }

        private void tsMenuItemOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dl = new OpenFileDialog();
            dl.InitialDirectory = "c:\\";
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

                    string sFileName = Path.GetFileNameWithoutExtension(dl.FileName);
                    if (defaultTab == null)
                    {
                        ucTabTextBox newTabTextBox = AddNewTab(sFileName);

                        newTabTextBox.sMainText = reader.ReadToEnd();
                        newTabTextBox.sFilePathName = sFileName;

                        nTabIndex = newTabTextBox.nTabIndex;
                    }
                    else
                    {
                        defaultTab.sFilePathName = dl.FileName;
                        defaultTab.Text = sFileName;

                        nTabIndex = defaultTab.nTabIndex;
                        tcTabControl.TabPages[nTabIndex].Text = sFileName;

                        defaultTab.sMainText = reader.ReadToEnd();
                    }

                    dicOpenedFilesAndIndex.Add(dl.FileName, nTabIndex);
                }
            }
        }

        private void SaveTab(ucTabTextBox tabToSave)
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

        private void tsMenuItemSave_Click(object sender, EventArgs e)
        {
            ucTabTextBox currentTab = GetSelectedTab();
            SaveTab(currentTab);
        }

        private void tsMenuItemSaveAs_Click(object sender, EventArgs e)
        {
            ucTabTextBox currentTab = GetSelectedTab();
            SaveAs(currentTab);
            ClearDefaultTab();
        }

        private DialogResult SaveAs(ucTabTextBox tabToSave)
        {
            if (tabToSave == null)
            {
                return DialogResult.No;
            }

            SaveFileDialog dl = new SaveFileDialog();
            dl.InitialDirectory = "c:\\";
            dl.Filter = "txt files (*.txt)|*.txt;*.log|All files (*.*)|*.*";
            dl.FilterIndex = 1;
            dl.RestoreDirectory = true;
            if (dl.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter streamWriter = new StreamWriter(dl.FileName, false, Encoding.UTF8))
                {
                    streamWriter.Write(tabToSave.sMainText);
                    tabToSave.sFilePathName = dl.FileName;
                    dicOpenedFilesAndIndex.Add(dl.FileName, tabToSave.nTabIndex);

                    return DialogResult.OK;
                }
            }
            return DialogResult.No;
        }

        private void tsMenuItemSaveAll_Click(object sender, EventArgs e)
        {
            foreach(TabPage tpPage in tcTabControl.TabPages)
            {
                ucTabTextBox perTab = tpPage.Controls[0] as ucTabTextBox;
                
                if(perTab == null)
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

        private void tsMenuItemFileNameChange_Click(object sender, EventArgs e)
        {
            ucTabTextBox selectedTab = GetSelectedTab();
            SaveFileDialog dl = new SaveFileDialog();
            dl.InitialDirectory = "c:\\";
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
    }
}
