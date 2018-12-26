using System;
using System.IO;

namespace DeploymentToolkit.Settings
{
    public static class Environment
    {
        private static string _deploymentToolkitInstallPath = null;
        public static string GetDeploymentToolkitInstallPath
        {
            get
            {
                if (_deploymentToolkitInstallPath != null)
                    return _deploymentToolkitInstallPath;

                var installPath = System.Environment.GetEnvironmentVariable("DTInstallPath", EnvironmentVariableTarget.Machine);
                if (installPath == null)
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    var debuggerPath = Path.Combine(currentDirectory, "DeploymentToolkit.Debugger.exe");
                    if (File.Exists(debuggerPath))
                    {
                        installPath = debuggerPath;
                    }
                    else
                    {
                        // Maybe add more options later??
                        // Like a registry entry with the install path other than an EnvironmentVariable
                        throw new Exception("DeploymentToolkit installation path could not be found");
                    }
                }

                _deploymentToolkitInstallPath = installPath;
                return _deploymentToolkitInstallPath;
            }
        }
    }
}
