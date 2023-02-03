using System;
using System.Runtime.InteropServices;
using System.Text;
using GTA.Math;

namespace RageCoop.Core
{
    public unsafe abstract class Buffer
    {
        /// <summary>
        /// Size of this buffer in memory
        /// </summary>
        public int Size { get; protected set; }

        /// <summary>
        /// The current read/write index
        /// </summary>
        public int Position { get; protected set; }

        /// <summary>
        /// Pointer to the start of this buffer
        /// </summary>
        public byte* Address { get; protected set; }

        /// <summary>
        /// Ensure memory safety and advance position by specified number of bytes
        /// </summary>
        /// <param name="cbSize"></param>
        /// <returns>Pointer to the current position in the buffer</returns>
        protected abstract byte* Alloc(int cbSize);

        protected T* Alloc<T>(int count = 1) where T : unmanaged
            => (T*)Alloc(count * sizeof(T));

        /// <summary>
        /// Reset position to the start of this buffer
        /// </summary>
        public void Reset()
        {
            Position = 0;
        }
    }

    public unsafe sealed class BufferWriter : Buffer
    {
        public BufferWriter(int size)
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
                System.Buffer.MemoryCopy(Address, newAddr, size, Size);
                Marshal.FreeHGlobal((IntPtr)Address);
            }
            Size = size;
            Address = newAddr;
        }

        protected override byte* Alloc(int cbSize)
        {
            var index = Position;
            Position += cbSize;

            // Resize the buffer by at least 50% if there's no sufficient space
            if (Position > Size)
                Resize(Math.Max(Position + 1, (int)(Size * 1.5f)));

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

        public void Write<T>(ReadOnlySpan<T> source) where T : unmanaged
        {
            var len = source.Length;
            fixed (T* pSource = source)
            {
                System.Buffer.MemoryCopy(pSource, Alloc(sizeof(T) * len), len, len);
            }
        }

        public void Write<T>(Span<T> source) where T : unmanaged => Write((ReadOnlySpan<T>)source);

        /// <summary>
        /// Write an array, prefix the data with its length so it can latter be read using <see cref="BufferReader.ReadArray{T}"/>
        /// </summary>
        public void WriteArray<T>(T[] values) where T : unmanaged
        {
            var len = values.Length;
            WriteVal(len);
            fixed (T* pFrom = values)
            {
                System.Buffer.MemoryCopy(pFrom, Alloc(sizeof(T) * len), len, len);
            }
        }

        /// <summary>
        /// Allocate a byte array on managed heap and copy the data of specified size to it 
        /// </summary>
        /// <param name="cbSize"></param>
        /// <returns>The newly created managed byte array</returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public byte[] ToByteArray(int cbSize)
        {
            if (cbSize > Size)
                throw new ArgumentOutOfRangeException(nameof(cbSize));

            var result = new byte[cbSize];
            fixed (byte* pResult = result)
            {
                System.Buffer.MemoryCopy(Address, pResult, cbSize, cbSize);
            }
            return result;
        }

        /// <summary>
        /// Free the associated memory allocated on the unmanaged heap
        /// </summary>
        public void Free() => Marshal.FreeHGlobal((IntPtr)Address);
    }

    public unsafe sealed class BufferReader : Buffer
    {
        /// <summary>
        /// Initialize an empty instance, needs to call <see cref="Initialise(byte*, int)"/> before reading data
        /// </summary>
        public BufferReader()
        {

        }
        public BufferReader(byte* address, int size) => Initialise(address, size);

        public void Initialise(byte* address, int size)
        {
            Address = address;
            Size = size;
            Reset();
        }

        protected override byte* Alloc(int cbSize)
        {
            if (Address == null)
                throw new NullReferenceException("Address is null");

            var index = Position;
            Position += cbSize;

            if (Position > Size)
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

        /// <summary>
        /// Read a span of type <typeparamref name="T"/> from current position to <paramref name="destination"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="destination"></param>
        public void Read<T>(Span<T> destination) where T : unmanaged
        {
            var len = destination.Length;
            fixed (T* pTo = destination)
            {
                System.Buffer.MemoryCopy(Alloc(len * sizeof(T)), pTo, len, len);
            }
        }

        /// <summary>
        /// Reads an array previously written using <see cref="BufferWriter.WriteArray{T}(T[])"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] ReadArray<T>() where T : unmanaged
        {
            var len = ReadVal<int>();
            var from = Alloc<T>(len);
            var result = new T[len];
            fixed (T* pTo = result)
            {
                System.Buffer.MemoryCopy(from, pTo, len, len);
            }
            return result;
        }
    }
}
