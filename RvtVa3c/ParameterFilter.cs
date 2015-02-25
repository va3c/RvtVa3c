using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RvtVa3c
{
    public partial class ParameterFilter : Form
    {
        public ParameterFilter()
        {
            InitializeComponent();
        }

        private bool areAllChecked=true;

        private void checkUncheck_Click(object sender, EventArgs e)
        {
            string thisTab=Command.tabControl.SelectedTab.Name;
            int index = Command.tabControl.SelectedIndex;

            CheckedListBox currentCheckList = (CheckedListBox)((TabPage)(Command.tabControl.GetControl(index))).Controls[0];

            if (!areAllChecked)
            {
                for (int i = 0; i <= (currentCheckList.Items.Count - 1); i++)
                {
                    currentCheckList.SetItemCheckState(i, CheckState.Checked);
                }
                areAllChecked = true;
            }
            else
            {
                for (int i = 0; i <= (currentCheckList.Items.Count - 1); i++)
                {
                    currentCheckList.SetItemCheckState(i, CheckState.Unchecked);
                }
                areAllChecked = false;

            }
            
        }

        private void export_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
