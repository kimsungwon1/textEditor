using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TextEditer
{
    public partial class ucTabTextBox : UserControl
    {
        bool m_bSaved = true;
        int m_nTabIndex;
        FormMain parentFormMain = null;
        string m_sFilePathName = "";
        public ucTabTextBox(int nIndex, FormMain parent)
        {
            InitializeComponent();

            m_nTabIndex = nIndex;
            parentFormMain = parent;
        }

        public bool bSaved
        {
            get { return m_bSaved; }
            set { m_bSaved = value; }
        }
        public int nTabIndex
        {
            get { return m_nTabIndex; }
        }

        public string sFilePathName
        {
            get
            {
                return m_sFilePathName;
            }
            set
            {
                m_sFilePathName = value;
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
            parentFormMain.ClearDefaultTab();
        }
    }
}
