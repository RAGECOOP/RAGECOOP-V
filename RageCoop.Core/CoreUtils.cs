using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using System.Security.Cryptography;
using System.Net;
namespace RageCoop.Core
{
    public class CoreUtils
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
                    return (0x10, (obj as string).GetBytesWithLength());
                default:
                    return (0x0, null);
            }
        }

    }
    public static class Extensions
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
    }
}
