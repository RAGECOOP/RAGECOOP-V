using System;
using System.Collections.Generic;
using System.Text;
using GTA.Math;
using System.Net;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal struct PlayerData
        {
            public int ID;
            public string Username;
        }
        public class Handshake : Packet
        {
            public override PacketType Type  => PacketType.Handshake;
            public int PedID { get; set; }

            public string Username { get; set; }

            public string ModVersion { get; set; }

            /// <summary>
            /// The asymetrically crypted Aes key
            /// </summary>
            public byte[] AesKeyCrypted;

            /// <summary>
            /// The asymetrically crypted Aes IV
            /// </summary>
            public byte[] AesIVCrypted;

            /// <summary>
            /// The password hash with client Aes
            /// </summary>
            public byte[] PasswordEncrypted { get; set; }

            public IPEndPoint InternalEndPoint { get; set; }
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write Player Ped ID
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Write Username
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));
                byteArray.AddRange(usernameBytes);

                // Write ModVersion
                byte[] modVersionBytes = Encoding.UTF8.GetBytes(ModVersion);
                byteArray.AddRange(BitConverter.GetBytes(modVersionBytes.Length));
                byteArray.AddRange(modVersionBytes);

                byteArray.AddString(InternalEndPoint.ToString());

                // Write AesKeyCrypted
                byteArray.AddArray(AesKeyCrypted);

                // Write AesIVCrypted
                byteArray.AddArray(AesIVCrypted);


                // Write PassHash
                byteArray.AddArray(PasswordEncrypted);

                return byteArray.ToArray();

            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                PedID = reader.ReadInt32();

                // Read Username
                Username = reader.ReadString();

                // Read ModVersion
                ModVersion = reader.ReadString();

                InternalEndPoint=CoreUtils.StringToEndPoint(reader.ReadString());

                AesKeyCrypted=reader.ReadByteArray();

                AesIVCrypted=reader.ReadByteArray();


                PasswordEncrypted=reader.ReadByteArray();
                #endregion
            }
        }
        public class HandshakeSuccess : Packet
        {
            public PlayerData[] Players { get; set; }
            public override PacketType Type => PacketType.HandshakeSuccess;
            public override byte[] Serialize()
            {
                var data = new List<byte>();
                data.AddInt(Players.Length);
                foreach(var p in Players)
                {
                    data.AddInt(p.ID);
                    data.AddString(p.Username);
                }
                return data.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                var reader = new BitReader(array);
                Players=new PlayerData[reader.ReadInt32()];
                for(int i = 0; i<Players.Length; i++)
                {
                    Players[i]=new PlayerData()
                    {
                        ID=reader.ReadInt32(),
                        Username=reader.ReadString(),
                    };
                }
            }
        }
        public class PlayerConnect : Packet
        {
            public override PacketType Type  => PacketType.PlayerConnect;
            public int PedID { get; set; }

            public string Username { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write NetHandle
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Get Username bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);

                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                PedID = reader.ReadInt32();

                // Read Username
                Username = reader.ReadString();
                #endregion
            }
        }

        public class PlayerDisconnect : Packet
        {
            public override PacketType Type  => PacketType.PlayerDisconnect;
            public int PedID { get; set; }

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddRange(BitConverter.GetBytes(PedID));

                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                PedID = reader.ReadInt32();
                #endregion
            }
        }
        public class PlayerInfoUpdate : Packet
        {
            public override PacketType Type  => PacketType.PlayerInfoUpdate;

            /// <summary>
            /// Ped ID for this Player
            /// </summary>
            public int PedID { get; set; }
            public string Username { get; set; }
            public float Latency { get; set; }
            public Vector3 Position { get; set; }
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write ID
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Write Username
                byteArray.AddString(Username);

                // Write Latency
                byteArray.AddFloat(Latency);

                byteArray.AddVector3(Position);

                return byteArray.ToArray();
            }

            public override void Deserialize(byte[] array)
            {
                BitReader reader = new BitReader(array);

                // Read player ID
                PedID = reader.ReadInt32();

                // Read Username
                Username = reader.ReadString();

                Latency=reader.ReadSingle();

                Position=reader.ReadVector3();
            }
        }

        public class PublicKeyResponse : Packet
        {
            public override PacketType Type  => PacketType.PublicKeyResponse;

            public byte[] Modulus;
            public byte[] Exponent;

            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                byteArray.AddArray(Modulus);

                byteArray.AddArray(Exponent);


                return byteArray.ToArray();
            }
            public override void Deserialize(byte[] array)
            {
                #region NetIncomingMessageToPacket
                var reader=new BitReader(array);
                Modulus=reader.ReadByteArray();
                Exponent=reader.ReadByteArray();

                #endregion
            }
        }

        public class PublicKeyRequest : Packet
        {
            public override PacketType Type  => PacketType.PublicKeyRequest;
        }
    }
}
