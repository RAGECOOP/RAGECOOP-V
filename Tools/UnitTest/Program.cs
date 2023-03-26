using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using GTA.Math;
using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
    struct TestStruct
    {
        public ulong val1;
        public ulong val2;
    }
    public unsafe partial class Program
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
            fixed (byte* p = payload)
            {
                reader.Initialise(p, payload.Length);
            }
            var result = CustomEvents.ReadObjects(reader);
            if (!result.SequenceEqual(objs))
                throw new Exception("CustomEvents fail");

            Console.WriteLine("CustomEvents OK");

            var sArr1 = new TestStruct[200];
            var sArr2 = new TestStruct[200];
            var sArr3 = new TestStruct[200];
            for (int i = 0; i < 200; i++)
            {
                sArr1[i] = sArr2[i] = new() { val1 = 123, val2 = 456 };
                sArr3[i] = new() { val1 = 456, val2 = 789 };
            }
            fixed (TestStruct* p1 = sArr1, p2 = sArr2, p3 = sArr3)
            {
                Debug.Assert(CoreUtils.MemCmp(p1, p2, sizeof(TestStruct)));
                Debug.Assert(!CoreUtils.MemCmp(p1, p3, sizeof(TestStruct)));
                Debug.Assert(!CoreUtils.MemCmp(p2, p3, sizeof(TestStruct)));
            }
#if !DEBUG
            var summary = BenchmarkRunner.Run<MemCmpTest>();
#endif

        }

        public class MemCmpTest
        {
            private const int N = 10000;
            TestStruct* p1;
            TestStruct* p2;
            TestStruct* p3;
            int size = sizeof(TestStruct) * N;
            public MemCmpTest()
            {
                p1 = (TestStruct*)Marshal.AllocHGlobal(N * sizeof(TestStruct));
                p2 = (TestStruct*)Marshal.AllocHGlobal(N * sizeof(TestStruct));
                p3 = (TestStruct*)Marshal.AllocHGlobal(N * sizeof(TestStruct));
                for (int i = 0; i < 200; i++)
                {
                    p1[i] = p2[i] = new() { val1 = 123, val2 = 456 };
                    p3[i] = new() { val1 = 456, val2 = 789 };
                }
            }

            [Benchmark]
            public void Simd()
            {
                CoreUtils.MemCmp(p1, p2, size);
            }

            [Benchmark]
            public void Win32()
            {
                memcmp(p1, p2, (UIntPtr)size);
            }
        }


        [DllImport("msvcrt.dll")]
        public static extern int memcmp(void* b1, void* b2, UIntPtr count);
    }
}