using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GTA.Math;
using GTA;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using Lidgren.Network;

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
        #region BYTE-LIST

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
        public static void AddInt(this List<byte> bytes, int i)
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

        #endregion

        #region MESSAGE-WRITE
        public static void Write(this NetOutgoingMessage msg,Vector3 vec)
        {
            msg.Write(vec.X);
            msg.Write(vec.Y);
            msg.Write(vec.Z);
        }
        public static void Write(this NetOutgoingMessage msg, Quaternion quat)
        {
            msg.Write(quat.W);
            msg.Write(quat.X);
            msg.Write(quat.Y);
            msg.Write(quat.Z);
        }


        public static void Write(this NetOutgoingMessage msg, PedData p)
        {
            if(p== null)
            {
                throw new Exception("null ped data");
            }
            // Write ped ID
            msg.Write(p.ID);

            // Write OwnerID
            msg.Write(p.OwnerID);

            // Write ped flags
            msg.Write((ushort)p.Flag);

            // Write ped health
            msg.Write(p.Health);

            // Write ped position
            msg.Write(p.Position);

            // Write ped rotation
            msg.Write(p.Rotation);

            // Write ped velocity
            msg.Write(p.Velocity);

            if (p.Flag.HasPedFlag(PedDataFlags.IsRagdoll))
            {
                msg.Write(p.RotationVelocity);
            }

            // Write ped speed
            msg.Write(p.Speed);

            // Write ped weapon hash
            msg.Write(p.CurrentWeaponHash);

            if (p.Flag.HasPedFlag(PedDataFlags.IsAiming))
            {
                // Write ped aim coords
                msg.Write(p.AimCoords);
            }

            msg.Write(p.Heading);

            if (p.State != null)
            {
                msg.Write(true);
                Write(msg, p.State);
            }
            else
            {
                msg.Write(false);
            }
        }
        public static void Write(this NetOutgoingMessage msg, PedStateData p)
        {

            // Write model hash
            msg.Write(p.ModelHash);

            msg.Write(p.Clothes);


            // Write player weapon components
            if (p.WeaponComponents != null)
            {
                msg.Write(true);
                msg.Write((ushort)p.WeaponComponents.Count);
                foreach (KeyValuePair<uint, bool> component in p.WeaponComponents)
                {
                    msg.Write(component.Key);
                    msg.Write(component.Value);
                }
            }
            else
            {
                // weapon doesn't have any components
                msg.Write(false);
            }

            msg.Write(p.WeaponTint);

            msg.Write((byte)p.BlipColor);
            if ((byte)p.BlipColor!=255)
            {
                msg.Write((ushort)p.BlipSprite);
                msg.Write(p.BlipScale);
            }
        }
        
        public static void Write(this NetOutgoingMessage msg, VehicleData v)
        {
            // Write vehicle id
            msg.Write(v.ID);

            msg.Write(v.OwnerID);

            // Write position
            msg.Write(v.Position);


            // Write quaternion
            msg.Write(v.Quaternion);

            // Write velocity
            msg.Write(v.Velocity);

            // Write rotation velocity
            msg.Write(v.RotationVelocity);


            msg.Write(v.ThrottlePower);

            msg.Write(v.BrakePower);

            // Write vehicle steering angle
            msg.Write(v.SteeringAngle);

            if (v.DeluxoWingRatio!=-1)
            {
                msg.Write(true);
                msg.Write(v.DeluxoWingRatio);
            }
            else
            {
                msg.Write(false);
            }
            if (v.State!=null)
            {
                msg.Write(true);
                msg.Write(v.State);
            }
            else
            {
                msg.Write(false);
            }
        }
        public static void Write(this NetOutgoingMessage msg, VehicleStateData v)
        {

            //Write vehicle flag
            msg.Write((ushort)v.Flag);

            // Write vehicle model hash
            msg.Write(v.ModelHash);


            // Write vehicle engine health
            msg.Write(v.EngineHealth);

            // Check
            if (v.Flag.HasVehFlag(VehicleDataFlags.IsAircraft))
            {
                // Write the vehicle landing gear
                msg.Write(v.LandingGear);
            }
            if (v.Flag.HasVehFlag(VehicleDataFlags.HasRoof))
            {
                msg.Write(v.RoofState);
            }

            // Write vehicle colors
            msg.Write(v.Colors[0]);
            msg.Write(v.Colors[1]);

            // Write vehicle mods
            // Write the count of mods
            msg.Write((short)v.Mods.Count);
            // Loop the dictionary and add the values
            foreach (KeyValuePair<int, int> mod in v.Mods)
            {
                // Write the mod value
                msg.Write(mod.Key);
                msg.Write(mod.Value);
            }

            if (!v.DamageModel.Equals(default(VehicleDamageModel)))
            {
                // Write boolean = true
                msg.Write(true);
                // Write vehicle damage model
                msg.Write(v.DamageModel.BrokenDoors);
                msg.Write(v.DamageModel.OpenedDoors);
                msg.Write(v.DamageModel.BrokenWindows);
                msg.Write(v.DamageModel.BurstedTires);
                msg.Write(v.DamageModel.LeftHeadLightBroken);
                msg.Write(v.DamageModel.RightHeadLightBroken);
            }
            else
            {
                // Write boolean = false
                msg.Write(false);
            }

            // Write passengers
            msg.Write(v.Passengers.Count);

            foreach (KeyValuePair<int, int> p in v.Passengers)
            {
                msg.Write(p.Key);
                msg.Write(p.Value);
            }



            // Write LockStatus
            msg.Write((byte)v.LockStatus);

            // Write RadioStation
            msg.Write(v.RadioStation);

            //　Write LicensePlate
            while (v.LicensePlate.Length<8)
            {
                v.LicensePlate+=" ";
            }
            if (v.LicensePlate.Length>8)
            {
                v.LicensePlate=new string(v.LicensePlate.Take(8).ToArray());
            }
            msg.Write(Encoding.ASCII.GetBytes(v.LicensePlate));

            msg.Write((byte)(v.Livery+1));
        }
        #endregion
        #region MESSAGE-READ
        public static Vector3 ReadVector3(this NetIncomingMessage msg)
        {
            return new Vector3()
            {
                X=msg.ReadFloat(),
                Y=msg.ReadFloat(),
                Z=msg.ReadFloat(),
            };
        }
        public static Quaternion ReadQuaternion(this NetIncomingMessage msg)
        {
            return new Quaternion()
            {
                W=msg.ReadFloat(),
                X=msg.ReadFloat(),
                Y=msg.ReadFloat(),
                Z=msg.ReadFloat(),
            };
        }


        public static PedData ReadPed(this NetIncomingMessage msg)
        {
            PedData p = new PedData();
            p.ID = msg.ReadInt32();

            p.OwnerID = msg.ReadInt32();

            // Read player flags
            p.Flag = (PedDataFlags)msg.ReadInt16();

            // Read player health
            p.Health = msg.ReadInt32();

            // Read player position
            p.Position = msg.ReadVector3();

            // Read player rotation
            p.Rotation = msg.ReadVector3();

            // Read player velocity
            p.Velocity = msg.ReadVector3();

            // Read rotation velocity if in ragdoll
            if (p.Flag.HasPedFlag(PedDataFlags.IsRagdoll))
            {
                p.RotationVelocity=msg.ReadVector3();
            }

            // Read player speed
            p.Speed = msg.ReadByte();

            // Read player weapon hash
            p.CurrentWeaponHash = msg.ReadUInt32();

            // Try to read aim coords
            if (p.Flag.HasPedFlag(PedDataFlags.IsAiming))
            {
                // Read player aim coords
                p.AimCoords = msg.ReadVector3();
            }

            p.Heading=msg.ReadFloat();
            if (msg.ReadBoolean())
            {
                p.State=msg.ReadPedState();
            }
            return p;
        }
        public static PedStateData ReadPedState(this NetIncomingMessage msg)
        {
            var p = new PedStateData();

            // Read player model hash
            p.ModelHash = msg.ReadInt32();

            // Read player clothes
            p.Clothes =msg.ReadBytes(36);


            // Read player weapon components
            if (msg.ReadBoolean())
            {
                p.WeaponComponents = new Dictionary<uint, bool>();
                ushort comCount = msg.ReadUInt16();
                for (ushort i = 0; i < comCount; i++)
                {
                    p.WeaponComponents.Add(msg.ReadUInt32(), msg.ReadBoolean());
                }
            }
            p.WeaponTint=msg.ReadByte();

            p.BlipColor=(BlipColor)msg.ReadByte();

            if ((byte)p.BlipColor!=255)
            {
                p.BlipSprite=(BlipSprite)msg.ReadUInt16();
                p.BlipScale=msg.ReadFloat();
            }
            return p;
        }

        public static VehicleData ReadVehicle(this NetIncomingMessage msg)
        {
            VehicleData v = new VehicleData()
            {
                // Read vehicle id
                ID = msg.ReadInt32(),

                // Read owner id
                OwnerID = msg.ReadInt32(),

                // Read position
                Position = msg.ReadVector3(),

                // Read quaternion
                Quaternion=msg.ReadQuaternion(),

                // Read velocity
                Velocity =msg.ReadVector3(),

                // Read rotation velocity
                RotationVelocity=msg.ReadVector3(),

                // Read throttle power
                ThrottlePower=msg.ReadFloat(),

                // Read brake power
                BrakePower=msg.ReadFloat(),

                // Read steering angle
                SteeringAngle = msg.ReadFloat(),
            };
            if (msg.ReadBoolean())
            {
                v.DeluxoWingRatio= msg.ReadFloat();
            }
            if (msg.ReadBoolean()) { 
                v.State=msg.ReadVehicleState();
            }
            return v;
        }
        public static VehicleStateData ReadVehicleState(this NetIncomingMessage msg)
        {
            var v = new VehicleStateData();

            // Read vehicle flags
            v.Flag = (VehicleDataFlags)msg.ReadUInt16();

            // Read vehicle model hash
            v.ModelHash = msg.ReadInt32();

            // Read vehicle engine health
            v.EngineHealth = msg.ReadFloat();


            // Check
            if (v.Flag.HasVehFlag(VehicleDataFlags.IsAircraft))
            {
                // Read vehicle landing gear
                v.LandingGear = msg.ReadByte();
            }
            if (v.Flag.HasVehFlag(VehicleDataFlags.HasRoof))
            {
                v.RoofState=msg.ReadByte();
            }

            // Read vehicle colors
            byte vehColor1 = msg.ReadByte();
            byte vehColor2 = msg.ReadByte();
            v.Colors = new byte[] { vehColor1, vehColor2 };

            // Read vehicle mods
            // Create new Dictionary
            v.Mods = new Dictionary<int, int>();
            // Read count of mods
            short vehModCount = msg.ReadInt16();
            // Loop
            for (int i = 0; i < vehModCount; i++)
            {
                // Read the mod value
                v.Mods.Add(msg.ReadInt32(), msg.ReadInt32());
            }

            if (msg.ReadBoolean())
            {
                // Read vehicle damage model
                v.DamageModel = new VehicleDamageModel()
                {
                    BrokenDoors = msg.ReadByte(),
                    OpenedDoors=msg.ReadByte(),
                    BrokenWindows = msg.ReadByte(),
                    BurstedTires = msg.ReadInt16(),
                    LeftHeadLightBroken = msg.ReadByte(),
                    RightHeadLightBroken = msg.ReadByte()
                };
            }


            // Read Passengers
            v.Passengers=new Dictionary<int, int>();
            int count = msg.ReadInt32();
            for (int i = 0; i<count; i++)
            {
                int seat, id;
                seat = msg.ReadInt32();
                id = msg.ReadInt32();
                v.Passengers.Add(seat, id);

            }


            // Read LockStatus
            v.LockStatus=(VehicleLockStatus)msg.ReadByte();

            // Read RadioStation
            v.RadioStation=msg.ReadByte();

            v.LicensePlate=Encoding.ASCII.GetString(msg.ReadBytes(8));

            v.Livery=msg.ReadByte()-1;

            return v;
        }
        #endregion
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
