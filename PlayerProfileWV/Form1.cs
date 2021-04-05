﻿using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayerProfileWV
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
        }

        private void RefreshProfiles()
        {
            Profiles.Refresh();
            listBox1.Items.Clear();
            foreach (Profile p in Profiles.profiles)
                listBox1.Items.Add(p.ToString());
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Application.Exit();
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            string name = toolStripTextBox1.Text;
            Profiles.Refresh();
            Profiles.Create(name);
            Profiles.Refresh();
            RefreshProfiles();
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            int n = listBox1.SelectedIndex;
            if (n == -1)
                return;
            Profile p = Profiles.profiles[n];
            string path = Profiles.getProfilePath(p.id);
            if (File.Exists(path))
                File.Delete(path);
            Profiles.Refresh();
            RefreshProfiles();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            RefreshProfiles();
        }
    }
}
