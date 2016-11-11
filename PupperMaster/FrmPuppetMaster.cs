using CommonCode.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DADStorm.PuppetMaster
{
    public partial class frmPuppetMaster : Form
    {
        private PuppetMaster puppet;

        public frmPuppetMaster()
        {
            InitializeComponent();
            puppet = new PuppetMaster(this);
        }

        public void LogMsg(string message)
        {
            this.txtOutput.AppendText(message + "\r\n");
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
                            foreach (string s in test.Split('\n').Select(p => p.Trim()).ToArray<string>())
                            {
                                if (!string.IsNullOrEmpty(s) && !s.StartsWith("%"))
                                {
                                    if (s.StartsWith("Semantics"))
                                    {
                                        puppet.Semantics = s.Split(' ')[1].ToLower();
                                    }
                                    else if (s.StartsWith("LoggingLevel"))
                                    {
                                        puppet.LoggingLevel = s.Split(' ')[1].ToLower();
                                    }
                                    else if (s.StartsWith("OP"))
                                    {
                                        puppet.ParseAndAddOperator(s);
                                    }
                                    else lstScript.Items.Add(s);
                                }
                            }
                            // File has been parsed, we can now send the configs to the PCS'
                            puppet.SendConfigToPCS();
                        }
                    }
                    lstScript.SelectedIndex = 0;
                    btnStep.Enabled = true;
                    btnRunScript.Enabled = true;

                    // puppet.notifyPCS();
                    // TODO: Stop whatever is happening (stop all OPs, etc)
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: Could not parse the configuration file. Original error: " + ex.Message);
                    throw;
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
            puppet.ParseCommand(itemVal);
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
            IReplica replica = (IReplica) Activator.GetObject(typeof(IReplica), @"tcp://localhost:11000/op");
            replica.Start();
            enableAll();
        }
    }
}
