using Microsoft.Win32;

namespace DeploymentToolkit.RegistryWrapper
{
    public abstract class WinRegistryBase
    {
        public abstract RegistryView View { get; }

        internal RegistryKey HKEY_LOCAL_MACHINE
        {
            get
            {
                return RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, View);
            }
        }

        internal RegistryKey HKEY_USERS
        {
            get
            {
                return RegistryKey.OpenBaseKey(RegistryHive.Users, View);
            }
        }

        internal RegistryKey HKEY_CLASSES_ROOT
        {
            get
            {
                return RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, View);
            }
        }

        internal RegistryKey HKEY_CURRENT_CONFIG
        {
            get
            {
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentConfig, View);
            }
        }

        internal RegistryKey HKEY_CURRENT_USER
        {
            get
            {
                return RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, View);
            }
        }
    }
}
