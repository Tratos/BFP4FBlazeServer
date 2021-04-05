using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;


namespace BFP4FStarter
{
    public static class Helper
    {
        public static void RunShell(string file, string command)
        {
            Process process = new System.Diagnostics.Process();
            ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = file;
            startInfo.Arguments = command;
            process.StartInfo = startInfo;
            process.Start();
        }

        public static void KillRunningProcesses()
        {
            int countClient = 0, countServer = 0;
            foreach (var process in Process.GetProcessesByName("bfp4f"))
            {
                process.Kill();
                countClient++;
            }
            foreach (var process in Process.GetProcessesByName("bfp4f_w32ded"))
            {
                process.Kill();
                countServer++;
            }
            MessageBox.Show("Killed\nClient: " + countClient + "\nServer: " + countServer + "\nProcesses");
        }
    }
}
