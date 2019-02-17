using DeploymentToolkit.Scripting.Extensions;
using DeploymentToolkit.Scripting.PreProcessorFunctions;
using DeploymentToolkit.ToolkitEnvironment;
using DeploymentToolkit.ToolkitEnvironment.Exceptions;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management.Automation;

namespace DeploymentToolkit.Scripting
{
    public static class PreProcessor
    {
        private const string _separator = "$";
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

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
                FileFunctions.Exists
            },
            {
                "DirectoryExists",
                DirectoryFunctions.Exists
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

        public static bool AddVariable(string name, string script)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(script))
                throw new ArgumentNullException(nameof(script));

            if(_variables.ContainsKey(name))
            {
                _logger.Warn($"Tried to overwrite an already existing variable! ({name})");
                return false;
            }

            try
            {
                using (var powershell = PowerShell.Create())
                {
                    powershell.AddScript(script, false);
                    powershell.Invoke();
                    powershell.Commands.Clear();
                    powershell.AddCommand(name);
                    var results = powershell.Invoke();
                    var result = GetResultFromPSObject(
                        results.Count >= 1 ?
                        results[0] :
                        string.Empty
                    );

                    _variables.Add(name, delegate ()
                    {
                        return result;
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while trying to add CustomVariable {name}");
                Debug.WriteLine(ex);
                return false;
            }
        }

        private static string GetResultFromPSObject(PSObject input)
        {
            var type = input.BaseObject.GetType();

            if(type == typeof(bool))
            {
                return ((bool)input.BaseObject).ToIntString();
            }
            else
            {
                return (string)input.BaseObject;
            }
        }
    }
}
