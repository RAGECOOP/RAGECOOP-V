using RageCoop.Core;
using RageCoop.Core.Scripting;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: InternalsVisibleTo("RageCoop.Client")] // For debugging

namespace RageCoop.Client.Scripting
{
    public static unsafe partial class APIBridge
    {
        static readonly ThreadLocal<char[]> _resultBuf = new(() => new char[4096]);
        static readonly List<CustomEventHandler> _handlers = new();

        static APIBridge()
        {
            if (SHVDN.Core.GetPtr == null)
                throw new InvalidOperationException("Game not running");

            foreach(var fd in typeof(APIBridge).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                var importAttri = fd.GetCustomAttribute<ApiImportAttribute>();
                if (importAttri == null)
                    continue;
                importAttri.EntryPoint ??= fd.Name;
                var key = $"RageCoop.Client.Scripting.API.{importAttri.EntryPoint}";
                var fptr = SHVDN.Core.GetPtr(key);
                if (fptr == default)
                    throw new KeyNotFoundException($"Failed to find function pointer: {key}");
                
                fd.SetValue(null,fptr);
            }
        }

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
                fixed(char* pName = name)
                {
                    var resultLen = InvokeCommandAsJsonUnsafe(pName, argc, argv);
                    if (resultLen == 0)
                        throw new Exception(GetLastResult());
                    return GetLastResult();
                }
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
                if (GetLastResultUnsafe(pBuf, cbBufSize) > 0)
                {
                    return new string(pBuf);
                }
                return null;
            }
        }

        public static void SendCustomEvent(CustomEventHash hash, params object[] args)
            => SendCustomEvent(CustomEventFlags.None, hash, args);
        public static void SendCustomEvent(CustomEventFlags flags, CustomEventHash hash, params object[] args)
        {
            var writer = GetWriter();
            CustomEvents.WriteObjects(writer, args);
            SendCustomEventUnsafe(flags, hash, writer.Address, writer.Position);
        }

        public static void RegisterCustomEventHandler(CustomEventHash hash, Action<CustomEventReceivedArgs> handler)
            => RegisterCustomEventHandler(hash, (CustomEventHandler)handler);

        internal static string GetPropertyAsJson(string name) => InvokeCommandAsJson("GetProperty", name);
        internal static string GetConfigAsJson(string name) => InvokeCommandAsJson("GetConfig", name);

        internal static T GetProperty<T>(string name) => JsonDeserialize<T>(GetPropertyAsJson(name));
        internal static T GetConfig<T>(string name) => JsonDeserialize<T>(GetConfigAsJson(name));

        internal static void SetProperty(string name, object val) => InvokeCommand("SetProperty", name, val);
        internal static void SetConfig(string name, object val) => InvokeCommand("SetConfig", name, val);


        [ApiImport]
        public static delegate* unmanaged<char*, CustomEventHash> GetEventHash;

        [ApiImport]
        private static delegate* unmanaged<char*,void> SetLastResult;

        [ApiImport(EntryPoint = "GetLastResult")]
        private static delegate* unmanaged<char*, int, int> GetLastResultUnsafe;

        [ApiImport(EntryPoint = "InvokeCommand")]
        private static delegate* unmanaged<char*, int, char**, int> InvokeCommandAsJsonUnsafe;

        [ApiImport(EntryPoint = "SendCustomEvent")]
        private static delegate* unmanaged<CustomEventFlags, int, byte*, int, void> SendCustomEventUnsafe;

        [ApiImport]
        private static delegate* unmanaged<int> GetLastResultLenInChars;

        [ApiImport]
        public static delegate* unmanaged<LogLevel, char*, void> LogEnqueue;
    }

    [AttributeUsage(AttributeTargets.Field)]
    class ApiImportAttribute : Attribute
    {
        public string EntryPoint;
    }
}