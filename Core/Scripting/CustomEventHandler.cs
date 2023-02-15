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
        [ThreadStatic]
        static object _tag;
        public CustomEventHandler(IntPtr func)
        {
            FunctionPtr = func;
            if (Path.GetFileName(Environment.ProcessPath).ToLower() == "gtav.exe")
            {
                Module = SHVDN.Core.CurrentModule;
            }
        }

        private CustomEventHandlerDelegate _managedHandler; // Used to keep GC reference
        public IntPtr FunctionPtr { get; }
        public IntPtr Module { get; }

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
