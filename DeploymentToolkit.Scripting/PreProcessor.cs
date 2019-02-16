using DeploymentToolkit.DTEnvironment;
using DeploymentToolkit.DTEnvironment.Exceptions;
using DeploymentToolkit.Scripting.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DeploymentToolkit.Scripting
{
    public static class PreProcessor
    {
        private const string _separator = "$";

        private static readonly Dictionary<string, Func<string>> _variables = new Dictionary<string, Func<string>>()
        {
            {
                "Is64Bit",
                delegate()
                {
                    return Environment.Is64BitOperatingSystem.ToIntString();
                }
            },
            {
                "Is64BitProcess",
                delegate()
                {
                    return Environment.Is64BitProcess.ToIntString();
                }
            },
            {
                "Is32Bit",
                delegate()
                {
                    return (!Environment.Is64BitOperatingSystem).ToIntString();
                }
            },
            {
                "Is32BitProcess",
                delegate()
                {
                    return (!Environment.Is64BitProcess).ToIntString();
                }
            },
            {
                "DT_InstallPath",
                delegate()
                {
                    try
                    {
                        return EnvironmentVariables.DeploymentToolkitInstallPath;
                    }
                    catch(DeploymentToolkitInstallPathNotFoundException)
                    {
                        return "";
                    }
                }
            },
            {
                "DT_FilesPath",
                delegate()
                {
                    // TODO: Add the right path
                    try
                    {
                        return EnvironmentVariables.DeploymentToolkitInstallPath;
                    }
                    catch(DeploymentToolkitInstallPathNotFoundException)
                    {
                        return "";
                    }
                }
            },
            {
                "DT_DeploymentUniqueName",
                delegate()
                {
                    return EnvironmentVariables.ActiveSequence.UniqueName;
                }
            },
            {
                "DT_IsTaskSequence",
                delegate()
                {
                    return EnvironmentVariables.IsRunningInTaskSequence.ToIntString();
                }
            },
        };

        private static readonly Dictionary<string, Func<string[], string>> _functions = new Dictionary<string, Func<string[], string>>()
        {
            {
                "FileExists",
                delegate(string[] parameters)
                {
                    if(parameters.Length == 0)
                        return false.ToIntString();

                    var path = parameters[0];
                    if(!Path.IsPathRooted(path))
                        path = Path.GetFullPath(path);
                    return File.Exists(path).ToIntString();
                }
            },
            {
                "DirectoryExists",
                delegate(string[] parameters)
                {
                    if(parameters.Length == 0)
                        return false.ToIntString();

                    var path = parameters[0];
                    if(!Path.IsPathRooted(path))
                        path = Path.GetFullPath(path);
                    return Directory.Exists(path).ToIntString();
                }
            }
        };

        public static string Process(string data)
        {
            // Nothing to process
            if (!data.Contains(_separator))
                return data;

            var processed = data;
            var toProcess = data;

            do
            {
                var start = toProcess.IndexOf(_separator);
                if (start == -1)
                    break;

                var part = toProcess.Substring(start + 1, toProcess.Length - start - 1);
                var end = part.IndexOf(_separator);
                if (end == -1)
                    break;

                var variableName = part.Substring(0, end);
                Debug.WriteLine($"Found {variableName}");
                Debug.WriteLine($"ToProcess: {toProcess}");
                toProcess = toProcess.Substring(end, toProcess.Length - end);

                if (variableName.Contains("("))
                {
                    var variablesStart = variableName.IndexOf('(');
                    var variablesEnd = variableName.IndexOf(')');

                    if (variablesStart == -1 || variablesEnd == -1)
                    {
                        processed = processed.Replace($"${variableName}$", "INCOMPLETE PARAMETERS");
                        continue;
                    }

                    var parameterString = variableName.Substring(variablesStart, variablesEnd - variablesStart).TrimStart('(').TrimEnd(')');
                    if(string.IsNullOrEmpty(parameterString))
                    {
                        processed = processed.Replace($"${variableName}$", "MISSING PARAMETERS");
                        continue;
                    }

                    var functionName = variableName.Substring(0, variablesStart);
                    Debug.WriteLine($"Function: {functionName}");
                    Debug.WriteLine($"Params: {parameterString}");

                    if (_functions.ContainsKey(functionName))
                    {
                        var parameters = parameterString.Split(',');
                        processed = processed.Replace($"${variableName}$", _functions[functionName].Invoke(parameters));
                    }
                    else
                    {
                        processed = processed.Replace($"${variableName}$", "INVALID FUNCTION");
                    }
                }
                else
                {
                    if (_variables.ContainsKey(variableName))
                        processed = processed.Replace($"${variableName}$", _variables[variableName].Invoke());
                    else
                        processed = processed.Replace($"${variableName}$", "VARIABLE NOT FOUND");
                }
            }
            while (toProcess.Contains(_separator));

            Debug.WriteLine($"Processed: {processed}");

            return processed;
        }
    }
}
