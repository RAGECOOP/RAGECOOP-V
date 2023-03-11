global using static RageCoop.Client.PInvoke;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace RageCoop.Client
{
    internal partial class PInvoke
    {
        public static void ClearLastError()
        {
            SetLastErrorEx(0, 0);
        }

        /// <summary>
        /// Check and throw if an error occurred during last winapi call in current thread
        /// </summary>
        public static void ErrorCheck32()
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0)
            {
                ClearLastError();
                throw new Win32Exception(error);
            }
        }

        [LibraryImport("user32.dll", SetLastError = true)]
        internal static partial void SetLastErrorEx(uint dwErrCode, uint dwType);

        [LibraryImport("Kernel32.dll", SetLastError = true)]
        public static unsafe partial uint GetFinalPathNameByHandleW(IntPtr hFile, char* lpszFilePath, uint cchFilePath, uint dwFlags);
    }
}