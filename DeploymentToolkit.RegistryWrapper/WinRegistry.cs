using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace DeploymentToolkit.RegistryWrapper
{
    public static class Win96Registry
    {
        private static Win32Registry _win32Registry = new Win32Registry();
        private static Win64Registry _win64Registry = new Win64Registry();

        private static List<RegistryKey> GetBaseKey(RegistryHive hive)
        {
            switch(hive)
            {
                case RegistryHive.ClassesRoot:
                    return new List<RegistryKey>() {
                        _win32Registry.HKEY_CLASSES_ROOT,
                        _win64Registry.HKEY_CLASSES_ROOT
                    };
                case RegistryHive.CurrentConfig:
                    return new List<RegistryKey>() {
                        _win32Registry.HKEY_CURRENT_CONFIG,
                        _win64Registry.HKEY_CURRENT_CONFIG
                    };
                case RegistryHive.CurrentUser:
                    return new List<RegistryKey>() {
                        _win32Registry.HKEY_CURRENT_USER,
                        _win64Registry.HKEY_CURRENT_USER
                    };
                case RegistryHive.LocalMachine:
                    return new List<RegistryKey>() {
                        _win32Registry.HKEY_LOCAL_MACHINE,
                        _win64Registry.HKEY_LOCAL_MACHINE
                    };
                case RegistryHive.Users:
                    return new List<RegistryKey>() {
                        _win32Registry.HKEY_USERS,
                        _win64Registry.HKEY_USERS
                    };
                default:
                    throw new ArgumentOutOfRangeException(nameof(hive), "Unsupported hive");
            }
        }

        public static void CreateSubKey(RegistryHive hive, string path, string subKeyName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(subKeyName))
                throw new ArgumentNullException(nameof(subKeyName));

            var hives = GetBaseKey(hive);
            foreach(var regHive in hives)
            {
                var subKey = regHive.OpenSubKey(subKeyName);
                if (subKey != null)
                {
                    subKey.CreateSubKey(subKeyName);
                }
            }
        }

        public static void DeleteSubKey(RegistryHive hive, string path, string subKeyName)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(subKeyName))
                throw new ArgumentNullException(nameof(subKeyName));

            var hives = GetBaseKey(hive);
            foreach(var reghive in hives)
            {
                var subKey = reghive.OpenSubKey(path);
                if(subKey != null)
                {
                    subKey.DeleteSubKeyTree(subKeyName);
                }
            }
        }

        public static void SetValue(RegistryHive hive, string path, string subKeyName, string name, string value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(subKeyName))
                throw new ArgumentNullException(nameof(subKeyName));

            SetValue(hive, Path.Combine(path, subKeyName), name, value);
        }

        public static void SetValue(RegistryHive hive, string path, string name, string value)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(value))
                throw new ArgumentNullException(nameof(value));

            var hives = GetBaseKey(hive);
            foreach(var regHive in hives)
            {
                var subKey = regHive.OpenSubKey(path);
                if(subKey != null)
                {
                    subKey.SetValue(name, value);
                }
            }
        }
    }
}
