using DeploymentToolkit.Registry.Modals;
using Microsoft.Win32;
using NLog;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Registry
{
    public abstract class WinRegistryBase
    {
        [DllImport("advapi32.dll")]
        static extern int RegOpenKeyEx(
            RegistryHive hKey,
            [MarshalAs(UnmanagedType.VBByRefStr)] ref string subKey,
            int options,
            RegAccess sam,
            out UIntPtr phkResult
        );

        [DllImport("advapi32.dll")]
        static extern int RegCloseKey(
            UIntPtr hKey
        );

        [DllImport("advapi32.dll")]
        static extern int RegCreateKeyEx(
            UIntPtr hKey,
            [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpSubKey,
            uint Reserved,
            string lpClass,
            RegOption dwOptions,
            RegSAM samDesired,
            ref SECURITY_ATTRIBUTES lpSecurityAttributes,
            out UIntPtr phkResult,
            out RegDisposition lpdwDisposition
        );

        protected abstract RegAccess RegAccess { get; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public WinRegistryKey OpenKey(string path, bool write = false)
        {
            _logger.Trace($"OpenKey({path}, {write})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var hive = GetHiveFromString(path, out var newPath);
            _logger.Trace($"Hive: {hive}");
            var error = RegOpenKeyEx(hive, ref newPath, 0, write ? RegAccess.KEY_ALL_ACCESS : RegAccess.KEY_READ | RegAccess, out var key);
            _logger.Trace($"Errorlevel: {error}");

            if (error == 0)
                return new WinRegistryKey(this, key, newPath);
            throw new Win32Exception(error);
        }

        public bool CloseKey(WinRegistryKey key)
        {
            _logger.Trace($"CloseKey({key.Key})");
            var error = RegCloseKey(key.RegPointer);
            _logger.Trace($"Errorlevel: {error}");

            if (error == 0)
                return true;
            throw new Win32Exception(error);
        }

        private RegistryHive GetHiveFromString(string path, out string newPath)
        {
            _logger.Trace($"GetHiveFromString({path})");
            var split = path.Split('\\');
            if (split.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(path));

            var hive = split[0].ToUpper();
            _logger.Trace($"Hive: {hive}");
            newPath = split.Length > 2 ? split.Skip(1).Aggregate((i, j) => i + "\\" + j) : split[1];
            _logger.Trace($"NewPath: {newPath}");
            switch(hive)
            {
                case "HKEY_LOCAL_MACHINE":
                    return RegistryHive.LocalMachine;
                case "HKEY_CURRENT_USER":
                    return RegistryHive.CurrentUser;
                case "HKEY_CURRENT_CONFIG":
                    return RegistryHive.CurrentConfig;
                case "HKEY_CLASSES_ROOT":
                    return RegistryHive.ClassesRoot;
                case "HKEY_USERS":
                    return RegistryHive.Users;

                default:
                    throw new ArgumentOutOfRangeException(nameof(path), "Invalid RegHive");
            }
        }
    }
}
