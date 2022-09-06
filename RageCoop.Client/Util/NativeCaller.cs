using GTA.Math;
using GTA.Native;
using System;
using System.Runtime.InteropServices;

namespace RageCoop.Client
{
    [StructLayout(LayoutKind.Explicit, Size = 80)]
    public struct HeadBlendData
    {
        [FieldOffset(0)]
        public int ShapeFirst;

        [FieldOffset(8)]
        public int ShapeSecond;

        [FieldOffset(16)]
        public int ShapeThird;

        [FieldOffset(24)]
        public int SkinFirst;

        [FieldOffset(32)]
        public int SkinSecond;

        [FieldOffset(40)]
        public int SkinThird;

        [FieldOffset(48)]
        public float ShapeMix;

        [FieldOffset(56)]
        public float SkinMix;

        [FieldOffset(64)]
        public float ThirdMix;
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    public struct NativeVector3
    {
        [FieldOffset(0)]
        public float X;

        [FieldOffset(8)]
        public float Y;

        [FieldOffset(16)]
        public float Z;

        public static implicit operator Vector3(NativeVector3 vec)
        {
            return new Vector3() { X = vec.X, Y = vec.Y, Z = vec.Z };
        }
        public static implicit operator NativeVector3(Vector3 vec)
        {
            return new NativeVector3() { X = vec.X, Y = vec.Y, Z = vec.Z };
        }
    }
    public static class NativeCaller
    {
        // These are borrowed from ScriptHookVDotNet's 
        [DllImport("ScriptHookV.dll", ExactSpelling = true, EntryPoint = "?nativeInit@@YAX_K@Z")]
        private static extern void NativeInit(ulong hash);

        [DllImport("ScriptHookV.dll", ExactSpelling = true, EntryPoint = "?nativePush64@@YAX_K@Z")]
        private static extern void NativePush64(ulong val);

        [DllImport("ScriptHookV.dll", ExactSpelling = true, EntryPoint = "?nativeCall@@YAPEA_KXZ")]
        private static extern unsafe ulong* NativeCall();

        // These are from ScriptHookV's nativeCaller.h
        private static unsafe void NativePush<T>(T val) where T : unmanaged
        {
            ulong val64 = 0;
            *(T*)(&val64) = val;
            NativePush64(val64);
        }

        public static unsafe R Invoke<R>(ulong hash) where R : unmanaged
        {
            NativeInit(hash);
            return *(R*)(NativeCall());
        }
        public static unsafe R Invoke<R>(Hash hash, params object[] args)
            where R : unmanaged
        {
            NativeInit((ulong)hash);
            var arguments = ConvertPrimitiveArguments(args);
            foreach (var arg in arguments)
                NativePush(arg);

            return *(R*)(NativeCall());
        }


        /// <summary>
        /// Helper function that converts an array of primitive values to a native stack.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static unsafe ulong[] ConvertPrimitiveArguments(object[] args)
        {
            var result = new ulong[args.Length];
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] is bool valueBool)
                {
                    result[i] = valueBool ? 1ul : 0ul;
                    continue;
                }
                if (args[i] is byte valueByte)
                {
                    result[i] = valueByte;
                    continue;
                }
                if (args[i] is int valueInt32)
                {
                    result[i] = (ulong)valueInt32;
                    continue;
                }
                if (args[i] is ulong valueUInt64)
                {
                    result[i] = valueUInt64;
                    continue;
                }
                if (args[i] is float valueFloat)
                {
                    result[i] = *(ulong*)&valueFloat;
                    continue;
                }
                if (args[i] is IntPtr valueIntPtr)
                {
                    result[i] = (ulong)valueIntPtr.ToInt64();
                    continue;
                }

                throw new ArgumentException("Unknown primitive type in native argument list", nameof(args));
            }

            return result;
        }
    }

}
