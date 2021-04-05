using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace BFP4FBlazeServer
{
    public static class Config
    {
        private static readonly string Loc = Path.GetDirectoryName(Application.ExecutablePath) + "\\";

        private static readonly object _sync = new object();
        public static List<string> Entries;

        public static string LogLevel = "All";
        public static bool useQOS = false;
        public static bool useWebServer = true;
        public static bool RediSSL = false;
        public static bool useShark = false;
        public static bool useDebug = false;
        public static bool DEDIServer = false;
        public static string IPAddress = "127.0.0.1";


        public static void LoadInitialConfig()
        {
            try
            {
                if (File.Exists(Loc + "conf\\conf.txt"))
                {
                    Entries = new List<string>(File.ReadAllLines(Loc + "conf\\conf.txt"));

                    LogLevel = Config.FindEntry("LogLevel");
                    Logger.Data("LogLevel = " + LogLevel);

                    IPAddress = Config.FindEntry("IPAddress");
                    Logger.Data("IP Address = " + IPAddress);

                    useQOS = Convert.ToBoolean(FindEntry("useQOS"));
                    Logger.Data("Use QOS = " + useQOS);

                    useWebServer = Convert.ToBoolean(FindEntry("useWebServer"));
                    Logger.Data("Use WebServer = " + useWebServer);

                    DEDIServer = Convert.ToBoolean(FindEntry("DEDIServer"));
                    Logger.Data("Auto Start DEDICATED Server = " + DEDIServer);

                    RediSSL = Convert.ToBoolean(FindEntry("RediSSL"));
                    Logger.Data("Redirector SSL Support = " + RediSSL);

                    useShark = Convert.ToBoolean(FindEntry("useShark"));
                    Logger.Data("Use Shark = " + useShark);

                    useDebug = Convert.ToBoolean(FindEntry("Debug"));
                    Logger.Data("Debug Mode = " + useDebug);

                }
                else
                {
                    Logger.Error(@"[Config]" + Loc + " conf\\conf.txt loading failed");
                }
            }
            catch (Exception Ex)
            {
                Logger.Error("LoadInitialConfig error: " + Ex);
            }
        }

        public static string FindEntry(string name)
        {
            string s = "";
            lock (_sync)
            {
                for (int i = 0; i < Entries.Count; i++)
                {
                    string line = Entries[i];
                    if (line.Trim().StartsWith("#"))
                        continue;
                    string[] parts = line.Split('=');
                    if (parts.Length != 2)
                        continue;
                    if (parts[0].Trim().ToLower() == name.ToLower())
                        return parts[1].Trim();
                }
            }
            return s;
        }
        public static string RemoveControlCharacters(string inString)
        {
            if (inString == null) return null;
            StringBuilder newString = new StringBuilder();
            char ch;
            for (int i = 0; i < inString.Length; i++)
            {
                ch = inString[i];
                if (!char.IsControl(ch))
                {
                    newString.Append(ch);
                }
            }
            return newString.ToString();
        }
    }
}
