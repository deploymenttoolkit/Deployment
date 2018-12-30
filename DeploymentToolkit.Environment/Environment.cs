﻿using DeploymentToolkit.Environment.Exceptions;
using NLog;
using System;
using System.IO;

namespace DeploymentToolkit.Environment
{
    public static partial class DTEnvironment
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private static string _deploymentToolkitInstallPath = null;
        public static string DeploymentToolkitInstallPath
        {
            get
            {
                if (_deploymentToolkitInstallPath != null)
                    return _deploymentToolkitInstallPath;

                var installPath = System.Environment.GetEnvironmentVariable("DTInstallPath", EnvironmentVariableTarget.Machine);
                if (installPath == null)
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    var debuggerPath = Path.Combine(currentDirectory, "DeploymentToolkit.Deployment.exe"); //We should check here for all exe files (e.g. DeploymentToolkit.Debugger.exe)
                    if (File.Exists(debuggerPath))
                    {
                        installPath = currentDirectory;
                    }
                    else
                    {
                        // Maybe add more options later??
                        // Like a registry entry with the install path other than an EnvironmentVariable
                        throw new DeploymentToolkitInstallPathNotFoundException("DeploymentToolkit installation path could not be found", "DeploymentToolkit.Deployment.exe");
                    }
                }

                _deploymentToolkitInstallPath = installPath;
                return _deploymentToolkitInstallPath;
            }
        }
    }
}