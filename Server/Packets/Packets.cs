using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;
using Newtonsoft.Json;

namespace CoopServer
{
    public static class VectorExtensions
    {
        public static LVector3 Normalize(this LVector3 value)
        {
            float value2 = value.Length();
            return value / value2;
        }

        public static float Distance(this LVector3 value1, LVector3 value2)
        {
            LVector3 vector = value1 - value2;
            float num = Dot(vector, vector);
            return (float)Math.Sqrt(num);
        }

        public static float Dot(this LVector3 vector1, LVector3 vector2)
        {
            return vector1.X * vector2.X + vector1.Y * vector2.Y + vector1.Z * vector2.Z;
        }
    }

    public struct LVector3
    {
        public LVector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        #region SERVER-ONLY
        public float Length() => (float)Math.Sqrt((X * X) + (Y * Y) + (Z * Z));
        public static LVector3 Subtract(LVector3 pos1, LVector3 pos2) => new(pos1.X - pos2.X, pos1.Y - pos2.Y, pos1.Z - pos2.Z);
        public static bool Equals(LVector3 value1, LVector3 value2) => value1.X == value2.X && value1.Y == value2.Y && value1.Z == value2.Z;
        public static LVector3 operator /(LVector3 value1, float value2)
        {
            float num = 1f / value2;
            return new LVector3(value1.X * num, value1.Y * num, value1.Z * num);
        }
        public static LVector3 operator -(LVector3 left, LVector3 right)
        {
            return new LVector3(left.X - right.X, left.Y - right.Y, left.Z - right.Z);
        }
        public static LVector3 operator -(LVector3 value)
        {
            return default(LVector3) - value;
        }
        #endregion
    }

    public struct LQuaternion
    {
        public LQuaternion(float X, float Y, float Z, float W)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
            this.W = W;
        }

        public float X { get; set; }

        public float Y { get; set; }

        public float Z { get; set; }

        public float W { get; set; }
    }

    enum PacketTypes
    {
        Handshake,
        PlayerConnect,
        PlayerDisconnect,
        FullSyncPlayer,
        FullSyncPlayerVeh,
        LightSyncPlayer,
        LightSyncPlayerVeh,
        SuperLightSync,
        FullSyncNpc,
        FullSyncNpcVeh,
        ChatMessage,
        NativeCall,
        NativeResponse,
        Mod,
        CleanUpWorld,
        FileTransferTick,
        FileTransferRequest,
        FileTransferComplete
    }

    enum ConnectionChannel
    {
        Default = 0,
        PlayerLight = 1,
        PlayerFull = 2,
        PlayerSuperLight = 3,
        NPCFull = 4,
        Chat = 5,
        Native = 6,
        Mod = 7,
        File = 8
    }

    [Flags]
    enum PedDataFlags
    {
        IsAiming = 1 << 0,
        IsShooting = 1 << 1,
        IsReloading = 1 << 2,
        IsJumping = 1 << 3,
        IsRagdoll = 1 << 4,
        IsOnFire = 1 << 5,
        IsInParachuteFreeFall = 1 << 6,
        IsParachuteOpen = 1 << 7,
        IsOnLadder = 1 << 8,
        IsVaulting = 1 << 9
    }

    #region ===== VEHICLE DATA =====
    [Flags]
    enum VehicleDataFlags
    {
        IsEngineRunning = 1 << 0,
        AreLightsOn = 1 << 1,
        AreBrakeLightsOn = 1 << 2,
        AreHighBeamsOn = 1 << 3,
        IsSirenActive = 1 << 4,
        IsDead = 1 << 5,
        IsHornActive = 1 << 6,
        IsTransformed = 1 << 7,
        RoofOpened = 1 << 8,
        OnTurretSeat = 1 << 9,
        IsPlane = 1 << 10
    }

    struct VehicleDamageModel
    {
        public byte BrokenWindows { get; set; }

        public byte BrokenDoors { get; set; }

        public ushort BurstedTires { get; set; }

        public ushort PuncturedTires { get; set; }
    }
    #endregion

    interface IPacket
    {
        void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        void NetIncomingMessageToPacket(byte[] array);
    }

    abstract class Packet : IPacket
    {
        public abstract void PacketToNetOutGoingMessage(NetOutgoingMessage message);
        public abstract void NetIncomingMessageToPacket(byte[] array);
    }

    internal partial class Packets
    {
        public class Mod : Packet
        {
            public long NetHandle { get; set; }

            public long Target { get; set; }

            public string Name { get; set; }

            public byte CustomPacketID { get; set; }

            public byte[] Bytes { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.Mod);

                List<byte> byteArray = new List<byte>();

                // Write NetHandle
                byteArray.AddRange(BitConverter.GetBytes(NetHandle));

                // Write Target
                byteArray.AddRange(BitConverter.GetBytes(Target));

                // Write Name
                byte[] nameBytes = Encoding.UTF8.GetBytes(Name);
                byteArray.AddRange(BitConverter.GetBytes(nameBytes.Length));
                byteArray.AddRange(nameBytes);

                // Write CustomPacketID
                byteArray.Add(CustomPacketID);

                // Write Bytes
                byteArray.AddRange(BitConverter.GetBytes(Bytes.Length));
                byteArray.AddRange(Bytes);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read NetHandle
                NetHandle = reader.ReadLong();

                // Read Target
                Target = reader.ReadLong();

                // Read Name
                int nameLength = reader.ReadInt();
                Name = reader.ReadString(nameLength);

                // Read CustomPacketID
                CustomPacketID = reader.ReadByte();

                // Read Bytes
                int bytesLength = reader.ReadInt();
                Bytes = reader.ReadByteArray(bytesLength);
                #endregion
            }
        }

        public class ChatMessage : Packet
        {
            public string Username { get; set; }

            public string Message { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.ChatMessage);

                List<byte> byteArray = new List<byte>();

                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);
                byte[] messageBytes = Encoding.UTF8.GetBytes(Message);

                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                // Write MessageLength
                byteArray.AddRange(BitConverter.GetBytes(messageBytes.Length));

                // Write Message
                byteArray.AddRange(messageBytes);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);

                // Read message
                int messageLength = reader.ReadInt();
                Message = reader.ReadString(messageLength);
                #endregion
            }
        }

        #region ===== NATIVECALL =====
        public class NativeCall : Packet
        {
            public ulong Hash { get; set; }

            public List<object> Args { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.NativeCall);

                List<byte> byteArray = new List<byte>();

                // Write Hash
                byteArray.AddRange(BitConverter.GetBytes(Hash));

                // Write Args
                byteArray.AddRange(BitConverter.GetBytes(Args.Count));
                Args.ForEach(x =>
                {
                    Type type = x.GetType();

                    if (type == typeof(int))
                    {
                        byteArray.Add(0x00);
                        byteArray.AddRange(BitConverter.GetBytes((int)x));
                    }
                    else if (type == typeof(bool))
                    {
                        byteArray.Add(0x01);
                        byteArray.AddRange(BitConverter.GetBytes((bool)x));
                    }
                    else if (type == typeof(float))
                    {
                        byteArray.Add(0x02);
                        byteArray.AddRange(BitConverter.GetBytes((float)x));
                    }
                    else if (type == typeof(string))
                    {
                        byteArray.Add(0x03);
                        byte[] stringBytes = Encoding.UTF8.GetBytes((string)x);
                        byteArray.AddRange(BitConverter.GetBytes(stringBytes.Length));
                        byteArray.AddRange(stringBytes);
                    }
                    else if (type == typeof(LVector3))
                    {
                        byteArray.Add(0x04);
                        LVector3 vector = (LVector3)x;
                        byteArray.AddRange(BitConverter.GetBytes(vector.X));
                        byteArray.AddRange(BitConverter.GetBytes(vector.Y));
                        byteArray.AddRange(BitConverter.GetBytes(vector.Z));
                    }
                });

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read Hash
                Hash = reader.ReadULong();

                // Read Args
                Args = new List<object>();
                int argsLength = reader.ReadInt();
                for (int i = 0; i < argsLength; i++)
                {
                    byte argType = reader.ReadByte();
                    switch (argType)
                    {
                        case 0x00:
                            Args.Add(reader.ReadInt());
                            break;
                        case 0x01:
                            Args.Add(reader.ReadBool());
                            break;
                        case 0x02:
                            Args.Add(reader.ReadFloat());
                            break;
                        case 0x03:
                            int stringLength = reader.ReadInt();
                            Args.Add(reader.ReadString(stringLength));
                            break;
                        case 0x04:
                            Args.Add(new LVector3()
                            {
                                X = reader.ReadFloat(),
                                Y = reader.ReadFloat(),
                                Z = reader.ReadFloat()
                            });
                            break;
                    }
                }
                #endregion
            }
        }

        public class NativeResponse : Packet
        {
            public ulong Hash { get; set; }

            public List<object> Args { get; set; }

            public byte? ResultType { get; set; }

            public long ID { get; set; }

            public override void PacketToNetOutGoingMessage(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.NativeResponse);

                List<byte> byteArray = new List<byte>();

                // Write Hash
                byteArray.AddRange(BitConverter.GetBytes(Hash));

                Type type;

                // Write Args
                byteArray.AddRange(BitConverter.GetBytes(Args.Count));
                Args.ForEach(x =>
                {
                    type = x.GetType();

                    if (type == typeof(int))
                    {
                        byteArray.Add(0x00);
                        byteArray.AddRange(BitConverter.GetBytes((int)x));
                    }
                    else if (type == typeof(bool))
                    {
                        byteArray.Add(0x01);
                        byteArray.AddRange(BitConverter.GetBytes((bool)x));
                    }
                    else if (type == typeof(float))
                    {
                        byteArray.Add(0x02);
                        byteArray.AddRange(BitConverter.GetBytes((float)x));
                    }
                    else if (type == typeof(string))
                    {
                        byteArray.Add(0x03);
                        byte[] stringBytes = Encoding.UTF8.GetBytes((string)x);
                        byteArray.AddRange(BitConverter.GetBytes(stringBytes.Length));
                        byteArray.AddRange(stringBytes);
                    }
                    else if (type == typeof(LVector3))
                    {
                        byteArray.Add(0x04);
                        LVector3 vector = (LVector3)x;
                        byteArray.AddRange(BitConverter.GetBytes(vector.X));
                        byteArray.AddRange(BitConverter.GetBytes(vector.Y));
                        byteArray.AddRange(BitConverter.GetBytes(vector.Z));
                    }
                });

                byteArray.AddRange(BitConverter.GetBytes(ID));

                // Write type of result
                if (ResultType.HasValue)
                {
                    byteArray.Add(ResultType.Value);
                }

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void NetIncomingMessageToPacket(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read Hash
                Hash = reader.ReadULong();

                // Read Args
                Args = new List<object>();
                int argsLength = reader.ReadInt();
                for (int i = 0; i < argsLength; i++)
                {
                    byte argType = reader.ReadByte();
                    switch (argType)
                    {
                        case 0x00:
                            Args.Add(reader.ReadInt());
                            break;
                        case 0x01:
                            Args.Add(reader.ReadBool());
                            break;
                        case 0x02:
                            Args.Add(reader.ReadFloat());
                            break;
                        case 0x03:
                            int stringLength = reader.ReadInt();
                            Args.Add(reader.ReadString(stringLength));
                            break;
                        case 0x04:
                            Args.Add(new LVector3()
                            {
                                X = reader.ReadFloat(),
                                Y = reader.ReadFloat(),
                                Z = reader.ReadFloat()
                            });
                            break;
                    }
                }

                ID = reader.ReadLong();

                // Read type of result
                if (reader.CanRead(1))
                {
                    ResultType = reader.ReadByte();
                }
                #endregion
            }
        }
        #endregion // ===== NATIVECALL =====
    }

    public static class CoopSerializer
    {
        public static byte[] Serialize(this object obj)
        {
            if (obj == null)
            {
                return null;
            }

            string jsonString = JsonConvert.SerializeObject(obj);
            return System.Text.Encoding.UTF8.GetBytes(jsonString);
        }

        public static T Deserialize<T>(this byte[] bytes) where T : class
        {
            if (bytes == null)
            {
                return null;
            }

            var jsonString = System.Text.Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(jsonString);
        }
    }
}
