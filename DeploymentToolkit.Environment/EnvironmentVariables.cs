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
        private static IInstallUninstallSequence _activeSequence;
        public static IInstallUninstallSequence ActiveSequence
        {
            get => _activeSequence;
            set
            {
                _activeSequence = value;
                RegistryManager.SetActiveSequence(value);
            }
        }

        public static Configuration Configuration;
        public static InstallSettings InstallSettings;
        public static UninstallSettings UninstallSettings;

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public const int DeploymentToolkitStepTimout = 600;
        public const int DeploymentToolkitStepExtraTime = 30;

        private const string _deploymentToolkitRestartExeName = "DeploymentToolkit.Restart.exe";
        private const string _deploymentToolkitDeploymentExeName = "DeploymentToolkit.Deployment.exe";
        private const string _deploymentToolkitBlockerExeName = "DeploymentToolkit.Blocker.exe";
        private const string _deploymentToolkitUnblockerExeName = "DeploymentToolkit.Unblocker.exe";
        private const string _deploymentToolkitTrayExeName = "DeploymentToolkit.TrayApp.exe";

        public static string DeploymentToolkitTrayExeName { get => _deploymentToolkitTrayExeName; }

        private static readonly string[] _requiredToolkitFiles = new string[]
        {
            _deploymentToolkitRestartExeName,
            _deploymentToolkitDeploymentExeName,
            _deploymentToolkitBlockerExeName,
            _deploymentToolkitUnblockerExeName,
            _deploymentToolkitTrayExeName
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
                    foreach (var file in _requiredToolkitFiles)
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

        private static string _deploymentToolkitExtensionsPath;
        public static string DeploymentToolkitExtensionsPath
        {
            get
            {
                if (_deploymentToolkitExtensionsPath == null)
                {
                    _deploymentToolkitExtensionsPath = Path.Combine(DeploymentToolkitInstallPath, "Extensions");

                    if (!Directory.Exists(_deploymentToolkitExtensionsPath))
                        Directory.CreateDirectory(_deploymentToolkitExtensionsPath);
                }
                return _deploymentToolkitExtensionsPath;
            }
        }

        private static string _deploymentToolkitSettingsPath;
        public static string DeploymentToolkitSettingsPath
        {
            get
            {
                if (_deploymentToolkitSettingsPath == null)
                {
                    _deploymentToolkitSettingsPath = Path.Combine(DeploymentToolkitInstallPath, "Config");

                    if (!Directory.Exists(_deploymentToolkitSettingsPath))
                        Directory.CreateDirectory(_deploymentToolkitSettingsPath);
                }

                return _deploymentToolkitSettingsPath;
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
        private static string _deploymentToolkitBlockerExePath = null;
        public static string DeploymentToolkitBlockerExePath
        {
            get
            {
                if (_deploymentToolkitBlockerExePath == null)
                    _deploymentToolkitBlockerExePath = Path.Combine(DeploymentToolkitInstallPath, _deploymentToolkitBlockerExeName);
                return _deploymentToolkitBlockerExePath;
            }
        }
        private static string _deploymentToolkitUnblockerExePath = null;
        public static string DeploymentToolkitUnblockerExePath
        {
            get
            {
                if (_deploymentToolkitUnblockerExePath == null)
                    _deploymentToolkitUnblockerExePath = Path.Combine(DeploymentToolkitInstallPath, _deploymentToolkitUnblockerExeName);
                return _deploymentToolkitUnblockerExePath;
            }
        }
        private static string _deploymentToolkitTrayExePath = null;
        public static string DeploymentToolkitTrayExePath
        {
            get
            {
                if (_deploymentToolkitTrayExePath == null)
                    _deploymentToolkitTrayExePath = Path.Combine(DeploymentToolkitInstallPath, _deploymentToolkitTrayExeName);
                return _deploymentToolkitTrayExePath;
            }
        }

        public static void Initialize()
        {
            RegistryManager.VerifyRegistry();
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
