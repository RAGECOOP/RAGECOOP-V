using GTA.Math;
using System.IO;
using System.Text;

namespace RageCoop.Core
{
    internal class BitReader : BinaryReader
    {

        public BitReader(byte[] array) : base(new MemoryStream(array))
        {
        }

        ~BitReader()
        {
            Close();
            Dispose();
        }

        public byte[] ReadByteArray()
        {
            return base.ReadBytes(ReadInt32());
        }
        public override string ReadString()
        {
            return Encoding.UTF8.GetString(ReadBytes(ReadInt32()));
        }

        public Vector3 ReadVector3()
        {
            return new Vector3()
            {
                X = ReadSingle(),
                Y = ReadSingle(),
                Z = ReadSingle()
            };
        }
        public Vector2 ReadVector2()
        {
            return new Vector2()
            {
                X = ReadSingle(),
                Y = ReadSingle()
            };
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion()
            {
                X = ReadSingle(),
                Y = ReadSingle(),
                Z = ReadSingle(),
                W = ReadSingle()
            };
        }
    }
}
