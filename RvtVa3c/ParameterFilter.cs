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


        public static string status = "";
        private bool changeAll = false;
        /// <summary>
        /// Function to check or uncheck all the checkboxes in a tab
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkUncheck_Click(object sender, EventArgs e)
        {
            
            int index = Command._tabControl.SelectedIndex;
            CheckedListBox currentCheckList = new CheckedListBox();
            if (!changeAll)
            {
                currentCheckList = (CheckedListBox)((TabPage)(Command._tabControl.GetControl(index))).Controls[0];

                bool areAllChecked = true;
                if (currentCheckList.CheckedItems.Count < currentCheckList.Items.Count) areAllChecked = false;
                checkUncheckBoxes(currentCheckList, areAllChecked);
            }
            else
            {
                List<CheckedListBox> allCheckLists = new List<CheckedListBox>();
                bool areAllChecked = true;
                
                foreach (TabPage tab in Command._tabControl.TabPages)
                {
                    currentCheckList = (CheckedListBox) tab.Controls[0];
                    if (currentCheckList.CheckedItems.Count < currentCheckList.Items.Count)
                    {
                        areAllChecked = false;
                        break;
                    }
                }

                foreach (TabPage tab in Command._tabControl.TabPages)
                {
                    currentCheckList = (CheckedListBox)tab.Controls[0];
                    checkUncheckBoxes(currentCheckList, areAllChecked);
                }
            }
        }

        private void export_button_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
            status = "cancelled";
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked) changeAll = false;
            else changeAll = true;
        }

        private void checkUncheckBoxes(CheckedListBox currentCheckList, bool areAllChecked)
        {
            if (!areAllChecked)
            {
                for (int i = 0; i <= (currentCheckList.Items.Count - 1); i++)
                {
                    currentCheckList.SetItemCheckState(i, CheckState.Checked);
                }
            }
            else
            {
                for (int i = 0; i <= (currentCheckList.Items.Count - 1); i++)
                {
                    currentCheckList.SetItemCheckState(i, CheckState.Unchecked);
                }
            }
        }
    }
}
