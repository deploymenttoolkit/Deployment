using System;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Util.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct SECURITY_ATTRIBUTES
    {
        public int Length;
        public IntPtr lpSecurityDescriptor;
        public bool bInheritHandle;
    }
}
