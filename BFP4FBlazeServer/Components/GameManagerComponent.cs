using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BlazeLibWV;
using System.Net.Sockets;

namespace BFP4FBlazeServer
{
    public static class GameManagerComponent
    {

        public static void HandlePacket(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            switch (p.Command)
            {
                case 0x1:
                    CreateGame(p, pi, ns);
                    break;
                case 0x2:
                    DestroyGame(p, pi, ns);
                    break;
                case 0x3:
                    AdvanceGameState(p, pi, ns);
                    break;
                case 0x7:
                    SetGameAttributes(p, pi, ns);
                    break;
                case 0x9:
                    JoinGame(p, pi, ns);
                    break;
                case 0xB:
                    RemovePlayer(p, pi, ns);
                    break;
                case 0xD:
                    StartMatchmaking(p, pi, ns);
                    break;
                case 0xf:
                    FinalizeGameCreation(p, pi, ns);
                    break;
                case 0x13:
                    ReplayGame(p, pi, ns);
                    break;
                case 0x1d:
                    UpdateMeshConnection(p, pi, ns);
                    break;
                case 0x67:
                    GetFullGameData(p, pi, ns);
                    break;
                case 0x6C:
                    SetPlayerTeam(p, pi, ns);
                    break;
            }
        }

        public static void CreateGame(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            pi.stat = 4;
            pi.slot = pi.game.getNextSlot();
            pi.game.setNextSlot((int)pi.userId);
            pi.game.id = 1;
            pi.game.isRunning = true;
            pi.game.GSTA = 7;
            pi.game.players[0] = pi;

            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            result.Add(Blaze.TdfInteger.Create("GID\0", pi.game.id));
            result.Add(Blaze.TdfInteger.Create("GSTA", pi.game.GSTA));
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            AsyncGameManager.NotifyGameStateChange(pi, p, pi, ns);
            AsyncGameManager.NotifyServerGameSetup(pi, p, pi, ns);
        }

        public static void DestroyGame(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            result.Add(Blaze.ReadPacketContent(p)[0]);
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();
        }

        public static void AdvanceGameState(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            pi.game.GSTA = (uint)((Blaze.TdfInteger)input[1]).Value;
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, new List<Blaze.Tdf>());
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            foreach (PlayerInfo peer in pi.game.players)
                if (peer != null)
                    AsyncGameManager.NotifyGameStateChange(peer, p, pi, peer.ns);
        }

        public static void SetGameAttributes(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            pi.game.ATTR = (Blaze.TdfDoubleList)input[0];
            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            foreach (PlayerInfo peer in pi.game.players)
                if (peer != null)
                    try
                    {
                        AsyncGameManager.NotifyGameSettingsChange(peer, p, pi, peer.ns);
                    }
                    catch
                    {
                        pi.game.removePlayer((int)peer.userId);
                        Logger.Error("[CLNT] #" + pi.userId + " : 'SetGameAttributes' peer crashed!");
                    }
        }

        public static void JoinGame(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            PlayerInfo srv = null;
            foreach (PlayerInfo info in BlazeServer.allClients)
                if (info.isServer)
                {
                    srv = info;
                    break;
                }
            if (srv == null)
            {
                Logger.Error("[CLNT] #" + pi.userId + " : cant find game to join!");
                return;
            }
            pi.game = srv.game;
            pi.slot = srv.game.getNextSlot();
            BlazeServer.Log("[CLNT] #" + pi.userId + " : assigned Slot Id " + pi.slot);
            if (pi.slot == 255)
            {
                Logger.Warn("[CLNT] #" + pi.userId + " : server full!");
                return;
            }
            srv.game.setNextSlot((int)pi.userId);
            srv.game.players[pi.slot] = pi;

            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            result.Add(Blaze.TdfInteger.Create("GID\0", srv.game.id));
            result.Add(Blaze.TdfInteger.Create("JGS\0", 0));
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            pi.stat = 2;

            AsyncUserSessions.NotifyUserAdded(pi, p, pi, ns);
            AsyncUserSessions.NotifyUserStatus(pi, p, pi, ns);
            AsyncGameManager.NotifyClientGameSetup(pi, p, pi, srv, ns);

            AsyncUserSessions.NotifyUserAdded(srv, p, pi, srv.ns);
            AsyncUserSessions.NotifyUserStatus(srv, p, pi, srv.ns);
            AsyncUserSessions.UserSessionExtendedDataUpdateNotification(srv, p, pi, srv.ns);
            AsyncGameManager.NotifyPlayerJoining(srv, p, pi, srv.ns);

        }

        public static void RemovePlayer(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            Blaze.TdfInteger CNTX = (Blaze.TdfInteger)input[1];
            Blaze.TdfInteger PID = (Blaze.TdfInteger)input[3];
            Blaze.TdfInteger REAS = (Blaze.TdfInteger)input[4];
            pi.game.removePlayer((int)PID.Value);
            GC.Collect();
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, new List<Blaze.Tdf>());
            ns.Write(buff, 0, buff.Length);
            ns.Flush();
            foreach (PlayerInfo player in BlazeServer.allClients)
                if (player != null && player.userId == PID.Value)
                    player.cntx = CNTX.Value;
            foreach (PlayerInfo player in pi.game.players)
                if (player != null && player.userId != PID.Value)
                    try
                    {
                        AsyncGameManager.NotifyPlayerRemoved(player, p, player, player.ns, PID.Value, CNTX.Value, REAS.Value);
                    }
                    catch
                    {
                        Logger.Error("[CLNT] #" + pi.userId + " : 'RemovePlayer' peer crashed!");
                    }
        }

        public static void FinalizeGameCreation(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            if (pi.isServer)
                AsyncGameManager.NotifyPlatformHostInitialized(pi, p, pi, ns);
        }

        public static void ReplayGame(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, new List<Blaze.Tdf>());
            ns.Write(buff, 0, buff.Length);
            ns.Flush();
            pi.game.GSTA = 130;
            pi.timeout.Restart();
            foreach (PlayerInfo peer in pi.game.players)
                if (peer != null)
                    AsyncGameManager.NotifyGameStateChange(peer, p, pi, peer.ns);
        }

        public static void UpdateMeshConnection(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            List<Blaze.Tdf> input = Blaze.ReadPacketContent(p);
            List<Blaze.TdfStruct> entries = (List<Blaze.TdfStruct>)((Blaze.TdfList)input[1]).List;
            Blaze.TdfInteger pid = (Blaze.TdfInteger)entries[0].Values[1];
            Blaze.TdfInteger stat = (Blaze.TdfInteger)entries[0].Values[2];
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, new List<Blaze.Tdf>());
            ns.Write(buff, 0, buff.Length);
            ns.Flush();

            PlayerInfo target = null;
            foreach (PlayerInfo info in BlazeServer.allClients)
                if (info.userId == pid.Value)
                {
                    target = info;
                    break;
                }
            if (target != null)
            {
                if (stat.Value == 2)
                {
                    if (pi.isServer)
                    {
                        AsyncUserSessions.UserSessionExtendedDataUpdateNotification(pi, p, target, ns);
                        AsyncGameManager.NotifyGamePlayerStateChange(pi, p, target, ns, 4);
                        AsyncGameManager.NotifyPlayerJoinCompleted(pi, p, target, ns);
                    }
                    else
                    {
                        AsyncGameManager.NotifyGamePlayerStateChange(pi, p, pi, ns, 4);
                        AsyncGameManager.NotifyPlayerJoinCompleted(pi, p, pi, ns);
                    }
                }
                else
                    AsyncUserSessions.NotifyUserRemoved(pi, p, pid.Value, ns);
            }
        }

        public static void GetFullGameData(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            PlayerInfo srv = null;
            foreach (PlayerInfo info in BlazeServer.allClients)
                if (info.isServer)
                {
                    srv = info;
                    break;
                }
            if (srv == null)
            {
                Logger.Error("[CLNT] #" + pi.userId + " : cant find game to join!");
                return;
            }
            pi.game = srv.game;
            pi.slot = srv.game.getNextSlot();
            srv.game.setNextSlot((int)pi.userId);

            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            List<Blaze.TdfStruct> LGAM = new List<Blaze.TdfStruct>();
            List<Blaze.Tdf> ee0 = new List<Blaze.Tdf>();
            List<Blaze.Tdf> GAME = new List<Blaze.Tdf>();
            GAME.Add(Blaze.TdfList.Create("ADMN", 0, 1, new List<long>(new long[] { srv.userId })));
            GAME.Add(srv.game.ATTR);
            GAME.Add(Blaze.TdfList.Create("CAP\0", 0, 2, new List<long>(new long[] { 0x20, 0 })));
            GAME.Add(Blaze.TdfInteger.Create("GID\0", pi.game.id));
            GAME.Add(Blaze.TdfString.Create("GNAM", pi.game.GNAM));
            GAME.Add(Blaze.TdfInteger.Create("GPVH", 666));
            GAME.Add(Blaze.TdfInteger.Create("GSET", pi.game.GSET));
            GAME.Add(Blaze.TdfInteger.Create("GSID", 1));
            GAME.Add(Blaze.TdfInteger.Create("GSTA", pi.game.GSTA));
            GAME.Add(Blaze.TdfString.Create("GTYP", "AssaultStandard"));
            GAME.Add(BlazeHelper.CreateNETField(srv, "HNET"));
            GAME.Add(Blaze.TdfInteger.Create("HSES", 13666));
            GAME.Add(Blaze.TdfInteger.Create("IGNO", 0));
            GAME.Add(Blaze.TdfInteger.Create("MCAP", 0x20));
            GAME.Add(BlazeHelper.CreateNQOSField(srv, "NQOS"));
            GAME.Add(Blaze.TdfInteger.Create("NRES", 0));
            GAME.Add(Blaze.TdfInteger.Create("NTOP", 1));
            GAME.Add(Blaze.TdfString.Create("PGID", ""));
            List<Blaze.Tdf> PHST = new List<Blaze.Tdf>();
            PHST.Add(Blaze.TdfInteger.Create("HPID", srv.userId));
            PHST.Add(Blaze.TdfInteger.Create("HSLT", srv.slot));
            GAME.Add(Blaze.TdfStruct.Create("PHST", PHST));
            GAME.Add(Blaze.TdfInteger.Create("PRES", 1));
            GAME.Add(Blaze.TdfString.Create("PSAS", "wv"));
            GAME.Add(Blaze.TdfInteger.Create("QCAP", 0x10));
            GAME.Add(Blaze.TdfInteger.Create("SEED", 0x2CF2048F));
            GAME.Add(Blaze.TdfInteger.Create("TCAP", 0x10));
            List<Blaze.Tdf> THST = new List<Blaze.Tdf>();
            THST.Add(Blaze.TdfInteger.Create("HPID", srv.userId));
            THST.Add(Blaze.TdfInteger.Create("HPID", srv.slot));
            GAME.Add(Blaze.TdfStruct.Create("THST", THST));
            GAME.Add(Blaze.TdfString.Create("UUID", "f5193367-c991-4429-aee4-8d5f3adab938"));
            GAME.Add(Blaze.TdfInteger.Create("VOIP", pi.game.VOIP));
            GAME.Add(Blaze.TdfString.Create("VSTR", pi.game.VSTR));
            ee0.Add(Blaze.TdfStruct.Create("GAME", GAME));
            LGAM.Add(Blaze.TdfStruct.Create("0", ee0));
            result.Add(Blaze.TdfList.Create("LGAM", 3, 1, LGAM));
            byte[] buff = Blaze.CreatePacket(p.Component, 0x67, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();
        }

        public static void StartMatchmaking(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            PlayerInfo srv = null;
            foreach (PlayerInfo info in BlazeServer.allClients)
                if (info.isServer)
                {
                    srv = info;
                    break;
                }
            if (srv == null)
            {
                Logger.Error("[CLNT] #" + pi.userId + " : cant find game to join!");
                return;
            }
            pi.game = srv.game;
            pi.slot = srv.game.getNextSlot();
            srv.game.setNextSlot((int)pi.userId);

            List<Blaze.Tdf> result = new List<Blaze.Tdf>();
            result.Add(Blaze.TdfInteger.Create("MSID", pi.userId));
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, result);
            ns.Write(buff, 0, buff.Length);
            ns.Flush();


            pi.stat = 2;

            AsyncUserSessions.NotifyUserAdded(pi, p, srv, ns);
            AsyncUserSessions.NotifyUserStatus(pi, p, srv, ns);
            AsyncGameManager.NotifyClientGameSetup(pi, p, pi, srv, ns);

            AsyncUserSessions.NotifyUserAdded(srv, p, pi, srv.ns);
            AsyncUserSessions.NotifyUserStatus(srv, p, pi, srv.ns);
            AsyncGameManager.NotifyPlayerJoining(srv, p, pi, srv.ns);
            AsyncUserSessions.UserSessionExtendedDataUpdateNotification(srv, p, pi, srv.ns);
        }

        public static void SetPlayerTeam(Blaze.Packet p, PlayerInfo pi, NetworkStream ns)
        {
            byte[] buff = Blaze.CreatePacket(p.Component, p.Command, 0, 0x1000, p.ID, new List<Blaze.Tdf>());
            ns.Write(buff, 0, buff.Length);
            ns.Flush();
        }
    }
}
