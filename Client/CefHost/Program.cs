using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Security.Permissions;
using System.Threading.Tasks;
using CefSharp;
using CefSharp.OffScreen;

namespace RageCoop.Client.CefHost
{
    internal static class Program
    {
        [SecurityPermission(SecurityAction.Demand)]
        private static void Main(string[] args)
        {
            Cef.Initialize(new CefSettings
            {
                BackgroundColor = 0x00
            });

            var name = Guid.NewGuid().ToString();

            var channel = new IpcChannel(name);
            ChannelServices.RegisterChannel(channel, false);
            RemotingConfiguration.RegisterActivatedServiceType(typeof(CefController));


            // Write to stdout so it can be read by the client
            Console.WriteLine(name);

            var adapterUrl = Console.ReadLine();
            var adapterEntry = new ActivatedClientTypeEntry(typeof(CefAdapter), adapterUrl);
            Console.WriteLine("Registered adapter entry: " + adapterUrl);
            RemotingConfiguration.RegisterActivatedClientType(adapterEntry);

            var channelData = (ChannelDataStore)channel.ChannelData;
            foreach (var uri in channelData.ChannelUris) Console.WriteLine("Channel URI: {0}", uri);

            Task.Run(() =>
            {
                try
                {
                    Util.GetParentProcess().WaitForExit();
                    Console.WriteLine("Parent process terminated, exiting...");
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });

            while (true)
                switch (Console.ReadLine())
                {
                    case "exit":
                        Cef.Shutdown();
                        Environment.Exit(0);
                        break;
                }
        }
    }

    /// <summary>
    ///     A utility class to determine a process parent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Util
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass,
            ref Util processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        ///     Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        ///     Gets the parent process of specified process.
        /// </summary>
        /// <param name="id">The process id.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(int id)
        {
            var process = Process.GetProcessById(id);
            return GetParentProcess(process.Handle);
        }

        /// <summary>
        ///     Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            var pbi = new Util();
            int returnLength;
            var status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}