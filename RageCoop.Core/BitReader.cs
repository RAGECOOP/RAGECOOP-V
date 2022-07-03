using System;
using System.Text;
using System.Linq;
using GTA.Math;

namespace RageCoop.Core
{
    internal class BitReader
    {
        public int CurrentIndex { get; set; }

        private byte[] ResultArray;

        public BitReader(byte[] array)
        {
            CurrentIndex = 0;
            ResultArray = array;
        }

        ~BitReader()
        {
            ResultArray = null;
        }

        public bool CanRead(int bytes)
        {
            return ResultArray.Length >= CurrentIndex + bytes;
        }

        public bool ReadBool()
        {
            bool value = BitConverter.ToBoolean(ResultArray, CurrentIndex);
            CurrentIndex += 1;
            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(ResultArray, CurrentIndex);
            CurrentIndex += 4;
            return value;
        }

        public byte ReadByte()
        {
            byte value = ResultArray[CurrentIndex];
            CurrentIndex += 1;
            return value;
        }

        public byte[] ReadByteArray(int length)
        {
            byte[] value = ResultArray.Skip(CurrentIndex).Take(length).ToArray();
            CurrentIndex += length;
            return value;
        }
        public byte[] ReadByteArray()
        {
            return ReadByteArray(ReadInt());
        }
        public short ReadShort()
        {
            short value = BitConverter.ToInt16(ResultArray, CurrentIndex);
            CurrentIndex += 2;
            return value;
        }

        public ushort ReadUShort()
        {
            ushort value = BitConverter.ToUInt16(ResultArray, CurrentIndex);
            CurrentIndex += 2;
            return value;
        }

        public int ReadInt()
        {
            int value = BitConverter.ToInt32(ResultArray, CurrentIndex);
            CurrentIndex += 4;
            return value;
        }

        public uint ReadUInt()
        {
            uint value = BitConverter.ToUInt32(ResultArray, CurrentIndex);
            CurrentIndex += 4;
            return value;
        }

        public long ReadLong()
        {
            long value = BitConverter.ToInt64(ResultArray, CurrentIndex);
            CurrentIndex += 8;
            return value;
        }

        public ulong ReadULong()
        {
            ulong value = BitConverter.ToUInt64(ResultArray, CurrentIndex);
            CurrentIndex += 8;
            return value;
        }

        public string ReadString(int index)
        {
            string value = Encoding.UTF8.GetString(ResultArray.Skip(CurrentIndex).Take(index).ToArray());
            CurrentIndex += index;
            return value;
        }
        public string ReadString()
        {
            var len = ReadInt();
            string value = Encoding.UTF8.GetString(ResultArray.Skip(CurrentIndex).Take(len).ToArray());
            CurrentIndex += len;
            return value;
        }

        public Vector3 ReadVector3()
        {
            return new Vector3()
            {
                X = ReadFloat(),
                Y = ReadFloat(),
                Z = ReadFloat()
            };
        }
        public Vector2 ReadVector2()
        {
            return new Vector2()
            {
                X = ReadFloat(),
                Y = ReadFloat()
            };
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion()
            {
                X = ReadFloat(),
                Y = ReadFloat(),
                Z = ReadFloat(),
                W = ReadFloat()
            };
        }
    }
}
