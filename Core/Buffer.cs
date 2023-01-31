using System;
using System.Runtime.InteropServices;
using System.Text;
using GTA.Math;

namespace RageCoop.Core
{
    internal unsafe abstract class BufferBase
    {
        public int Size { get; protected set; }
        public int CurrentIndex { get; protected set; }
        public byte* Address { get; protected set; }

        /// <summary>
        /// Ensure memory safety and advance position by specified number of bytes
        /// </summary>
        /// <param name="cbSize"></param>
        /// <returns>Pointer to the current position in the buffer</returns>
        protected abstract byte* Alloc(int cbSize);

        protected T* Alloc<T>(int count = 1) where T : unmanaged
            => (T*)Alloc(count * sizeof(T));
    }

    internal unsafe sealed class WriteBuffer : BufferBase
    {
        public WriteBuffer(int size)
        {
            Resize(size);
        }

        public void Resize(int size)
        {
            if (size < Size)
            {
                Size = size;
                return;
            }

            var newAddr = (byte*)Marshal.AllocHGlobal(size);
            if (Address != null)
            {
                Buffer.MemoryCopy(Address, newAddr, size, Size);
                Marshal.FreeHGlobal((IntPtr)Address);
            }
            Size = size;
            Address = newAddr;
        }

        protected override byte* Alloc(int cbSize)
        {
            var index = CurrentIndex;
            CurrentIndex += cbSize;

            // Resize the buffer by at least 50% if there's no sufficient space
            if (CurrentIndex > Size)
                Resize(Math.Max(CurrentIndex + 1, (int)(Size * 1.5f)));

            return Address + index;
        }

        public void Write<T>(ref T value) where T : unmanaged
        {
            var addr = Alloc<T>();
            *addr = value;
        }

        /// <summary>
        /// For passing struct smaller than word size (4/8 bytes on 32/64 bit system)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void WriteVal<T>(T value) where T : unmanaged
        {
            var addr = Alloc<T>();
            *addr = value;
        }


        public void Write(ReadOnlySpan<char> str)
        {
            // Prefixed by its size in bytes
            var cbBody = Encoding.UTF8.GetByteCount(str);
            WriteVal(cbBody);

            // Allocate and write string body
            var pBody = Alloc(cbBody);
            Encoding.UTF8.GetBytes(str, new(pBody, cbBody));
        }

        // Struct in GTA.Math have pack/padding for memory alignment, we don't want it to waste the bandwidth

        public void Write(ref Vector2 vec2)
        {
            var faddr = Alloc<float>(2);
            faddr[0] = vec2.X;
            faddr[1] = vec2.Y;
        }

        public void Write(ref Vector3 vec3)
        {
            var faddr = Alloc<float>(3);
            faddr[0] = vec3.X;
            faddr[1] = vec3.Y;
            faddr[2] = vec3.Z;
        }

        public void Write(ref Quaternion quat)
        {
            var faddr = Alloc<float>(4);
            faddr[0] = quat.X;
            faddr[1] = quat.Y;
            faddr[2] = quat.Z;
            faddr[3] = quat.W;
        }
    }

    internal unsafe sealed class ReadBuffer : BufferBase
    {
        public ReadBuffer(byte* address, int size)
        {
            Address = address;
            Size = size;
        }

        protected override byte* Alloc(int cbSize)
        {
            var index = CurrentIndex;
            CurrentIndex += cbSize;

            if (CurrentIndex > Size)
                throw new InvalidOperationException("Attempting to read beyond the existing buffer");

            return Address + index;
        }


        public T ReadVal<T>() where T : unmanaged => *Alloc<T>();

        public void Read<T>(out T result) where T : unmanaged
            => result = *Alloc<T>();

        public void Read(out string str)
        {
            var cbBody = ReadVal<int>();
            str = Encoding.UTF8.GetString(Alloc(cbBody), cbBody);
        }

        public void Read(out Vector2 vec)
        {
            var faddr = Alloc<float>(2);
            vec = new()
            {
                X = faddr[0],
                Y = faddr[1],
            };
        }

        public void Read(out Vector3 vec)
        {
            var faddr = Alloc<float>(3);
            vec = new()
            {
                X = faddr[0],
                Y = faddr[1],
                Z = faddr[2],
            };
        }

        public void Read(out Quaternion quat)
        {
            var faddr = Alloc<float>(4);
            quat = new()
            {
                X = faddr[0],
                Y = faddr[1],
                Z = faddr[2],
                W = faddr[3],
            };
        }
    }
}
