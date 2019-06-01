using DeploymentToolkit.RegistryWrapper;
using Microsoft.Win32;
using NLog;
using System;

namespace DeploymentToolkit.Actions
{
    public static class RegistryActions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public enum Architecture : byte
        {
            Win32,
            Win64
        }

        private static WinRegistryBase GetRegistry(Architecture architecture)
        {
            if (architecture == Architecture.Win32)
            {
                return new Win32Registry();
            }
            else if (architecture == Architecture.Win64)
            {
                return new Win64Registry();
            }
            throw new Exception("Invalid architecture");
        }

        public static bool KeyExists(Architecture architecture, string path, string keyName)
        {
            _logger.Trace($"KeyExists({architecture}, {path}, {keyName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(keyName))
                throw new ArgumentNullException(nameof(keyName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.SubKeyExists(path, keyName);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to check for existence of {path}");
                return false;
            }
        }

        public static bool CreateKey(Architecture architecture, string path, string keyName)
        {
            _logger.Trace($"CreateKey({architecture}, {path}, {keyName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(keyName))
                throw new ArgumentNullException(nameof(keyName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.CreateSubKey(path, keyName);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to create key {keyName} in {path}");
                return false;
            }
        }

        public static bool DeleteKey(Architecture architecture, string path, string keyName)
        {
            _logger.Trace($"DeleteKey({architecture}, {path}, {keyName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(keyName))
                throw new ArgumentNullException(nameof(keyName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.DeleteSubKey(path, keyName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to delete key {keyName} in {path}");
                return false;
            }
        }

        public static bool ValueExists(Architecture architecture, string path, string valueName)
        {
            _logger.Trace($"ValueExists({architecture}, {path}, {valueName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(valueName))
                throw new ArgumentNullException(nameof(valueName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.HasValue(path, valueName);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to get {valueName} in {path}");
                return false;
            }
        }

        public static string GetValue(Architecture architecture, string path, string valueName)
        {
            _logger.Trace($"GetValue({architecture}, {path}, {valueName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(valueName))
                throw new ArgumentNullException(nameof(valueName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.GetValue(path, valueName)?.ToString() ?? string.Empty;
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to get value of {valueName} in {path}");
                return null;
            }
        }

        public static bool SetValue(Architecture architecture, string path, string valueName, string value, RegistryValueKind valueType)
        {
            _logger.Trace($"SetValue({architecture}, {path}, {valueName}, {value}, {valueType})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(valueName))
                throw new ArgumentNullException(nameof(valueName));
        
            try
            {
                var registry = GetRegistry(architecture);
                return registry.SetValue(path, valueName, value, valueType);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to set value of {valueName}");
                return false;
            }
        }

        public static bool DeleteValue(Architecture architecture, string path, string valueName)
        {
            _logger.Trace($"DeleteValue({architecture}, {path}, {valueName})");
            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(valueName))
                throw new ArgumentNullException(nameof(valueName));

            try
            {
                var registry = GetRegistry(architecture);
                return registry.DeleteValue(path, valueName);
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to delete {valueName} in {path}");
                return false;
            }
        }
    }
}
