using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings.Install;
using NLog;
using System;
using System.Diagnostics;

namespace DeploymentToolkit.Installer.MSI
{
    public class MSIInstaller : Installer
    {
        public override InstallerType InstallerType
        {
            get => InstallerType.MicrosoftInstaller;
        }

        public override event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private string _commandLine = "";
        private Process _installerProcess;

        public MSIInstaller(InstallSettings installSettings) : base(installSettings)
        {
            if (!installSettings.CommandLine.ToLower().EndsWith(".msi"))
                throw new Exception("MSIInstaller can only be used with MSI installations");

            if(!InstallSettings.MSISettings.UseDefaultMSIParameters && string.IsNullOrEmpty(InstallSettings.Parameters))
            {
                _logger.Warn($"No command line specified on an MSI installation. Using default parameters ('{ToolkitEnvironment.MSI.DefaultInstallParameters}')");
                InstallSettings.Parameters = ToolkitEnvironment.MSI.DefaultInstallParameters;
            }

            if (InstallSettings.MSISettings.UseDefaultMSIParameters)
            {
                _logger.Info($"MSISettings->UseDefaultMSIParameters specified. Switching Parameters from '{InstallSettings.Parameters}' to '{ToolkitEnvironment.MSI.DefaultInstallParameters}'");
                InstallSettings.Parameters = ToolkitEnvironment.MSI.DefaultInstallParameters;
            }

            if (InstallSettings.MSISettings.SupressMSIRestartReturnCode)
            {
                _logger.Info("MSISettings->SupressMSIRestartReturnCode specified. Suppressing restarts after installation");
            }

            _commandLine = $"/i \"{InstallSettings.CommandLine}\" {InstallSettings.Parameters}";
            // Add the file name for logging
            var logFileName = "DeploymentToolkit-MSI.log";
            var path = System.IO.Path.Combine(Logging.LogManager.LogDirectory, logFileName);
            // Beware the space !!!
            _commandLine += $" {ToolkitEnvironment.MSI.DefaultLoggingParameters} \"{path}\"";

            if(InstallSettings.MSISettings.SuppressReboot)
            {
                _logger.Info($"Adding {ToolkitEnvironment.MSI.SuppressReboot} to command line");
                _commandLine += $" {ToolkitEnvironment.MSI.SuppressReboot}";
            }

            _logger.Info($"Final command line: {_commandLine}");
        }

        public override void SequenceBegin()
        {
            _logger.Trace("Preparing process execution...");

            _installerProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "msiexec.exe",
                    Arguments = _commandLine,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };

            _installerProcess.Exited += OnInstallationEnded;
            _installerProcess.Start();

            _logger.Info($"Started MSI installation process. {_installerProcess.Id} in {_installerProcess.SessionId}");
        }

        public override void SequenceEnd()
        {
            
        }

        private void OnInstallationEnded(object sender, EventArgs e)
        {
            _logger.Info("MSI installation process ended. Validating...");

            if(!Enum.TryParse<MSIReturnCode>(_installerProcess.ExitCode.ToString(), true, out var exitCode))
            {
                _logger.Error($"Failed to decode return code ({_installerProcess.ExitCode}). Passing return code straight to parent...");
                OnSequenceCompleted?.BeginInvoke(
                    this,
                    new SequenceCompletedEventArgs()
                    {
                        // We don't know the exit code so it's probably a non default exit code. Meaning it probably failed
                        SequenceSuccessful = false,
                        ReturnCode = _installerProcess.ExitCode
                    },
                    OnSequenceCompleted.EndInvoke,
                    null
                );
                return;
            }
            
            if(exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED && InstallSettings.MSISettings.SuppressReboot)
            {
                _logger.Info($"MSI returned {MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED} and SuppressReboot specified. Overwriting return code...");
                exitCode = MSIReturnCode.ERROR_SUCCESS;
            }

            var installCompletedEventArgs = new SequenceCompletedEventArgs()
            {
                ReturnCode = _installerProcess.ExitCode
            };

            switch (exitCode)
            {
                case MSIReturnCode.ERROR_SUCCESS:
                case MSIReturnCode.ERROR_SUCCESS_REBOOT_INITIATED:
                case MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED:
                    {
                        _logger.Info($"Installation successful. Return code {exitCode}({_installerProcess.ExitCode})");
                        installCompletedEventArgs.SequenceSuccessful = true;

                        if(exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_INITIATED)
                        {
                            _logger.Warn("MSI initiated a reboot!");
                            installCompletedEventArgs.SequenceWarnings.Add(new Exception("MSI intiated a reboot"));
                        }
                        else if(exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED && InstallSettings.MSISettings.SupressMSIRestartReturnCode)
                        {
                            _logger.Info($"Replacing return code with {MSIReturnCode.ERROR_SUCCESS}({(int)MSIReturnCode.ERROR_SUCCESS}) because MSISettings->SupressMSIRestartReturnCode was specified");
                            installCompletedEventArgs.ReturnCode = (int)MSIReturnCode.ERROR_SUCCESS;
                        }
                    }
                    break;

                default:
                    {
                        _logger.Error($"Installation failed. Return code {exitCode}({_installerProcess.ExitCode})");
                        installCompletedEventArgs.SequenceSuccessful = false;
                        installCompletedEventArgs.SequenceErrors.Add(new Exception($"Install failed with code {exitCode}({(int)exitCode})"));

                        // Output some more information to the log file for faster troubleshooting
                        if(
                            exitCode == MSIReturnCode.ERROR_INSTALL_TRANSFORM_FAILURE ||
                            exitCode == MSIReturnCode.ERROR_INSTALL_TRANSFORM_REJECTED)
                        {
                            _logger.Error("There seems to be a problem with the transform. Please check the transform");
                        }

                        if(
                            exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_INVALID ||
                            exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_OPEN_FAILED)
                        {
                            _logger.Error("There seems to be a problem with the msi package. Please check the msi file");
                        }

                        if(exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_REJECTED)
                        {
                            _logger.Error("The msi file was rejected by the system. Please check the msi file (requirements)");
                        }

                        if(exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_VERSION)
                        {
                            _logger.Error("The msi file is created for a newer version of Windows Installer. Please update the Windows Installer service");
                        }
                    }
                    break;
            }

            OnSequenceCompleted?.BeginInvoke(
                this,
                installCompletedEventArgs,
                OnSequenceCompleted.EndInvoke,
                null
            );
        }
    }
}
