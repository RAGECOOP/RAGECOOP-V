using System;
using System.Text;
using System.Linq;

namespace CoopClient
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
    }
}
