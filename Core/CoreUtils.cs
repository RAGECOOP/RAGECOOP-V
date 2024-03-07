using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Xml;
using GTA;
using GTA.Math;
using Lidgren.Network;
using RageCoop.Core.Scripting;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
[assembly: InternalsVisibleTo("RageCoop.Client.Scripting")]
[assembly: InternalsVisibleTo("RageCoop.Client.Installer")]
[assembly: InternalsVisibleTo("DataDumper")]
[assembly: InternalsVisibleTo("UnitTest")]
[assembly: InternalsVisibleTo("RageCoop.ResourceBuilder")]

namespace RageCoop.Core
{
    internal static class CoreUtils
    {
        internal static Random SafeRandom => _randInstance.Value;
        private static int _randSeed = Environment.TickCount;
        private static readonly ThreadLocal<Random> _randInstance
            = new(() => new Random(Interlocked.Increment(ref _randSeed)));

        private static readonly HashSet<string> ToIgnore = new()
        {
            "RageCoop.Client",
            "RageCoop.Client.Loader",
            "RageCoop.Client.Installer",
            "RageCoop.Core",
            "RageCoop.Server",
            "ScriptHookVDotNet2",
            "ScriptHookVDotNet3",
            "ScriptHookVDotNet",
            "ScriptHookVDotNetCore"
        };

        public static string FormatToSharpStyle(string input, int offset)
        {
            var ss = input.Substring(offset).Split("_".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            // Replace first character with upper case
            for (var i = 0; i < ss.Length; i++)
            {
                var sec = ss[i].ToLower();
                var head = sec[0];
                ss[i] = head.ToString().ToUpper() + sec.Remove(0, 1);
            }

            return string.Join("", ss);
        }

        public static string ToHex(this int value)
        {
            return string.Format("0x{0:X}", value);
        }

        public static string ToHex(this uint value)
        {
            return string.Format("0x{0:X}", value);
        }

        public static int RandInt(int start, int end)
        {
            return SafeRandom.Next(start, end);
        }

        public static string GetTempDirectory(string dir = null)
        {
            dir = dir ?? Path.GetTempPath();
            string path;
            do
            {
                path = Path.Combine(dir, RandomString(10));
            } while (Directory.Exists(path) || File.Exists(path));

            return path;
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[SafeRandom.Next(s.Length)]).ToArray());
        }

        public static Version GetLatestVersion(string branch = "dev-nightly")
        {
            var url =
                $"https://raw.githubusercontent.com/RAGECOOP/RAGECOOP-V/{branch}/RageCoop.Server/Properties/AssemblyInfo.cs";
            var versionLine = HttpHelper.DownloadString(url)
                .Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Contains("[assembly: AssemblyVersion(")).First();
            var start = versionLine.IndexOf('\"') + 1;
            var end = versionLine.LastIndexOf('\"');
            return Version.Parse(versionLine.Substring(start, end - start));
        }

        public static bool CanBeIgnored(this string name)
        {
            name = Path.GetFileNameWithoutExtension(name);
            return ToIgnore.Contains(name) || AssemblyLoadContext.Default.Assemblies.Any(x => x.GetName().Name == name);
        }

        public static void ForceLoadAllAssemblies()
        {
            foreach (var a in AssemblyLoadContext.Default.Assemblies)
                LoadAllReferencedAssemblies(a.GetName());
        }

        public static void LoadAllReferencedAssemblies(this AssemblyName assembly)
        {
            try
            {
                foreach (var child in Assembly.Load(assembly).GetReferencedAssemblies())
                    LoadAllReferencedAssemblies(child);
            }
            catch (Exception ex)
            {
                if (!assembly.Name.StartsWith("Microsoft.CodeAnalysis"))
                {
                    System.Console.WriteLine("Error loading dependency: " + ex);
                }
            }
        }

        public static string ToFullPath(this string path)
        {
            return Path.GetFullPath(path);
        }

        public static IPEndPoint StringToEndPoint(string endpointstring)
        {
            return StringToEndPoint(endpointstring, -1);
        }

        public static IPEndPoint StringToEndPoint(string endpointstring, int defaultport)
        {
            if (string.IsNullOrEmpty(endpointstring)
                || endpointstring.Trim().Length == 0)
                throw new ArgumentException("Endpoint descriptor may not be empty.");

            if (defaultport != -1 &&
                (defaultport < IPEndPoint.MinPort
                 || defaultport > IPEndPoint.MaxPort))
                throw new ArgumentException(string.Format("Invalid default port '{0}'", defaultport));

            var values = endpointstring.Split(':');
            IPAddress ipaddy;
            var port = -1;

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
                    var ipaddressstring = string.Join(":", values.Take(values.Length - 1).ToArray());
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
            if (!int.TryParse(p, out var port)
                || port < IPEndPoint.MinPort
                || port > IPEndPoint.MaxPort)
                throw new FormatException(string.Format("Invalid end point port '{0}'", p));

            return port;
        }

        public static IPAddress GetLocalAddress(string target = "8.8.8.8")
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect(target, 65530);
                var endPoint = socket.LocalEndPoint as IPEndPoint;
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
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

            var httpClient = new HttpClient();
            var response = httpClient.GetAsync("https://ipinfo.io/json").GetAwaiter().GetResult();
            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception($"IPv4 request failed! [{(int)response.StatusCode}/{response.ReasonPhrase}]");

            var content = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            return JsonDeserialize<IpInfo>(content);
        }

        public static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
                CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
            foreach (var file in source.GetFiles())
                file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        public static string GetInvariantRID()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return "win-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return "linux-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return "osx-" + RuntimeInformation.OSArchitecture.ToString().ToLower();
            return "unknown";
        }

        /// <summary>
        ///     Get local ip addresses on all network interfaces
        /// </summary>
        /// <returns></returns>
        public static List<IPAddress> GetLocalAddress()
        {
            var addresses = new List<IPAddress>();
            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var ipProps = netInterface.GetIPProperties();
                foreach (var addr in ipProps.UnicastAddresses) addresses.Add(addr.Address);
            }

            return addresses;
        }

        public static StreamWriter OpenWriter(string path, FileMode mode = FileMode.Create,
            FileAccess access = FileAccess.Write, FileShare share = FileShare.ReadWrite)
        {
            return new StreamWriter(File.Open(path, mode, access, share));
        }


        public static float GetFloat(this XmlNode n)
        {
            return float.Parse(n.Attributes["value"].Value);
        }

        /// <summary>
        ///     Generate jenkins one-at-a-time hash from specified string (lower)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static uint JoaatHash(string key)
        {
            var i = 0;
            uint hash = 0;
            while (i != key.Length)
            {
                hash += char.ToLowerInvariant(key[i++]);
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;
            return hash;
        }

        public static unsafe bool StructCmp<T>(ref T left, ref T right) where T : unmanaged
        {
            fixed (T* pLeft = &left, pRight = &right)
            {
                return MemCmp(pLeft, pRight, sizeof(T));
            }
        }


        public static unsafe bool StructCmp<T>(T left, T right) where T : unmanaged
        {
            return MemCmp(&left, &right, sizeof(T));
        }

        static bool _simdSupported = System.Numerics.Vector<byte>.IsSupported;
        static int _simdSlots = _simdSupported ? System.Numerics.Vector<byte>.Count : 0;

        /// <summary>
        /// SIMD-accelerated memory comparer
        /// </summary>
        /// <returns></returns>
        public static unsafe bool MemCmp(void* p1, void* p2, int cbToCompare)
        {
            int numVectors = cbToCompare / _simdSlots;
            int ceiling = numVectors * _simdSlots;
            if (numVectors > 0)
            {
                ReadOnlySpan<System.Numerics.Vector<byte>> leftVecArray = new(p1, numVectors);
                ReadOnlySpan<System.Numerics.Vector<byte>> rightVecArray = new(p2, numVectors);

                for (int i = 0; i < numVectors; i++)
                {
                    if (leftVecArray[i] != rightVecArray[i])
                        return false;
                }
            }

            int numWords = cbToCompare / sizeof(IntPtr);
            var pwLeft = (IntPtr*)p1;
            var pwRight = (IntPtr*)p2;

            for (int i = (ceiling / sizeof(IntPtr)); i < numWords; i++)
            {
                if (pwLeft[i] != pwRight[i])
                    return false;
            }

            var pbLeft = (byte*)p1;
            var pbRight = (byte*)p2;

            for (int i = ceiling + (numWords * sizeof(IntPtr)); i < cbToCompare; i++)
            {
                if (pbLeft[i] != pbRight[i])
                    return false;
            }

            return true;
        }
    }

    internal class IpInfo
    {
        [JsonPropertyName("ip")] public string Address { get; set; }

        [JsonPropertyName("country")] public string Country { get; set; }
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

        public static T GetPacket<T>(this NetIncomingMessage msg) where T : Packet, new()
        {
            var p = new T();
            p.Deserialize(msg);
            return p;
        }

        public static bool HasPedFlag(this PedDataFlags flags, PedDataFlags flag)
        {
            return (flags & flag) != 0;
        }

        public static bool HasProjDataFlag(this ProjectileDataFlags flags, ProjectileDataFlags flag)
        {
            return (flags & flag) != 0;
        }

        public static bool HasVehFlag(this VehicleDataFlags flags, VehicleDataFlags flag)
        {
            return (flags & flag) != 0;
        }

        public static bool HasConfigFlag(this PlayerConfigFlags flags, PlayerConfigFlags flag)
        {
            return (flags & flag) != 0;
        }

        public static bool HasEventFlag(this CustomEventFlags flags, CustomEventFlags flag)
        {
            return (flags & flag) != 0;
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
                    return typeof(float);

                case TypeCode.String:
                    return typeof(string);

                case TypeCode.UInt16:
                    return typeof(ushort);

                case TypeCode.UInt32:
                    return typeof(uint);

                case TypeCode.UInt64:
                    return typeof(ulong);
            }

            return null;
        }

        public static string DumpWithType(this IEnumerable<object> objects)
        {
            var sb = new StringBuilder();
            foreach (var obj in objects) sb.Append(obj.GetType() + ":" + obj + "\n");
            return sb.ToString();
        }

        public static string Dump<T>(this IEnumerable<T> objects)
        {
            return $"{{{string.Join(",", objects)}}}";
        }

        public static void ForEach<T>(this IEnumerable<T> objects, Action<T> action)
        {
            foreach (var obj in objects) action(obj);
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
            if (arrays.Count == 1) return arrays[0];
            var output = lengthPerArray == -1
                ? new byte[arrays.Sum(arr => arr.Length)]
                : new byte[arrays.Count * lengthPerArray];
            var writeIdx = 0;
            foreach (var byteArr in arrays)
            {
                byteArr.CopyTo(output, writeIdx);
                writeIdx += byteArr.Length;
            }

            return output;
        }

        public static bool IsScript(this Type type, Type scriptType)
        {
            return !type.IsAbstract && type.IsSubclassOf(scriptType);
        }
    }

    /// <summary>
    ///     Some extension methods provided by RageCoop
    /// </summary>
    public static class PublicExtensions
    {
        /// <summary>
        ///     Get a SHA256 hashed byte array of the input string, internally used to hash password at client side.
        /// </summary>
        /// <param name="inputString"></param>
        /// <returns></returns>
        public static byte[] GetSHA256Hash(this string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
            {
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
            }
        }

        /// <summary>
        ///     Convert a byte array to hex-encoded string, internally used to trigger handshake event
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        /// <summary>
        ///     Convert a string to IP address
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static IPAddress ToIP(this string ip)
        {
            return IPAddress.Parse(ip);
        }
    }
}