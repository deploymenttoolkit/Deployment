using System.Runtime.InteropServices;

namespace DeploymentToolkit.Util.Structs
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct _PRIVILEGE_SET
    {
        private readonly int PrivilegeCount;
        private readonly int Control;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // ANYSIZE_ARRAY = 1
        private readonly LUID_AND_ATTRIBUTES[] Privileges;
    }
}
