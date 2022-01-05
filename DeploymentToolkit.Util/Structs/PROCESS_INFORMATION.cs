using System;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Util.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessID;
        public int dwThreadID;
    }
}
