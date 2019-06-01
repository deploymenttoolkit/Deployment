using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.Modals.Settings.Uninstall;
using DeploymentToolkit.ToolkitEnvironment.Exceptions;
using NLog;
using System;
using System.IO;

namespace DeploymentToolkit.ToolkitEnvironment
{
    public static partial class EnvironmentVariables
    {
        public static SequenceType ActiveSequenceType;
        public static IInstallUninstallSequence ActiveSequence;

        public static Configuration Configuration;
        public static InstallSettings InstallSettings;
        public static UninstallSettings UninstallSettings;

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private const string _deploymentToolkitRestartExeName = "DeploymentToolkit.Restart.exe";
        private const string _deploymentToolkitDeploymentExeName = "DeploymentToolkit.Deployment.exe";

        private static readonly string[] _requiredToolkitFiles = new string[]
        {
            _deploymentToolkitRestartExeName,
            _deploymentToolkitDeploymentExeName
        };

        private static string _deploymentToolkitInstallPath = null;
        public static string DeploymentToolkitInstallPath
        {
            get
            {
                if (_deploymentToolkitInstallPath != null)
                    return _deploymentToolkitInstallPath;

                var installPath = Environment.GetEnvironmentVariable("DTInstallPath", EnvironmentVariableTarget.Machine);
                if (installPath == null)
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    foreach(var file in _requiredToolkitFiles)
                    {
                        var debuggerPath = Path.Combine(currentDirectory, file);
                        if (!File.Exists(debuggerPath))
                        {
                            // Maybe add more options later??
                            // Like a registry entry with the install path other than an EnvironmentVariable
                            throw new DeploymentToolkitInstallPathNotFoundException("DeploymentToolkit installation path could not be found", file);
                        }
                    }
                    installPath = currentDirectory;
                }

                _deploymentToolkitInstallPath = installPath;
                return _deploymentToolkitInstallPath;
            }
        }

        private static string _deploymentToolkitRestartExePath = null;
        public static string DeploymentToolkitRestartExePath
        {
            get
            {
                if (_deploymentToolkitRestartExePath == null)
                    _deploymentToolkitRestartExePath = Path.Combine(DeploymentToolkitInstallPath, _deploymentToolkitRestartExeName);
                return _deploymentToolkitRestartExePath;
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
