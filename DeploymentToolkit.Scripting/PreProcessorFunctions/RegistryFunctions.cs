using DeploymentToolkit.Actions;
using DeploymentToolkit.RegistryWrapper;
using DeploymentToolkit.Scripting.Extensions;
using NLog;
using System;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class RegistryFunctions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static string HasKey(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[2];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            return RegistryActions.KeyExists(architecture, path, subKeyName).ToIntString();
        }

        public static string CreateKey(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[2];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            return RegistryActions.CreateKey(architecture, path, subKeyName).ToIntString();
        }

        public static string DeleteKey(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var subKeyName = parameters[2];
            if (string.IsNullOrEmpty(subKeyName))
                return false.ToIntString();

            return RegistryActions.DeleteKey(architecture, path, subKeyName).ToIntString();
        }

        public static string HasValue(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[2];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            return RegistryActions.ValueExists(architecture, path, valueName).ToIntString();
        }

        public static string SetValue(string[] parameters)
        {
            if (parameters.Length <= 4)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[2];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            // Value can be an empty string
            var value = parameters[3];

            var valueType = parameters[4];
            if (string.IsNullOrEmpty(valueType) || !Enum.TryParse<Microsoft.Win32.RegistryValueKind>(valueType, out var valueKind))
                return false.ToIntString();

            return RegistryActions.SetValue(architecture, path, valueName, value, valueKind).ToIntString();
        }

        public static string GetValue(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[2];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            return RegistryActions.GetValue(architecture, path, valueName);
        }

        public static string DeleteValue(string[] parameters)
        {
            if (parameters.Length <= 2)
                return false.ToIntString();

            var architectureString = parameters[0];
            if (string.IsNullOrEmpty(architectureString) || !Enum.TryParse<Architecture>(architectureString, out var architecture))
                return false.ToIntString();

            var path = parameters[1];
            if (string.IsNullOrEmpty(path))
                return false.ToIntString();

            var valueName = parameters[2];
            if (string.IsNullOrEmpty(valueName))
                return false.ToIntString();

            return RegistryActions.DeleteValue(architecture, path, valueName).ToIntString();
        }
    }
}
