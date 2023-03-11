using GTA.Math;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Security.Cryptography;

namespace UnitTest
{
    class TestElement
    {
        static SHA256 sha = SHA256.Create();
        static int blah = new Random().Next();
        public TestElement(int i)
        {
            num = (i + blah) * 1024;
            vec2 = new(num * 10, num * 20);
            vec3 = new(num * 10, num * 20, num * 30);
            quat = new(num * 10, num * 20, num * 30, num * 40);
            str = sha.ComputeHash(BitConverter.GetBytes(num)).Dump();
        }
        public int num;
        public Vector2 vec2;
        public Vector3 vec3;
        public Quaternion quat;
        public string str;
    }
    internal unsafe class Program
    {
        static void Main(string[] args)
        {
            TestElement[] test = new TestElement[1024];
            Console.WriteLine("Testing buffers");
            var buf = new BufferWriter(1024);
            for (int i = 0; i < 1024; i++)
            {
                var e = test[i] = new TestElement(i);
                buf.WriteVal(e.num);
                buf.Write(ref e.vec2);
                buf.Write(ref e.vec3);
                buf.Write(ref e.quat);
                buf.Write(e.str);
            }
            Console.WriteLine($"Buffer size: {buf.Size}");
            Console.WriteLine($"Current position: {buf.Position}");

            Console.WriteLine("Validating data");
            var reader = new BufferReader(buf.Address, buf.Size);
            for (int i = 0; i < 1024; i++)
            {
                var e = test[i];
                reader.Read(out int num);
                reader.Read(out Vector2 vec2);
                reader.Read(out Vector3 vec3);
                reader.Read(out Quaternion quat);
                reader.Read(out string str);

                if (num != e.num)
                    throw new Exception("POCO fail");

                if (vec2 != e.vec2)
                    throw new Exception("vec2 fail");

                if (vec3 != e.vec3)
                    throw new Exception("vec3 fail");

                if (quat != e.quat)
                    throw new Exception("quat fail");

                if (str != e.str)
                    throw new Exception("str fail");
            }

            Console.WriteLine("Buffers OK");

            Console.WriteLine("Testing CustomEvents");
            var objs = new object[] { (byte)236, (short)82, (ushort)322, 
                "test", 123, 123U, 456UL, 345L, 5F, new Vector2(15, 54), new Vector3(22, 45, 25), new Quaternion(2, 3, 222, 5) };

            buf.Reset();
            CustomEvents.WriteObjects(buf, objs);
            var payload = buf.ToByteArray(buf.Position);
            fixed(byte* p = payload)
            {
                reader.Initialise(p, payload.Length);
            }

            if (!CustomEvents.ReadObjects(reader).SequenceEqual(objs))
                throw new Exception("CustomEvents fail");

            Console.WriteLine("CustomEvents OK");
        }
    }
}