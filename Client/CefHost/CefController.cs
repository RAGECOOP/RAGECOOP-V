using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.MemoryMappedFiles;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;
using Microsoft.Win32.SafeHandles;

namespace RageCoop.Client.CefHost
{
    public enum BufferMode
    {
        Full = 1,
        Dirty = 2
    }

    public enum MouseButton
    {
        Left,
        Middle,
        Right
    }

    /// <summary>
    ///     Hosted by CefHost for managing cef instances.
    /// </summary>
    public class CefController : MarshalByRefObject, IDisposable
    {
        private static Process _host;
        private static ActivatedClientTypeEntry _controllerEntry;
        private static IpcChannel _adapterChannel;
        public static Action<string> OnCefMessage;
        private object _browser;
        private MemoryMappedFile _mmf;
        private string _mmfName;
        private SafeMemoryMappedViewHandle _mmfView;
        private BufferMode _mode;
        public IntPtr PtrBuffer { get; private set; }

        public void Dispose()
        {
            (_browser as ChromiumWebBrowser)?.Dispose();

            _mmf?.Dispose();
            if (PtrBuffer != IntPtr.Zero) _mmfView?.ReleasePointer();
            _mmfView?.Dispose();

            PtrBuffer = IntPtr.Zero;
            _mmf = null;
            _mmfView = null;
        }


        public static void Initialize(string fileName = "RageCoop.Client.CefHost.exe")
        {
            _host = new Process();
            _host.StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            _host.EnableRaisingEvents = true;
            _host.Start();
            RegisterChannels(_host.StandardOutput.ReadLine());
            Task.Run(() =>
            {
                while (_host?.HasExited == false) OnCefMessage?.Invoke("[CEF]: " + _host.StandardOutput.ReadLine());
            });
            Task.Run(() =>
            {
                while (_host?.HasExited == false)
                    OnCefMessage?.Invoke("[CEF][ERROR]: " + _host.StandardError.ReadLine());
            });
        }

        public static void ShutDown()
        {
            if (_host == null) return;
            _host.StandardInput.WriteLine("exit");
            _host.WaitForExit(1000);
            _host.Kill();
            _host = null;
        }

        private static void RegisterChannels(string hostChannel)
        {
            var service = Guid.NewGuid().ToString();
            Console.WriteLine("Registering adapter channel: " + service);
            _adapterChannel = new IpcChannel(service);
            ChannelServices.RegisterChannel(_adapterChannel, false);

            _controllerEntry = new ActivatedClientTypeEntry(typeof(CefController), "ipc://" + hostChannel);
            RemotingConfiguration.RegisterActivatedClientType(_controllerEntry);
            Console.WriteLine("Registered controller entry: " + "ipc://" + hostChannel);


            RemotingConfiguration.RegisterActivatedServiceType(typeof(CefAdapter));
            Console.WriteLine("Registered service: " + nameof(CefAdapter));
            _host.StandardInput.WriteLine("ipc://" + service);
        }

        /// <summary>
        ///     Called inside client process
        /// </summary>
        /// <param name="id"></param>
        /// <param name="size"></param>
        /// <param name="adapter"></param>
        /// <param name="bufferMode"></param>
        /// <param name="bufferSize"></param>
        /// <returns></returns>
        public static CefController Create(int id, Size size, out CefAdapter adapter, BufferMode bufferMode,
            long bufferSize = 1024 * 1024 * 16)
        {
            if (RemotingConfiguration.IsRemotelyActivatedClientType(typeof(CefController)) == null)
                throw new RemotingException();
            var controller = new CefController();
            controller.Activate(id, size, bufferMode, bufferSize);
            adapter = CefAdapter.Adapters[id];
            controller.Ping();
            return controller;
        }

        public unsafe void Activate(int id, Size size, BufferMode mode = BufferMode.Dirty,
            long sharedMemorySize = 1024 * 1024 * 16)
        {
            _mode = mode;
            _mmfName = Guid.NewGuid().ToString();

            // Set up shared memory
            _mmf = MemoryMappedFile.CreateNew(_mmfName, sharedMemorySize);
            _mmfView = _mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
            byte* pBuf = null;
            try
            {
                _mmfView.AcquirePointer(ref pBuf);
                PtrBuffer = (IntPtr)pBuf;
            }
            catch
            {
                Dispose();
                throw;
            }


            var adapter = new CefAdapter();
            adapter.Register(id, mode, _mmfName);

            _browser = new ChromiumWebBrowser();
            ((ChromiumWebBrowser)_browser).RenderHandler = new CefProcessor(size, adapter, PtrBuffer, mode);
            Console.WriteLine("CefController created: " + size);
        }


        public void LoadUrl(string url)
        {
            ((ChromiumWebBrowser)_browser).LoadUrl(url);
        }

        public void SendMouseClick(int x, int y, int modifiers, MouseButton button, bool mouseUp, int clicks)
        {
            var e = new MouseEvent(x, y, (CefEventFlags)modifiers);
            ((ChromiumWebBrowser)_browser).GetBrowserHost()
                ?.SendMouseClickEvent(e, (MouseButtonType)button, mouseUp, clicks);
        }

        public void SendMouseMove()
        {
            // _browser.GetBrowserHost() ?.SendMouseMoveEvent(,);
        }

        public DateTime Ping()
        {
            return DateTime.UtcNow;
            ;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }
    }

    /// <summary>
    ///     Hosted by client for receiving rendering data
    /// </summary>
    public class CefAdapter : MarshalByRefObject, IDisposable
    {
        public delegate void PaintDelegate(int bufferSize, Rectangle dirtyRect);

        public delegate void ResizeDelegate(Size newSize);

        public static Dictionary<int, CefAdapter> Adapters = new Dictionary<int, CefAdapter>();

        private MemoryMappedFile _mmf;
        private SafeMemoryMappedViewHandle _mmfView;
        public int Id;
        public Size Size;

        public CefAdapter()
        {
            Console.WriteLine("Adapter created");
        }

        public IntPtr PtrBuffer { get; private set; }

        /// <summary>
        ///     Maximum buffer size for a paint event, use this property to allocate memory.
        /// </summary>
        /// <remarks>Value is equal to <see cref="Size" />*4, therefore will change upon resize</remarks>
        public int MaxBufferSize => Size.Height * Size.Width * 4;

        public BufferMode BufferMode { get; private set; }

        public void Dispose()
        {
            _mmf?.Dispose();
            if (PtrBuffer != IntPtr.Zero) _mmfView?.ReleasePointer();
            _mmfView?.Dispose();

            PtrBuffer = IntPtr.Zero;
            _mmf = null;
            _mmfView = null;

            lock (Adapters)
            {
                if (Adapters.ContainsKey(Id)) Adapters.Remove(Id);
            }
        }

        public event PaintDelegate OnPaint;
        public event ResizeDelegate OnResize;

        public void Resized(Size newSize)
        {
            Size = newSize;
            OnResize?.Invoke(newSize);
        }

        public void Paint(Rectangle dirtyRect)
        {
            var size = BufferMode == BufferMode.Dirty
                ? dirtyRect.Width * dirtyRect.Height * 4
                : Size.Width * Size.Height * 4;
            OnPaint?.Invoke(size, dirtyRect);
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        public unsafe void Register(int id, BufferMode mode, string mmfName)
        {
            lock (Adapters)
            {
                if (Adapters.ContainsKey(id)) throw new ArgumentException("Specified id is already used", nameof(id));

                // Set up shared memory
                _mmf = MemoryMappedFile.OpenExisting(mmfName);
                _mmfView = _mmf.CreateViewAccessor().SafeMemoryMappedViewHandle;
                byte* pBuf = null;
                try
                {
                    _mmfView.AcquirePointer(ref pBuf);
                    PtrBuffer = (IntPtr)pBuf;
                }
                catch
                {
                    Dispose();
                    throw;
                }

                Id = id;
                BufferMode = mode;
                Adapters.Add(id, this);
            }
        }

        /// <summary>
        ///     Ensure ipc connection
        /// </summary>
        /// <returns></returns>
        public DateTime Ping()
        {
            return DateTime.UtcNow;
        }
    }
}