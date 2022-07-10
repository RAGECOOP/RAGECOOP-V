using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using System.Collections.Generic;
using RageCoop.Core;
using Lidgren.Network;
using System.Net;
using System.Net.Sockets;

namespace RageCoop.Server
{
    static partial class Util
    {

        public static string DownloadString(string url)
        {
            try
            {
                // TLS only
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls13 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                
                WebClient client = new();
                return client.DownloadString(url);
            }
            catch
            {
                return "";
            }
        }
        public static (byte, byte[]) GetBytesFromObject(object obj)
        {
            return obj switch
            {
                byte _ => (0x01, BitConverter.GetBytes((byte)obj)),
                short _ => (0x02, BitConverter.GetBytes((short)obj)),
                ushort _ => (0x03, BitConverter.GetBytes((ushort)obj)),
                int _ => (0x04, BitConverter.GetBytes((int)obj)),
                uint _ => (0x05, BitConverter.GetBytes((uint)obj)),
                long _ => (0x06, BitConverter.GetBytes((long)obj)),
                ulong _ => (0x07, BitConverter.GetBytes((ulong)obj)),
                float _ => (0x08, BitConverter.GetBytes((float)obj)),
                bool _ => (0x09, BitConverter.GetBytes((bool)obj)),
                _ => (0x0, null),
            };
        }
        public static List<NetConnection> Exclude(this IEnumerable<NetConnection> connections,NetConnection toExclude)
        {
            return new(connections.Where(e => e != toExclude));
        }
        
        public static T Read<T>(string file) where T : new()
        {
            XmlSerializer ser = new(typeof(T));

            XmlWriterSettings settings = new()
            {
                Indent = true,
                IndentChars = ("\t"),
                OmitXmlDeclaration = true
            };

            string path = AppContext.BaseDirectory + file;
            T data;

            if (File.Exists(path))
            {
                try
                {
                    using (XmlReader stream = XmlReader.Create(path))
                    {
                        data = (T)ser.Deserialize(stream);
                    }

                    using (XmlWriter stream = XmlWriter.Create(path, settings))
                    {
                        ser.Serialize(stream, data);
                    }
                }
                catch
                {
                    using (XmlWriter stream = XmlWriter.Create(path, settings))
                    {
                        ser.Serialize(stream, data = new T());
                    }
                }
            }
            else
            {
                using (XmlWriter stream = XmlWriter.Create(path, settings))
                {
                    ser.Serialize(stream, data = new T());
                }
            }

            return data;
        }

        public static T Next<T>(this T[] values)
        {
            return values[new Random().Next(values.Length-1)];
        }
    }
}
