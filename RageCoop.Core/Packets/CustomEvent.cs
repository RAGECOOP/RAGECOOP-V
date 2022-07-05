using System;
using System.Collections.Generic;
using System.Text;
using Lidgren.Network;
namespace RageCoop.Core
{
    internal partial class Packets
    {

        internal class CustomEvent : Packet
        {
            public CustomEvent(Func<byte,BitReader,object> onResolve = null)
            {
                _resolve= onResolve;
            }
            private Func<byte, BitReader, object> _resolve { get; set; }
            public int Hash { get; set; }
            public bool IsStaged { get; set; }=false;
            public object[] Args { get; set; }

            public override void Pack(NetOutgoingMessage message)
            {
                Args= Args ?? new object[] { };
                message.Write((byte)PacketType.CustomEvent);

                List<byte> result = new List<byte>();
                result.AddInt(Hash);
                result.AddInt(Args.Length);
                (byte, byte[]) tup;
                foreach (var arg in Args)
                {
                    tup=CoreUtils.GetBytesFromObject(arg);
                    if (tup.Item1==0||tup.Item2==null)
                    {
                        throw new ArgumentException($"Object of type {arg.GetType()} is not supported");
                    }
                    result.Add(tup.Item1);
                    result.AddRange(tup.Item2);
                }

                message.Write(result.Count);
                message.Write(result.ToArray());
            }

            public override void Unpack(byte[] array)
            {
                BitReader reader = new BitReader(array);

                Hash = reader.ReadInt();
                var len=reader.ReadInt();
                Args=new object[len];
                for (int i = 0; i < len; i++)
                {
                    byte type = reader.ReadByte();
                    switch (type)
                    {
                        case 0x01:
                            Args[i]=reader.ReadByte(); break;
                        case 0x02:
                            Args[i]=reader.ReadShort(); break;
                        case 0x03:
                            Args[i]=reader.ReadUShort(); break;
                        case 0x04:
                            Args[i]=reader.ReadInt(); break;
                        case 0x05:
                            Args[i]=reader.ReadUInt(); break;
                        case 0x06:
                            Args[i]=reader.ReadLong(); break;
                        case 0x07:
                            Args[i]=reader.ReadULong(); break;
                        case 0x08:
                            Args[i]=reader.ReadFloat(); break;
                        case 0x09:
                            Args[i]=reader.ReadBool(); break;
                        case 0x10:
                            Args[i]=reader.ReadString(); break;
                        case 0x11: 
                            Args[i]=reader.ReadVector3(); break;
                        case 0x12:
                            Args[i]=reader.ReadQuaternion(); break;
                        case 0x13:
                            Args[i]=(GTA.Model)reader.ReadInt(); break;
                        case 0x14:
                            Args[i]=reader.ReadVector2(); break;
                        default:
                            if (_resolve==null)
                            {
                                throw new InvalidOperationException($"Unexpected type:{type}\r\n{array.Dump()}");
                            }
                            else
                            {
                                Args[i]=_resolve(type, reader); break;
                            }
                    }
                }
            }
        }
    }
}
