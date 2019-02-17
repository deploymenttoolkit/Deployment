using DeploymentToolkit.Scripting.Extensions;
using DeploymentToolkit.ToolkitEnvironment;
using DeploymentToolkit.ToolkitEnvironment.Exceptions;
using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace DeploymentToolkit.Scripting
{
    public static partial class PreProcessor
    {
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

        public static bool AddVariable(string name, string script)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            if (string.IsNullOrEmpty(script))
                throw new ArgumentNullException(nameof(script));

            if (_variables.ContainsKey(name))
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
                return false;
            }
        }

        private static string GetResultFromPSObject(PSObject input)
        {
            var type = input.BaseObject.GetType();

            if (type == typeof(bool))
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
