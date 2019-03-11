using Microsoft.Win32;
using NLog;
using System;
using System.Linq;

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

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        //private RegistryHive GetHiveFromKey(string path)
        //{
        //    switch(path.ToUpper())
        //    {
        //        case "HKCR":
        //        case "HKEY_CLASSES_ROOT":
        //            return RegistryHive.ClassesRoot;

        //        case "HKCC":
        //        case "HKEY_CURRENT_CONFIG":
        //            return RegistryHive.CurrentConfig;

        //        case "HKCU":
        //        case "HKEY_CURRENT_USER":
        //            return RegistryHive.CurrentUser;

        //        case "HKLM":
        //        case "HKEY_LOCAL_MACHINE":
        //            return RegistryHive.LocalMachine;

        //        case "HKUS":
        //        case "HKEY_USERS":
        //            return RegistryHive.Users;

        //        default:
        //            throw new ArgumentOutOfRangeException(nameof(path), "Unknown or unsupported hive");
        //    }
        //}

        private RegistryKey GetBaseKey(string path, out string newPath)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            var split = path.Split('\\');
            if (split.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(path), "Invalid path supplied");

            var hive = split[0];
            newPath = split.Length == 1 ? split[1] : split.Skip(1).Aggregate((i, j) => i + @"\" + j);

            switch(hive)
            {
                case "HKCR":
                case "HKEY_CLASSES_ROOT":
                    return HKEY_CLASSES_ROOT;

                case "HKCC":
                case "HKEY_CURRENT_CONFIG":
                    return HKEY_CURRENT_CONFIG;

                case "HKCU":
                case "HKEY_CURRENT_USER":
                    return HKEY_CURRENT_USER;

                case "HKLM":
                case "HKEY_LOCAL_MACHINE":
                    return HKEY_LOCAL_MACHINE;

                case "HKUS":
                case "HKEY_USERS":
                    return HKEY_USERS;

                default:
                    throw new ArgumentOutOfRangeException(nameof(path), "Invalid or not supported hive");
            }
        }

        public bool SubKeyExists(string path, string subKeyName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(subKeyName))
                throw new ArgumentNullException(nameof(subKeyName));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if(subKey != null)
            {
                return subKey.GetSubKeyNames().Contains(subKeyName);
            }

            _logger.Warn($"Failed to open {path}: Key not found");
            return false;
        }

        public bool CreateSubKey(string path, string subKeyName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(nameof(subKeyName)))
                throw new ArgumentNullException(nameof(subKeyName));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if(subKey != null)
            {
                subKey.CreateSubKey(subKeyName);
                return true;
            }
            _logger.Warn($"Failed to open {newPath}: Key not found");
            return false;
        }

        public bool DeleteSubKey(string path, string subKeyName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(nameof(subKeyName)))
                throw new ArgumentNullException(nameof(subKeyName));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if (subKey != null)
            {
                subKey.DeleteSubKeyTree(subKeyName);
                return true;
            }
            _logger.Warn($"Failed to delete {subKeyName} from {newPath}: Key not found");
            return false;
        }

        public bool HasValue(string path, string value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if (subKey != null)
            {
                return subKey.GetValue(value) != null;
            }
            _logger.Warn($"Failed to get {value} from {newPath}: Key not found");
            return false;
        }

        public bool SetValue(string path, string name, string value, RegistryValueKind valueKind)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if (subKey != null)
            {
                subKey.SetValue(name, value, valueKind);
                return true;
            }
            _logger.Warn($"Failed to set {value} from {newPath}: Key not found");
            return false;
        }

        public string GetValue(string path, string value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if (subKey != null)
            {
                return subKey.GetValue(value).ToString();
            }
            _logger.Warn($"Failed to get {value} from {newPath}: Key not found");
            return null;
        }

        public bool DeleteValue(string path, string value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var key = GetBaseKey(path, out var newPath);
            var subKey = key.OpenSubKey(newPath);
            if (subKey != null)
            {
                subKey.DeleteValue(value);
                return true;
            }
            _logger.Warn($"Failed to delete {value} from {newPath}: Key not found");
            return false;
        }
    }
}
