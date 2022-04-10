using System;
using System.Linq;
using System.Text;

namespace CoopServer
{
    internal class BitReader
    {
        private int _currentIndex { get; set; } = 0;

        private byte[] _resultArray = null;

        public BitReader(byte[] array)
        {
            _resultArray = array;
        }

        public bool CanRead(int bytes)
        {
            return _resultArray.Length >= _currentIndex + bytes;
        }

        public bool ReadBool()
        {
            bool value = BitConverter.ToBoolean(_resultArray, _currentIndex);
            _currentIndex += 1;
            return value;
        }

        public float ReadFloat()
        {
            float value = BitConverter.ToSingle(_resultArray, _currentIndex);
            _currentIndex += 4;
            return value;
        }

        public byte ReadByte()
        {
            byte value = _resultArray[_currentIndex];
            _currentIndex += 1;
            return value;
        }

        public byte[] ReadByteArray(int length)
        {
            byte[] value = _resultArray.Skip(_currentIndex).Take(length).ToArray();
            _currentIndex += length;
            return value;
        }

        public short ReadShort()
        {
            short value = BitConverter.ToInt16(_resultArray, _currentIndex);
            _currentIndex += 2;
            return value;
        }

        public ushort ReadUShort()
        {
            ushort value = BitConverter.ToUInt16(_resultArray, _currentIndex);
            _currentIndex += 2;
            return value;
        }

        public int ReadInt()
        {
            int value = BitConverter.ToInt32(_resultArray, _currentIndex);
            _currentIndex += 4;
            return value;
        }

        public uint ReadUInt()
        {
            uint value = BitConverter.ToUInt32(_resultArray, _currentIndex);
            _currentIndex += 4;
            return value;
        }

        public long ReadLong()
        {
            long value = BitConverter.ToInt64(_resultArray, _currentIndex);
            _currentIndex += 8;
            return value;
        }

        public ulong ReadULong()
        {
            ulong value = BitConverter.ToUInt64(_resultArray, _currentIndex);
            _currentIndex += 8;
            return value;
        }

        public string ReadString(int index)
        {
            string value = Encoding.UTF8.GetString(_resultArray.Skip(_currentIndex).Take(index).ToArray());
            _currentIndex += index;
            return value;
        }
    }
}
