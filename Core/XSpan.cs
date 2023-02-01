using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RageCoop.Core
{
    /// <summary>
    /// A light-weight and less restricted implementation of <see cref="Span{T}"/>, gonna be used at some point, maybe?
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly unsafe struct XSpan<T> where T : unmanaged
    {
        public XSpan(void* address, int len)
        {
            Address = (T*)address;
            Length = len;
        }

        public T this[int i]
        {
            get { return Address[i]; }
            set { Address[i] = value; }
        }

        public readonly T* Address;
        public readonly int Length;

        public void CopyTo(XSpan<T> dest, int destStart = 0)
        {
            for (int i = 0; i < Length; i++)
            {
                dest[destStart + i] = this[i];
            }
        }

        public XSpan<byte> Slice(int start) => new(Address + start, Length - start);
        public XSpan<byte> Slice(int start, int len) => new(Address + start, len);

        public static implicit operator Span<T>(XSpan<T> s)
        {
            return new Span<T>(s.Address, s.Length);
        }

        public static implicit operator XSpan<T>(Span<T> s)
        {
            fixed (T* ptr = s)
            {
                return new XSpan<T>(ptr, s.Length);
            }
        }
    }
}
