using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
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
                    return (0x01, BitConverter.GetBytes((byte)obj));
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

        public static byte[] GetBytes(this string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }
        public static string GetString(this byte[] data)
        {
            return Encoding.UTF8.GetString(data);
        }
        

    }
}
