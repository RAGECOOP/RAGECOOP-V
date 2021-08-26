using System;
using System.Collections.Generic;
using System.Threading;

namespace MasterServer
{
    class Program
    {
        static readonly List<Server> Servers = new();
        static readonly Dictionary<string, int> SpamSecurity = new();

        static void Main()
        {
            try
            {
                Console.Title = "MasterServer v0.1.0";

                Logging.Info("Trying to start the socket...");
            }
            catch (Exception e)
            {
                Logging.Error(e.Message.ToString());
            }

            Listen();
        }

        static void Listen()
        {
            while (true)
            {
                // 16 milliseconds to sleep to reduce CPU usage
                Thread.Sleep(1000 / 60);
            }
        }

        #region ===== FUNCTIONS =====
        static bool CheckSpam(string address)
        {
            return SpamSecurity.TryGetValue(address, out int lastCheck) && (Environment.TickCount - lastCheck) < 10000;
        }

        static Server GetServerForClient()
        {
            return Servers.Find(x => x.Players < x.MaxPlayers);
        }

        static bool AddServer(Server server)
        {
            if (Servers.Exists(x => x.Address == server.Address))
            {
                return false;
            }

            try
            {
                Servers.Add(server);
            }
            catch (Exception e)
            {
                Logging.Error(e.Message.ToString());
                return false;
            }

            return true;
        }

        static bool RemoveServer(Server server)
        {
            if (!Servers.Exists(x => x.Address == server.Address))
            {
                return true;
            }

            try
            {
                Servers.Remove(server);
            }
            catch (Exception e)
            {
                Logging.Error(e.Message.ToString());
                return false;
            }

            return true;
        }
        #endregion
    }
}
