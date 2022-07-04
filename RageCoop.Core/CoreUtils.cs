using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("RageCoop.Server")]
[assembly: InternalsVisibleTo("RageCoop.Client")]
namespace RageCoop.Core
{
    internal class CoreUtils
    {

        public static (byte, byte[]) GetBytesFromObject(object obj)
        {
            switch (obj)
            {
                case byte _:
                    return (0x01, new byte[] { (byte)obj });
                case short _:
                    return (0x02, BitConverter.GetBytes((short)obj));
                case ushort _:
                    return (0x03, BitConverter.GetBytes((ushort)obj));
                case int _:
                    return (0x04, BitConverter.GetBytes((int)obj));
                case uint _:
                    return (0x05, BitConverter.GetBytes((uint)obj));
                case long _:
                    return (0x06, BitConverter.GetBytes((long)obj));
                case ulong _:
                    return (0x07, BitConverter.GetBytes((ulong)obj));
                case float _:
                    return (0x08, BitConverter.GetBytes((float)obj));
                case bool _:
                    return (0x09, BitConverter.GetBytes((bool)obj));
                case string _:
                    return (0x10, ((string)obj).GetBytesWithLength());
                case Vector3 _:
                    return (0x11,((Vector3)obj).GetBytes());
                case Quaternion _:
                    return (0x12, ((Quaternion)obj).GetBytes());
                case GTA.Model _:
                    return (0x13, BitConverter.GetBytes((GTA.Model)obj));
                case Vector2 _:
                    return (0x14, ((Vector2)obj).GetBytes());
                case Tuple<byte, byte[]> _:
                    var tup = (Tuple<byte, byte[]>)obj;
                    return (tup.Item1, tup.Item2);
                default:
                    return (0x0, null);
            }
        }

    }
    internal static class Extensions
    {
        public static void AddVector3(this List<byte> bytes, Vector3 vec3)
        {
            bytes.AddRange(BitConverter.GetBytes(vec3.X));
            bytes.AddRange(BitConverter.GetBytes(vec3.Y));
            bytes.AddRange(BitConverter.GetBytes(vec3.Z));
        }
        public static void AddQuaternion(this List<byte> bytes, Quaternion quat)
        {
            bytes.AddRange(BitConverter.GetBytes(quat.X));
            bytes.AddRange(BitConverter.GetBytes(quat.Y));
            bytes.AddRange(BitConverter.GetBytes(quat.Z));
            bytes.AddRange(BitConverter.GetBytes(quat.W));
        }
        public static void AddInt(this List<byte> bytes,int i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUint(this List<byte> bytes, uint i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddShort(this List<byte> bytes, short i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUshort(this List<byte> bytes, ushort i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddLong(this List<byte> bytes, long i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddUlong(this List<byte> bytes, ulong i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddFloat(this List<byte> bytes, float i)
        {
            bytes.AddRange(BitConverter.GetBytes(i));
        }
        public static void AddString(this List<byte> bytes, string s)
        {
            var sb = Encoding.UTF8.GetBytes(s);
            bytes.AddInt(sb.Length);
            bytes.AddRange(sb);
        }
        public static void AddArray(this List<byte> bytes, byte[] toadd)
        {
            bytes.AddInt(toadd.Length);
            bytes.AddRange(toadd);
        }

        public static int GetHash(string s)
        {
            MD5 md5Hasher = MD5.Create();
            var hashed = md5Hasher.ComputeHash(Encoding.UTF8.GetBytes(s));
            return BitConverter.ToInt32(hashed, 0);
        }
        public static byte[] GetBytes(this string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
        public static byte[] GetBytesWithLength(this string s)
        {
            var data = new List<byte>(100);
            var sb = Encoding.UTF8.GetBytes(s);
            data.AddInt(sb.Length);
            data.AddRange(sb);
            return data.ToArray();
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

        public static bool HasPedFlag(this PedDataFlags flagToCheck, PedDataFlags flag)
        {
            return (flagToCheck & flag)!=0;
        }

        public static bool HasVehFlag(this VehicleDataFlags flagToCheck, VehicleDataFlags flag)
        {
            return (flagToCheck & flag)!=0;
        }
        public static bool HasConfigFlag(this PlayerConfigFlags flagToCheck, PlayerConfigFlags flag)
        {
            return (flagToCheck & flag)!=0;
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
            foreach(var obj in objects)
            {
                sb.Append(obj.GetType()+":"+obj.ToString()+"\n");
            }
            return sb.ToString();
        }
        public static string Dump<T>(this IEnumerable<T> objects)
        {
            return "{"+string.Join(",",objects)+"}";
        }
        public static void ForEach<T>(this IEnumerable<T> objects,Action<T> action)
        {
            foreach(var obj in objects)
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
        public static byte[] Join(this List<byte[]> arrays,int lengthPerArray=-1)
        {
            if (arrays.Count==1) { return arrays[0]; }
            var output = lengthPerArray== -1 ? new byte[arrays.Sum(arr => arr.Length)] : new byte[arrays.Count*lengthPerArray];
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
}
