using DeploymentToolkit.RegistryWrapper;
using DeploymentToolkit.Scripting.Extensions;
using NLog;
using System;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class RegistryFunctions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private enum Architecture : byte
        {
            Win32,
            Win64
        }

        private static Architecture GetArchitecture(string[] parameters, int expectedPositon)
        {
            var registryNamespace = parameters.Length > expectedPositon ? parameters[expectedPositon] : "32";
            if (!int.TryParse(registryNamespace, out var architecture))
            {
                _logger.Warn($"Failed to convert {registryNamespace} into a valid architecture");
                architecture = 32;
            }
            return architecture != 64 ? Architecture.Win32 : Architecture.Win64;
        }

        private static WinRegistryBase GetRegistryBaseForArchitecture(Architecture architecture)
        {
            if(architecture == Architecture.Win32)
            {
                return new Win32Registry();
            }
            return new Win64Registry();
        }

        public static string HasKey(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[1];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.SubKeyExists(path, subKeyName).ToIntString();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Error while checking for existence of {subKeyName} in {path}");
                return false.ToIntString();
            }
        }

        public static string CreateKey(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[1];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.CreateSubKey(path, subKeyName).ToIntString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while creating {subKeyName} in {path}");
                return false.ToIntString();
            }
        }

        public static string DeleteKey(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[1];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.DeleteSubKey(path, subKeyName).ToIntString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while deleting {subKeyName} in {path}");
                return false.ToIntString();
            }
        }

        public static string HasValue(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[1];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.HasValue(path, valueName).ToIntString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while deleting {valueName} in {path}");
                return false.ToIntString();
            }
        }

        public static string SetValue(string[] parameters)
        {
            if (parameters.Length <= 3)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[1];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            // Value can be an empty string
            var value = parameters[2];

            var valueType = parameters[3];
            if (string.IsNullOrEmpty(valueType))
                return false.ToIntString();

            if (!Enum.TryParse<Microsoft.Win32.RegistryValueKind>(valueType, out var valueKind))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 4);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.SetValue(path, valueName, value, valueKind).ToIntString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while deleting {valueName} in {path}");
                return false.ToIntString();
            }
        }

        public static string GetValue(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[1];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.GetValue(path, valueName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while deleting {valueName} in {path}");
                return false.ToIntString();
            }
        }

        public static string DeleteValue(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var path = parameters[0];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[1];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            var architecture = GetArchitecture(parameters, 2);
            var registry = GetRegistryBaseForArchitecture(architecture);

            try
            {
                return registry.HasValue(path, valueName).ToIntString();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while deleting {valueName} in {path}");
                return false.ToIntString();
            }
        }
    }
}
