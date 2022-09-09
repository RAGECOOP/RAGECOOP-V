using GTA.Math;
using Lidgren.Network;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
[assembly: InternalsVisibleTo("RageCoop.Client.Installer")]
[assembly: InternalsVisibleTo("RageCoop.ResourceBuilder")]
namespace RageCoop.Core
{
    internal static class CoreUtils
    {
        private static readonly HashSet<string> ToIgnore = new HashSet<string>()
        {
            "RageCoop.Client.dll",
            "RageCoop.Core.dll",
            "RageCoop.Server.dll",
            "ScriptHookVDotNet3.dll",
            "ScriptHookVDotNet.dll"
        };
        public static bool CanBeIgnored(this string name)
        {
            return ToIgnore.Contains(name);
        }
        public static void GetBytesFromObject(object obj, NetOutgoingMessage m)
        {
            switch (obj)
            {
                case byte value:
                    m.Write((byte)0x01); m.Write(value); break;
                case short value:
                    m.Write((byte)0x02); m.Write(value); break;
                case ushort value:
                    m.Write((byte)0x03); m.Write(value); break;
                case int value:
                    m.Write((byte)0x04); m.Write(value); break;
                case uint value:
                    m.Write((byte)0x05); m.Write(value); break;
                case long value:
                    m.Write((byte)0x06); m.Write(value); break;
                case ulong value:
                    m.Write((byte)0x07); m.Write(value); break;
                case float value:
                    m.Write((byte)0x08); m.Write(value); break;
                case bool value:
                    m.Write((byte)0x09); m.Write(value); break;
                case string value:
                    m.Write((byte)0x10); m.Write(value); break;
                case Vector3 value:
                    m.Write((byte)0x11); m.Write(value); break;
                case Quaternion value:
                    m.Write((byte)0x12); m.Write(value); break;
                case GTA.Model value:
                    m.Write((byte)0x13); m.Write(value); break;
                case Vector2 value:
                    m.Write((byte)0x14); m.Write(value); break;
                case byte[] value:
                    m.Write((byte)0x15); m.WriteByteArray(value); break;
                case Tuple<byte, byte[]> value:
                    m.Write(value.Item1); m.Write(value.Item2); break;
                default:
                    throw new Exception("Unsupported object type: " + obj.GetType());
            }
        }
        public static IPEndPoint StringToEndPoint(string endpointstring)
        {
            return StringToEndPoint(endpointstring, -1);
        }
        public static IPEndPoint StringToEndPoint(string endpointstring, int defaultport)
        {
            if (string.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
            {
                throw new ArgumentException("Endpoint descriptor may not be empty.");
            }

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                || defaultport > IPEndPoint.MaxPort))
            {
                throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultport));
            }

            string[] values = endpointstring.Split(new char[] { ':' });
            IPAddress ipaddy;
            int port = -1;

            //check if we have an IPv6 or ports
            if (values.Length <= 2) // ipv4 or hostname
            {
                if (values.Length == 1)
                    //no port is specified, default
                    port = defaultport;
                else
                    port = getPort(values[1]);

                //try to use the address as IPv4, otherwise get hostname
                if (!IPAddress.TryParse(values[0], out ipaddy))
                    ipaddy = GetIPfromHost(values[0]);
            }
            else if (values.Length > 2) //ipv6
            {
                //could [a:b:c]:d
                if (values[0].StartsWith("[") && values[values.Length - 2].EndsWith("]"))
                {
                    string ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
                    ipaddy = IPAddress.Parse(ipaddressstring);
                    port = getPort(values[values.Length - 1]);
                }
                else //[a:b:c] or a:b:c
                {
                    ipaddy = IPAddress.Parse(endpointstring);
                    port = defaultport;
                }
            }
            else
            {
                throw new FormatException(string.Format("Invalid endpoint ipaddress '{0}'", endpointstring));
            }

            if (port == -1)
                throw new ArgumentException(string.Format("No port specified: '{0}'", endpointstring));

            return new IPEndPoint(ipaddy, port);
        }

        private static int getPort(string p)
        {

            if (!int.TryParse(p, out int port)
             || port < IPEndPoint.MinPort
             || port > IPEndPoint.MaxPort)
            {
                throw new FormatException(string.Format("Invalid end point port '{0}'", p));
            }

            return port;
        }
        public static IPAddress GetLocalAddress(string target = "8.8.8.8")
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(target, 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }
        public static IPAddress GetIPfromHost(string p)
        {
            var hosts = Dns.GetHostAddresses(p);

            if (hosts == null || hosts.Length == 0)
                throw new ArgumentException(string.Format("Host not found: {0}", p));

            return hosts[0];
        }
        public static IpInfo GetIPInfo()
        {
            // TLS only
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var httpClient = new HttpClient();
            HttpResponseMessage response = httpClient.GetAsync("https://ipinfo.io/json").GetAwaiter().GetResult();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"IPv4 request failed! [{(int)response.StatusCode}/{response.ReasonPhrase}]");
            }

            string content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonConvert.DeserializeObject<IpInfo>(content);
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (DirectoryInfo dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (FileInfo file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }
        public static string GetInvariantRID()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            }
            return "unknown";
        }

        /// <summary>
        /// Get local ip addresses on all network interfaces 
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetLocalAddress()
        {
            var addresses = new List<IPAddress>();
            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties ipProps = netInterface.GetIPProperties();
                foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
                {
                    addresses.Add(addr.Address);
                }
            }
            return addresses;
        }
    }
    internal class IpInfo
    {
        [JsonProperty("ip")]
        public string Address { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }
    }
    internal static class Extensions
    {
        public static byte[] GetBytes(this string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
        public static string GetString(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
        public static byte[] GetBytes(this Vector3 vec)
        {
            // 12 bytes
            return new List<byte[]>() { BitConverter.GetBytes(vec.X), BitConverter.GetBytes(vec.Y), BitConverter.GetBytes(vec.Z) }.Join(4);
        }

        public static byte[] GetBytes(this Vector2 vec)
        {
            // 8 bytes
            return new List<byte[]>() { BitConverter.GetBytes(vec.X), BitConverter.GetBytes(vec.Y) }.Join(4);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qua"></param>
        /// <returns>An array of bytes with length 16</returns>
        public static byte[] GetBytes(this Quaternion qua)
        {
            // 16 bytes
            return new List<byte[]>() { BitConverter.GetBytes(qua.X), BitConverter.GetBytes(qua.Y), BitConverter.GetBytes(qua.Z), BitConverter.GetBytes(qua.W) }.Join(4);
        }
        public static T GetPacket<T>(this NetIncomingMessage msg) where T : Packet, new()
        {
            var p = new T();
            p.Deserialize(msg);
            return p;
        }
        public static bool HasPedFlag(this PedDataFlags flagToCheck, PedDataFlags flag)
        {
            return (flagToCheck & flag) != 0;
        }
        public static bool HasProjDataFlag(this ProjectileDataFlags flagToCheck, ProjectileDataFlags flag)
        {
            return (flagToCheck & flag) != 0;
        }

        public static bool HasVehFlag(this VehicleDataFlags flagToCheck, VehicleDataFlags flag)
        {
            return (flagToCheck & flag) != 0;
        }
        public static bool HasConfigFlag(this PlayerConfigFlags flagToCheck, PlayerConfigFlags flag)
        {
            return (flagToCheck & flag) != 0;
        }
        public static Type GetActualType(this TypeCode code)
        {

            switch (code)
            {
                case TypeCode.Boolean:
                    return typeof(bool);

                case TypeCode.Byte:
                    return typeof(byte);

                case TypeCode.Char:
                    return typeof(char);

                case TypeCode.DateTime:
                    return typeof(DateTime);

                case TypeCode.DBNull:
                    return typeof(DBNull);

                case TypeCode.Decimal:
                    return typeof(decimal);

                case TypeCode.Double:
                    return typeof(double);

                case TypeCode.Empty:
                    return null;

                case TypeCode.Int16:
                    return typeof(short);

                case TypeCode.Int32:
                    return typeof(int);

                case TypeCode.Int64:
                    return typeof(long);

                case TypeCode.Object:
                    return typeof(object);

                case TypeCode.SByte:
                    return typeof(sbyte);

                case TypeCode.Single:
                    return typeof(Single);

                case TypeCode.String:
                    return typeof(string);

                case TypeCode.UInt16:
                    return typeof(UInt16);

                case TypeCode.UInt32:
                    return typeof(UInt32);

                case TypeCode.UInt64:
                    return typeof(UInt64);
            }

            return null;
        }
        public static string DumpWithType(this IEnumerable<object> objects)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var obj in objects)
            {
                sb.Append(obj.GetType() + ":" + obj.ToString() + "\n");
            }
            return sb.ToString();
        }
        public static string Dump<T>(this IEnumerable<T> objects)
        {
            return $"{{{string.Join(",", objects)}}}";
        }
        public static void ForEach<T>(this IEnumerable<T> objects, Action<T> action)
        {
            foreach (var obj in objects)
            {
                action(obj);
            }
        }
        public static byte[] ReadToEnd(this Stream stream)
        {
            if (stream is MemoryStream)
                return ((MemoryStream)stream).ToArray();

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
        public static MemoryStream ToMemStream(this Stream stream)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream;
        }
        public static byte[] Join(this List<byte[]> arrays, int lengthPerArray = -1)
        {
            if (arrays.Count == 1) { return arrays[0]; }
            var output = lengthPerArray == -1 ? new byte[arrays.Sum(arr => arr.Length)] : new byte[arrays.Count * lengthPerArray];
            int writeIdx = 0;
            foreach (var byteArr in arrays)
            {
                byteArr.CopyTo(output, writeIdx);
                writeIdx += byteArr.Length;
            }
            return output;
        }

        public static bool IsSubclassOf(this Type type, string baseTypeName)
        {
            for (Type t = type.BaseType; t != null; t = t.BaseType)
                if (t.FullName == baseTypeName)
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Some extension methods provided by RageCoop
    /// </summary>
    public static class PublicExtensions
    {
        /// <summary>
        /// Get a SHA256 hashed byte array of the input string, internally used to hash password at client side.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static byte[] GetSHA256Hash(this string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        /// <summary>
        /// Convert a byte array to hex-encoded string, internally used to trigger handshake event
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", String.Empty);
        }

        /// <summary>
        /// Convert a string to IP address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static IPAddress ToIP(this string ip)
        {
            return IPAddress.Parse(ip);
        }

    }
}
