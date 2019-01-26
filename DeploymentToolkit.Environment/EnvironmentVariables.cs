using DeploymentToolkit.Deployment.Settings;
using DeploymentToolkit.Deployment.Settings.Install;
using DeploymentToolkit.DTEnvironment.Exceptions;
using DeploymentToolkit.Modals;
using NLog;
using System;
using System.IO;

namespace DeploymentToolkit.DTEnvironment
{
    public static partial class EnvironmentVariables
    {
        public static SequenceType ActiveSequenceType;
        public static IInstallUninstallSequence ActiveSequence;

        public static Configuration Configuration;
        public static InstallSettings InstallSettings;

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

        public static void Initialize()
        {
            CheckRunningInTaskSequence();
        }

        private static void CheckRunningInTaskSequence()
        {
            _logger.Trace("Checking if running in TaskSequence...");

            var tsEnvironment = Type.GetTypeFromProgID("Microsoft.SMS.TSEnvironment");
            if (tsEnvironment == null)
            {
                _logger.Info("Couldn't load 'Microsoft.SMS.TSEnvironment' therefore we are not in a task sequence");
                return;
            }

            _logger.Info("Successfully loaded 'Microsoft.SMS.TSEnvironment'. Task sequence mode enabled");
            IsRunningInTaskSequence = true;
        }
    }
}
