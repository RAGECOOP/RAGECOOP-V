using System;
using System.Collections.Generic;
using System.Text;

using System.Net;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class Handshake : Packet
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
            public override byte[] Serialize()
            {

                List<byte> byteArray = new List<byte>();

                // Write ID
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Get Username bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);


                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                // Write Latency
                byteArray.AddFloat(Latency);

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
