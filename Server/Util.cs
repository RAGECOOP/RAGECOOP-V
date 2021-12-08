using System;
using System.IO;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;

using Lidgren.Network;

namespace CoopServer
{
    internal class Util
    {
        public static List<NativeArgument> ParseNativeArguments(params object[] args)
        {
            List<NativeArgument> result = new();

            foreach (object arg in args)
            {
                Type typeOf = arg.GetType();

                if (typeOf == typeof(int))
                {
                    result.Add(new IntArgument() { Data = (int)arg });
                }
                else if (typeOf == typeof(bool))
                {
                    result.Add(new BoolArgument() { Data = (bool)arg });
                }
                else if (typeOf == typeof(float))
                {
                    result.Add(new FloatArgument() { Data = (float)arg });
                }
                else if (typeOf == typeof(string))
                {
                    result.Add(new StringArgument() { Data = (string)arg });
                }
                else if (typeOf == typeof(LVector3))
                {
                    result.Add(new LVector3Argument() { Data = (LVector3)arg });
                }
                else
                {
                    Logging.Error("[Util->ParseNativeArguments(params object[] args)]: Type of argument not found!");
                    return null;
                }
            }

            return result;
        }

        public static NetConnection GetConnectionByUsername(string username)
        {
            Client client = Server.Clients.FirstOrDefault(x => x.Player.Username == username);
            if (client.Equals(default(Client)))
            {
                return null;
            }

            return Server.MainNetServer.Connections.FirstOrDefault(x => x.RemoteUniqueIdentifier == client.ID);
        }

        // Return a list of all connections but not the local connection
        public static List<NetConnection> FilterAllLocal(NetConnection local)
        {
            return new(Server.MainNetServer.Connections.Where(e => e != local));
        }
        public static List<NetConnection> FilterAllLocal(long local)
        {
            return new(Server.MainNetServer.Connections.Where(e => e.RemoteUniqueIdentifier != local));
        }

        // Return a list of players within range of ...
        public static List<NetConnection> GetAllInRange(LVector3 position, float range)
        {
            return new(Server.MainNetServer.Connections.FindAll(e =>
            {
                Client client = Server.Clients.First(x => x.ID == e.RemoteUniqueIdentifier);
                return !client.Equals(default(Client)) && client.Player.IsInRangeOf(position, range);
            }));
        }
        // Return a list of players within range of ... but not the local one
        public static List<NetConnection> GetAllInRange(LVector3 position, float range, NetConnection local)
        {
            return new(Server.MainNetServer.Connections.Where(e =>
            {
                Client client = Server.Clients.First(x => x.ID == e.RemoteUniqueIdentifier);
                return e != local && !client.Equals(default(Client)) && client.Player.IsInRangeOf(position, range);
            }));
        }

        public static T Read<T>(string file) where T : new()
        {
            XmlSerializer ser = new(typeof(T));

            string path = AppContext.BaseDirectory + file;
            T data;

            if (File.Exists(path))
            {
                using (FileStream stream = File.OpenRead(path))
                {
                    data = (T)ser.Deserialize(stream);
                }

                using (FileStream stream = new(path, File.Exists(path) ? FileMode.Truncate : FileMode.Create, FileAccess.ReadWrite))
                {
                    ser.Serialize(stream, data);
                }
            }
            else
            {
                using FileStream stream = File.OpenWrite(path);
                ser.Serialize(stream, data = new T());
            }

            return data;
        }
    }
}
