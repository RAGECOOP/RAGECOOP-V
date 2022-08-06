using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    internal partial class Packets
    {
        internal class Handshake : Packet
        {
            public override PacketType Type { get { return PacketType.Handshake; } }
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
                PedID = reader.ReadInt();

                // Read Username
                Username = reader.ReadString(reader.ReadInt());

                // Read ModVersion
                ModVersion = reader.ReadString(reader.ReadInt());

                AesKeyCrypted=reader.ReadByteArray();

                AesIVCrypted=reader.ReadByteArray();


                PasswordEncrypted=reader.ReadByteArray();
                #endregion
            }
        }

        public class PlayerConnect : Packet
        {
            public override PacketType Type { get { return PacketType.PlayerConnect; } }
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
                PedID = reader.ReadInt();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);
                #endregion
            }
        }

        public class PlayerDisconnect : Packet
        {
            public override PacketType Type { get { return PacketType.PlayerDisconnect; } }
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

                PedID = reader.ReadInt();
                #endregion
            }
        }
        public class PlayerInfoUpdate : Packet
        {
            public override PacketType Type { get { return PacketType.PlayerInfoUpdate; } }

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
                PedID = reader.ReadInt();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);

                Latency=reader.ReadFloat();
            }
        }

        public class PublicKeyResponse : Packet
        {
            public override PacketType Type { get { return PacketType.PublicKeyResponse; } }

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
            public override PacketType Type { get { return PacketType.PublicKeyRequest; } }

            public override byte[] Serialize()
            {
                return new byte[0];
            }
            public override void Deserialize(byte[] array)
            {
            }
        }
    }
}
