using GTA.Math;
using Lidgren.Network;
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
            public override PacketType Type => PacketType.Handshake;
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
            protected override void Serialize(NetOutgoingMessage m)
            {

                // Write Player Ped ID
                m.Write(PedID);

                // Write Username
                m.Write(Username);

                // Write ModVersion
                m.Write(ModVersion);

                m.Write(InternalEndPoint.ToString());

                // Write AesKeyCrypted
                m.WriteByteArray(AesKeyCrypted);

                // Write AesIVCrypted
                m.WriteByteArray(AesIVCrypted);


                // Write PassHash
                m.WriteByteArray(PasswordEncrypted);


            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket


                // Read player netHandle
                PedID = m.ReadInt32();

                // Read Username
                Username = m.ReadString();

                // Read ModVersion
                ModVersion = m.ReadString();

                InternalEndPoint = CoreUtils.StringToEndPoint(m.ReadString());

                AesKeyCrypted = m.ReadByteArray();

                AesIVCrypted = m.ReadByteArray();


                PasswordEncrypted = m.ReadByteArray();
                #endregion
            }
        }
        public class HandshakeSuccess : Packet
        {
            public PlayerData[] Players { get; set; }
            public override PacketType Type => PacketType.HandshakeSuccess;
            protected override void Serialize(NetOutgoingMessage m)
            {
                m.Write(Players.Length);
                foreach (var p in Players)
                {
                    m.Write(p.ID);
                    m.Write(p.Username);
                }
            }
            public override void Deserialize(NetIncomingMessage m)
            {

                Players = new PlayerData[m.ReadInt32()];
                for (int i = 0; i < Players.Length; i++)
                {
                    Players[i] = new PlayerData()
                    {
                        ID = m.ReadInt32(),
                        Username = m.ReadString(),
                    };
                }
            }
        }
        public class PlayerConnect : Packet
        {
            public override PacketType Type => PacketType.PlayerConnect;
            public int PedID { get; set; }

            public string Username { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {

                // Write NetHandle
                m.Write(PedID);

                m.Write(Username);
            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                // Read player netHandle
                PedID = m.ReadInt32();

                // Read Username
                Username = m.ReadString();
                #endregion
            }
        }

        public class PlayerDisconnect : Packet
        {
            public override PacketType Type => PacketType.PlayerDisconnect;
            public int PedID { get; set; }

            protected override void Serialize(NetOutgoingMessage m)
            {

                m.Write(PedID);

            }

            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                PedID = m.ReadInt32();
                #endregion
            }
        }
        public class PlayerInfoUpdate : Packet
        {
            public override PacketType Type => PacketType.PlayerInfoUpdate;

            /// <summary>
            /// Ped ID for this Player
            /// </summary>
            public int PedID { get; set; }
            public string Username { get; set; }
            public float Latency { get; set; }
            public Vector3 Position { get; set; }
            public bool IsHost;
            protected override void Serialize(NetOutgoingMessage m)
            {

                // Write ID
                m.Write(PedID);

                // Write Username
                m.Write(Username);

                // Write Latency
                m.Write(Latency);

                m.Write(Position);

                m.Write(IsHost);
            }

            public override void Deserialize(NetIncomingMessage m)
            {


                // Read player ID
                PedID = m.ReadInt32();

                // Read Username
                Username = m.ReadString();

                Latency = m.ReadFloat();

                Position = m.ReadVector3();

                IsHost = m.ReadBoolean();
            }
        }

        public class PublicKeyResponse : Packet
        {
            public override PacketType Type => PacketType.PublicKeyResponse;

            public byte[] Modulus;
            public byte[] Exponent;

            protected override void Serialize(NetOutgoingMessage m)
            {



                m.WriteByteArray(Modulus);

                m.WriteByteArray(Exponent);



            }
            public override void Deserialize(NetIncomingMessage m)
            {
                #region NetIncomingMessageToPacket

                Modulus = m.ReadByteArray();
                Exponent = m.ReadByteArray();

                #endregion
            }
        }

        public class PublicKeyRequest : Packet
        {
            public override PacketType Type => PacketType.PublicKeyRequest;
        }
    }
}
