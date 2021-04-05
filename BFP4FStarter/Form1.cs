using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;

namespace BFP4FStarter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            RefreshProfiles();
            Logger.box = rtb1;

            if (toolStripComboBox1.Items.Count > 0)
                toolStripComboBox1.SelectedIndex = 0;
            if (toolStripComboBox1.Items.Count > 1)
                toolStripComboBox1.SelectedIndex = 1;
        }

        private void RefreshProfiles()
        {
            Profiles.Refresh();
            toolStripComboBox1.Items.Clear();
            foreach (Profile p in Profiles.profiles)
            toolStripComboBox1.Items.Add(p.id + ": " + p.name);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            int n = toolStripComboBox1.SelectedIndex;
            if (n == -1)
                return;
            Profile p = Profiles.profiles[n];
            string args = Resource1.client_startup;
            args = args.Replace("#SESSION#", p.id.ToString());
            args = args.Replace("#PLAYER#", p.name);
            args = args.Replace("#IP#", toolStripTextBox1.Text);
            Helper.RunShell("bfp4f.exe", args);
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            Helper.KillRunningProcesses();
        }

        private void sethostsFileToIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string ip = toolStripTextBox1.Text.Trim();
            if (MessageBox.Show("This will overwrite your current 'hosts' file in 'C:\\Windows\\System32\\drivers\\etc\\', are you sure?", "Security Warning", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                string content = Resource1.template_hosts_file.Replace("#IP#", ip);
                File.WriteAllText("C:\\Windows\\System32\\drivers\\etc\\hosts", content);
                MessageBox.Show("Done.");
            }
        }

        private void syncPlayerProfilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("This will overwrite your local player profiles, are you sure?", "Security Warning", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
            {
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        string ip = toolStripTextBox1.Text.Trim();
                        string xml = client.DownloadString("http://" + ip + "/wv/getProfiles");
                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(xml);
                        XmlNodeList list = xmlDoc.SelectNodes("//profile");
                        string[] oldFiles = Directory.GetFiles("backend\\profiles\\");
                        foreach (string oldFile in oldFiles)
                            File.Delete(oldFile);
                        foreach (XmlNode node in list)
                        {
                            XmlAttribute attr = node.Attributes[0];
                            byte[] tmp = Convert.FromBase64String(node.InnerText);
                            File.WriteAllText(attr.Value, Encoding.Unicode.GetString(tmp));
                        }
                        Profiles.Refresh();
                        RefreshProfiles();
                        if (Profiles.profiles.Count != 0)
                        toolStripComboBox1.SelectedIndex = 0;
                        MessageBox.Show("Done");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading profiles: \n" + ex.Message);
                }
            }
        }
    }
}
