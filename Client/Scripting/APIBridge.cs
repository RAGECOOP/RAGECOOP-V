using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]
[assembly: InternalsVisibleTo("RageCoop.Client")] // For debugging

namespace RageCoop.Client.Scripting
{
    public static unsafe partial class APIBridge
    {
        static readonly ThreadLocal<char[]> _resultBuf = new(() => new char[4096]);

        /// <summary>
        /// Copy content of string to a sequential block of memory
        /// </summary>
        /// <param name="strs"></param>
        /// <returns>Pointer to the start of the block, can be used as argv</returns>
        /// <remarks>Call <see cref="Marshal.FreeHGlobal(nint)"/> with the returned pointer when finished using</remarks>
        internal static char** StringArrayToMemory(string[] strs)
        {
            var argc = strs.Length;
            var cbSize = sizeof(IntPtr) * argc + strs.Sum(s => (s.Length + 1) * sizeof(char));
            var result = (char**)Marshal.AllocHGlobal(cbSize);
            var pCurStr = (char*)(result + argc);
            for (int i = 0; i < argc; i++)
            {
                result[i] = pCurStr;
                var len = strs[i].Length;
                var cbStrSize = (len + 1) * sizeof(char); // null terminator
                fixed (char* pStr = strs[i])
                {
                    System.Buffer.MemoryCopy(pStr, pCurStr, cbStrSize, cbStrSize);
                }
                pCurStr += len + 1;
            }
            return result;
        }

        internal static void InvokeCommand(string name, params object[] args)
            => InvokeCommandAsJson(name, args);
        internal static T InvokeCommand<T>(string name, params object[] args)
            => JsonDeserialize<T>(InvokeCommandAsJson(name, args));

        /// <summary>
        /// Invoke command and get the return value as json
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns>The json representation of returned object</returns>
        /// <exception cref="Exception"></exception>
        internal static string InvokeCommandAsJson(string name, params object[] args)
        {
            var argc = args.Length;
            var argv = StringArrayToMemory(args.Select(JsonSerialize).ToArray());
            try
            {
                var resultLen = InvokeCommandAsJsonUnsafe(name, argc, argv);
                if (resultLen == 0)
                    throw new Exception(GetLastResult());
                return GetLastResult();
            }
            finally
            {
                Marshal.FreeHGlobal((IntPtr)argv);
            }
        }

        public static string GetLastResult()
        {
            var countCharsRequired = GetLastResultLenInChars() + 1;
            if (countCharsRequired > _resultBuf.Value.Length)
            {
                _resultBuf.Value = new char[countCharsRequired];
            }
            var cbBufSize = _resultBuf.Value.Length * sizeof(char);
            fixed (char* pBuf = _resultBuf.Value)
            {
                if (GetLastResult(pBuf, cbBufSize) > 0)
                {
                    return new string(pBuf);
                }
                return null;
            }
        }

        public static void SendCustomEvent(CustomEventFlags flags, CustomEventHash hash, params object[] args)
        {
            var writer = GetWriter();
            CustomEvents.WriteObjects(writer, args);
            SendCustomEvent(flags, hash, writer.Address, writer.Position);
        }

        internal static string GetPropertyAsJson(string name) => InvokeCommandAsJson("GetProperty", name);
        internal static string GetConfigAsJson(string name) => InvokeCommandAsJson("GetConfig", name);

        internal static T GetProperty<T>(string name) => JsonDeserialize<T>(GetPropertyAsJson(name));
        internal static T GetConfig<T>(string name) => JsonDeserialize<T>(GetConfigAsJson(name));

        internal static void SetProperty(string name, object val) => InvokeCommand("SetProperty", name, val);
        internal static void SetConfig(string name, object val) => InvokeCommand("SetConfig", name, val);


        [LibraryImport("RageCoop.Client.dll", StringMarshalling = StringMarshalling.Utf16)]
        public static partial CustomEventHash GetEventHash(string name);

        [LibraryImport("RageCoop.Client.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial void SetLastResult(string msg);

        [LibraryImport("RageCoop.Client.dll")]
        private static partial int GetLastResult(char* buf, int cbBufSize);

        [LibraryImport("RageCoop.Client.dll", EntryPoint = "InvokeCommand", StringMarshalling = StringMarshalling.Utf16)]
        private static partial int InvokeCommandAsJsonUnsafe(string name, int argc, char** argv);

        [LibraryImport("RageCoop.Client.dll")]
        private static partial void SendCustomEvent(CustomEventFlags flags, int hash, byte* data, int cbData);

        [LibraryImport("RageCoop.Client.dll")]
        private static partial int GetLastResultLenInChars();

        [LibraryImport("RageCoop.Client.dll")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool RegisterCustomEventHandler(CustomEventHash hash, IntPtr ptrHandler);

    }
}