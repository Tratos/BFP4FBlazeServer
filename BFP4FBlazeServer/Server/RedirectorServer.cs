﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BlazeLibWV;

namespace BFP4FBlazeServer
{
    public static class RedirectorServer
    {
        public static readonly object _sync = new object();
        public static bool _exit;
        public static bool useSSL;
        public static bool box = true;
        public static TcpListener lRedirector = null;
        public static int targetPort = 30000;

        public static void Start()
        {
            SetExit(false);
            Log("Starting Redirector...");
            new Thread(new ParameterizedThreadStart(tRedirectorMain)).Start();
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(10);
                Application.DoEvents();
            }
        }

        public static void Stop()
        {
            Log("Backend stopping...");
            if (lRedirector != null) lRedirector.Stop();
            SetExit(true);
            Log("Done.");
        }

        public static void tRedirectorMain(object obj)
        {
            X509Certificate2 cert = null;
            try
            {
                Log("[REDI] Redirector starting...");
                lRedirector = new TcpListener(IPAddress.Parse(ProviderInfo.backendIP), 42127);
                Log("[REDI] Redirector bound to  " + ProviderInfo.backendIP + ":42127");
                lRedirector.Start();
                if (useSSL)
                {
                    Log("[REDI] Loading Cert...");
                    cert = new X509Certificate2(BFP4FBlazeServer.Resources.Resource1.redi, "123456");
                }
                Log("[REDI] Redirector listening...");
                TcpClient client;
                while (!GetExit())
                {
                    client = lRedirector.AcceptTcpClient();
                    Log("[REDI] Client connected");
                    if (useSSL)
                    {
                        SslStream sslStream = new SslStream(client.GetStream(), false);
                        sslStream.AuthenticateAsServer(cert, false, SslProtocols.Ssl3, false);
                        byte[] data = Helper.ReadContentSSL(sslStream);
                        MemoryStream m = new MemoryStream();
                        m.Write(data, 0, data.Length);
                        data = CreateRedirectorPacket();
                        m.Write(data, 0, data.Length);
                        sslStream.Write(data);
                        sslStream.Flush();
                        client.Close();
                    }
                    else
                    {
                        NetworkStream stream = client.GetStream();
                        byte[] data = Helper.ReadContentTCP(stream);
                        MemoryStream m = new MemoryStream();
                        m.Write(data, 0, data.Length);
                        data = CreateRedirectorPacket();
                        m.Write(data, 0, data.Length);
                        stream.Write(data, 0, data.Length);
                        stream.Flush();
                        client.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("REDI", ex);
            }
        }

        public static byte[] CreateRedirectorPacket()
        {
            List<Blaze.Tdf> Result = new List<Blaze.Tdf>();
            List<Blaze.Tdf> VALU = new List<Blaze.Tdf>();
            VALU.Add(Blaze.TdfString.Create("HOST", ProviderInfo.backendIP));
            VALU.Add(Blaze.TdfInteger.Create("IP\0\0", Blaze.GetIPfromString(ProviderInfo.backendIP)));
            VALU.Add(Blaze.TdfInteger.Create("PORT", targetPort));
            Blaze.TdfUnion ADDR = Blaze.TdfUnion.Create("ADDR", 0, Blaze.TdfStruct.Create("VALU", VALU));
            Result.Add(ADDR);
            Result.Add(Blaze.TdfInteger.Create("SECU", 0));
            Result.Add(Blaze.TdfInteger.Create("XDNS", 0));
            return Blaze.CreatePacket(5, 1, 0, 0x1000, 0, Result);
        }

        public static void SetExit(bool state)
        {
            lock (_sync)
            {
                _exit = state;
            }
        }

        public static bool GetExit()
        {
            bool result;
            lock (_sync)
            {
                result = _exit;
            }
            return result;
        }

        public static void Log(string s)
        {
            if (box == false) return;
            try
            {
                Logger.Info(s);
            }
            catch { }
        }

        public static void LogError(string who, Exception e, string cName = "")
        {
            string result = "";
            if (who != "") result = "[" + who + "] " + cName + " ERROR: ";
            result += e.Message;
            if (e.InnerException != null)
                result += " - " + e.InnerException.Message;
            //Log(result);
            Logger.Error(result);
        }
    }
}
