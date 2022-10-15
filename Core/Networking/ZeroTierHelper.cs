using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RageCoop.Core
{
    internal class ZeroTierNetwork
    {
        public ZeroTierNetwork(string line)
        {
            // <nwid> <name> <mac> <status> <type> <dev> <ZT assigned ips>
            var v = Regex.Split(line, " ").Skip(2).ToArray();
            ID = v[0];
            Name = v[1];
            Mac = v[2];
            Status = v[3];
            Type = v[4];
            Device = v[5];
            foreach (var i in v[6].Split(','))
            {
                Addresses.Add(i.Split('/')[0]);
            }
        }
        public string ID;
        public string Name;
        public string Mac;
        public string Status;
        public string Type;
        public string Device;
        public List<string> Addresses = new List<string>();

    }
    internal static class ZeroTierHelper
    {
        private static readonly string _path = "zerotier-cli";
        private static readonly string _arg = "";
        static ZeroTierHelper()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var batpath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "ZeroTier", "One", "zerotier-cli.bat");
                _arg = $"/c \"{batpath}\" ";
                _path = "cmd.exe";
            }
            var status = RunCommand("status");
            if (!status.StartsWith("200"))
            {
                throw new Exception("ZeroTier not ready: " + status);
            }
        }
        public static ZeroTierNetwork Join(string networkId, int timeout = 10000)
        {
            var p = Run("join " + networkId);
            var o = p.StandardOutput.ReadToEnd();
            if (!o.StartsWith("200 join OK"))
            {
                throw new Exception(o + p.StandardError.ReadToEnd());
            }
            if (timeout == 0) { return Networks[networkId]; }
            int i = 0;
            while (i <= timeout)
            {
                i += 100;
                if (Networks.TryGetValue(networkId, out var n))
                {
                    if (n.Addresses.Count != 0 && (!n.Addresses.Where(x => x == "-").Any()))
                    {
                        return n;
                    }
                    System.Threading.Thread.Sleep(100);
                }
                else
                {
                    break;
                }
            }
            return null;
        }
        public static void Leave(string networkId)
        {
            var p = Run("leave " + networkId);
            var o = p.StandardOutput.ReadToEnd();
            if (!o.StartsWith("200 leave OK"))
            {
                throw new Exception(o + p.StandardError.ReadToEnd());
            }
        }
        public static Dictionary<string, ZeroTierNetwork> Networks
        {
            get
            {
                Dictionary<string, ZeroTierNetwork> networks = new Dictionary<string, ZeroTierNetwork>();
                var p = Run("listnetworks");
                var lines = Regex.Split(p.StandardOutput.ReadToEnd(), "\n").Skip(1);

                foreach (var line in lines)
                {
                    var l = line.Replace("\r", "");
                    if (!string.IsNullOrWhiteSpace(l))
                    {
                        var n = new ZeroTierNetwork(l);
                        networks.Add(n.ID, n);
                    }
                }
                return networks;
            }
        }
        private static Process Run(string args)
        {
            var p = new Process();
            p.StartInfo = new ProcessStartInfo()
            {
                FileName = _path,
                Arguments = _arg + args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            p.Start();
            p.WaitForExit();
            return p;
        }
        private static string RunCommand(string command)
        {
            var p = Run(command);
            return p.StandardOutput.ReadToEnd() + p.StandardError.ReadToEnd();
        }
        public static void Check()
        {

        }
    }
}
