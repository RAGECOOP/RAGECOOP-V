using System;
using System.Collections.Generic;
using System.Text;

using Lidgren.Network;

namespace RageCoop.Core
{
    public partial class Packets
    {
        public class Handshake : Packet
        {
            public int PedID { get; set; }

            public string Username { get; set; }

            public string ModVersion { get; set; }

            public bool NPCsAllowed { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.Handshake);

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

                // Write NpcsAllowed
                byteArray.Add(NPCsAllowed ? (byte)0x01 : (byte)0x00);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                // Read player netHandle
                PedID = reader.ReadInt();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);

                // Read ModVersion
                int modVersionLength = reader.ReadInt();
                ModVersion = reader.ReadString(modVersionLength);

                // Read NPCsAllowed
                NPCsAllowed = reader.ReadBool();
                #endregion
            }
        }

        public class PlayerConnect : Packet
        {
            public int PedID { get; set; }

            public string Username { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerConnect);

                List<byte> byteArray = new List<byte>();

                // Write NetHandle
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Get Username bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);

                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
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
            public int PedID { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerDisconnect);

                List<byte> byteArray = new List<byte>();

                byteArray.AddRange(BitConverter.GetBytes(PedID));

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                #region NetIncomingMessageToPacket
                BitReader reader = new BitReader(array);

                PedID = reader.ReadInt();
                #endregion
            }
        }

        public class PlayerPedID : Packet
        {
            /// <summary>
            /// Ped ID for this Player
            /// </summary>
            public int PedID { get; set; }
            public string Username { get; set; }
            public override void Pack(NetOutgoingMessage message)
            {
                #region PacketToNetOutGoingMessage
                message.Write((byte)PacketTypes.PlayerConnect);

                List<byte> byteArray = new List<byte>();

                // Write ID
                byteArray.AddRange(BitConverter.GetBytes(PedID));

                // Get Username bytes
                byte[] usernameBytes = Encoding.UTF8.GetBytes(Username);

                // Write UsernameLength
                byteArray.AddRange(BitConverter.GetBytes(usernameBytes.Length));

                // Write Username
                byteArray.AddRange(usernameBytes);

                byte[] result = byteArray.ToArray();

                message.Write(result.Length);
                message.Write(result);
                #endregion
            }

            public override void Unpack(byte[] array)
            {
                BitReader reader = new BitReader(array);

                // Read player ID
                PedID = reader.ReadInt();

                // Read Username
                int usernameLength = reader.ReadInt();
                Username = reader.ReadString(usernameLength);
            }
        }
    }
}
