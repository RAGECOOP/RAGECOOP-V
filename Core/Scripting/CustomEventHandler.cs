using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace RageCoop.Core.Scripting
{
    public unsafe delegate void CustomEventHandlerDelegate(int hash, byte* data, int cbData);

    /// <summary>
    /// </summary>
    public class CustomEventReceivedArgs : EventArgs
    {
        /// <summary>
        ///     The event hash
        /// </summary>
        public int Hash { get; set; }

        /// <summary>
        ///     Supported types: byte, short, ushort, int, uint, long, ulong, float, bool, string, Vector3, Quaternion
        /// </summary>
        public object[] Args { get; set; }

        internal object Tag { get; set; }
    }
    public unsafe class CustomEventHandler
    {
        // Make sure the handler doesn't get GC'd
        static List<CustomEventHandler> _handlers = new();

        [ThreadStatic]
        static object _tag;
        public CustomEventHandler()
        {
            lock (_handlers)
            {
                _handlers.Add(this);
            }
        }
        public CustomEventHandler(IntPtr func) : this()
        {
            FunctionPtr = (ulong)func;
            Directory = SHVDN.Core.CurrentDirectory;
        }

        [JsonIgnore]
        private CustomEventHandlerDelegate _managedHandler; // Used to keep GC reference

        [JsonProperty]
        public ulong FunctionPtr { get; private set; }

        [JsonProperty]
        public string Directory { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="data"></param>
        /// <param name="cbData"></param>
        /// <param name="tag">Only works when using <see cref="CustomEventReceivedArgs"/></param>
        public void Invoke(int hash, byte* data, int cbData, object tag = null)
        {
            _tag = tag;
            ((delegate* unmanaged<int, byte*, int, void>)FunctionPtr)(hash, data, cbData);
            _tag = null;
        }

        public static implicit operator CustomEventHandler(CustomEventHandlerDelegate handler)
        => new(Marshal.GetFunctionPointerForDelegate(handler)) { _managedHandler = handler };

        public static implicit operator CustomEventHandler(Action<CustomEventReceivedArgs> handler)
        {
            return new CustomEventHandlerDelegate((hash, data, cbData) =>
            {
                var reader = GetReader(data, cbData);
                var arg = new CustomEventReceivedArgs
                {
                    Hash = hash,
                    Args = CustomEvents.ReadObjects(reader),
                    Tag = _tag
                };
                handler(arg);
            });
        }
    }
}
