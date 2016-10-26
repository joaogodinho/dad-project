using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PupperMaster
{
    public partial class frmPuppetMaster : Form
    {
        public frmPuppetMaster()
        {
            InitializeComponent();
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            Stream stream = null;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                lstScript.Items.Clear();
                try
                {
                    if ((stream = openFileDialog.OpenFile()) != null)
                    {
                        using (stream)
                        {
                            string test = (new StreamReader(stream)).ReadToEnd();
                            foreach (string s in test.Split('\n'))
                            {
                                if (!string.IsNullOrEmpty(s) && !s.StartsWith("%"))
                                {
                                    lstScript.Items.Add(s);
                                }
                            }
                        }
                    }
                    lstScript.SelectedIndex = 0;
                    btnStep.Enabled = true;
                    btnRunScript.Enabled = true;
                    // TODO: Stop whatever is happening (stop all OPs, etc)
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
                }
            }
        }

        private void btnStep_Click(object sender, EventArgs e)
        {
            string itemVal = lstScript.SelectedItem.ToString();
            if (lstScript.Items.Count - 1 == lstScript.SelectedIndex)
            {
                lstScript.SelectedIndex = -1;
            }
            else
            {
                lstScript.SelectedIndex++;
            }

            disableAll();
            // TODO: Run the extracted command
            enableAll();
            // Auto scroll the script
            lstScript.TopIndex = lstScript.SelectedIndex - 2;
        }

        private void disableAll()
        {
            btnLoad.Enabled = false;
            btnStep.Enabled = false;
            btnRunCmd.Enabled = false;
            btnRunScript.Enabled = false;
        }

        public void enableAll()
        {
            btnLoad.Enabled = true;
            btnRunCmd.Enabled = true;
            if (lstScript.SelectedIndex == -1)
            {
                btnStep.Enabled = false;
                btnRunScript.Enabled = false;
            }
            else
            {
                btnStep.Enabled = true;
                btnRunScript.Enabled = true;
            }
        }

        private void btnRunScript_Click(object sender, EventArgs e)
        {
            disableAll();
            // TODO: Run the script
            enableAll();
        }
    }
}
