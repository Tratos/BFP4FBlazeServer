using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BFP4FBlazeServer
{
    class Program
    {
        static readonly string Title = "Battlefield Play4Free BlazeServer";
        public static bool stop = true;

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.Title = Title;

            Logger.Data(@"Battlefield - Play4Free BlazeServer");
            Logger.Data(@"");
            Logger.Data(@"Migration (c) 2021 by Eisbaer");
            Logger.Data(@"");
            Logger.Data(@"Code by Warranty Voider");
            Logger.Data(@"");
            Logger.Data(@"Version: " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
            Logger.Data(@"");

            try
            {
                Config.LoadInitialConfig();
            }
            catch (Exception ex)
            {
                Logger.Error("[MAIN] Failed to load the config: {0}" + ex);
                return;
            }

            try
            {
                Logger.Initialize("BackendLog.txt", LogLevel.All, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
                return;
            }

            try
            {
                StartServers();
            }
            catch (Exception ex)
            {
                Logger.Error("[MAIN] Failed to start the servers: {0}" + ex);
                return;
            }

            while (stop)
            {
                string[] cmdstr = Console.ReadLine().Split(' ');
                switch (cmdstr[0])
                {
                    case "exit":
                        Exit();
                        break;
                    default:
                        Logger.Warn("[MAIN] Unknow command: " + cmdstr[0]);
                        break;
                }
            }

            Logger.Data("Press Key to Exit...");
            Console.ReadKey();
        }

        public static void RefreshProfiles()
        {
            Profiles.Refresh();
        }

        public static void Exit()
        {
            Logger.Warn("[MAIN] Exit the Server...");
            stop = false;
            if (!BlazeServer._exit) BlazeServer.Stop();
            if (!MagmaServer._exit) MagmaServer.Stop();
            if (!QOSServer._exit) QOSServer.Stop();
            if (!RedirectorServer._exit) RedirectorServer.Stop();
            if (!Webserver._exit) Webserver.Stop();
        }

        public static void StartDEDServer()
        {
            string args = "+key \"eakey\" +useServerMonitorTool 0 +soldierName \"test-server\" +sessionId 1234 +magmaProtocol http +magmaHost \"#HOSTIP#\" +magma 1 +guid \"5678\" +secret \"secret\"";
            args = args.Replace("#HOSTIP#", ProviderInfo.backendIP);
            Helper.RunShell("bfp4f_w32ded.exe", args);
        }

        public static void StartServers()
        {
            bool useQOS = Config.useQOS;
            bool useWebServer = Config.useWebServer;
            bool RediSSL = Config.RediSSL;
            bool useShark = Config.useShark;
            bool DEDISRV = Config.DEDIServer;

            ProviderInfo.backendIP = Config.IPAddress;

            if (useQOS)
            {
                ProviderInfo.QOS_IP = ProviderInfo.backendIP;
            }
            else
            {
                ProviderInfo.QOS_IP = "gossjcprod-qos01.ea.com";
            }

            if (RediSSL)
            {
                RedirectorServer.useSSL = true;
            }
            else
            {
                RedirectorServer.useSSL = false;
            }

            if(!useShark)
            {
                RedirectorServer.targetPort = 30001;
            }

            RedirectorServer.Start();
            BlazeServer.Start();

            MagmaServer.basicMode = true;
            MagmaServer.Start();

            if (useQOS)
            {
                QOSServer.Start();
            }

            if (useWebServer)
            {
                Webserver.Start();
            }

            if (DEDISRV)
            {
                StartDEDServer();
            }
        }
    }
}
