using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings.Uninstall;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DeploymentToolkit.Uninstaller.MSI
{
    public class MSIUninstaller : Uninstaller
    {
        public override UninstallerType UninstallerType
        {
            get => UninstallerType.MicrosoftInstaller;
        }

        public override event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private string _commandLine = "";
        private Process _uninstallerProcess;

        public MSIUninstaller(UninstallSettings uninstallSettings) : base(uninstallSettings)
        {
            if (!UninstallSettings.MSISettings.UseDefaultMSIParameters && string.IsNullOrEmpty(UninstallSettings.Parameters))
            {
                _logger.Warn($"No command line specified on an MSI uninstallation. Using default parameters ('{ToolkitEnvironment.MSI.DefaultUninstallParameters}')");
                UninstallSettings.Parameters = ToolkitEnvironment.MSI.DefaultUninstallParameters;
            }

            if (UninstallSettings.MSISettings.UseDefaultMSIParameters)
            {
                _logger.Info($"MSISettings->UseDefaultMSIParameters specified. Switching Parameters from '{UninstallSettings.Parameters}' to '{ToolkitEnvironment.MSI.DefaultUninstallParameters}'");
                UninstallSettings.Parameters = ToolkitEnvironment.MSI.DefaultUninstallParameters;
            }

            if (UninstallSettings.MSISettings.SupressMSIRestartReturnCode)
            {
                _logger.Info("MSISettings->SupressMSIRestartReturnCode specified. Suppressing restarts after installation");
            }

            _commandLine = $"/x \"{UninstallSettings.CommandLine}\" {UninstallSettings.Parameters}";
            // Add the file name for logging
            var logFileName = "DeploymentToolkit-MSI_uninstallation.log";
            if (!string.IsNullOrEmpty(UninstallSettings.LogFileSuffix))
                logFileName = $"DeploymentToolkit-MSI_uninstallation_{UninstallSettings.LogFileSuffix}.log";

            var path = System.IO.Path.Combine(Logging.LogManager.LogDirectory, logFileName);
            // Beware the space !!!
            _commandLine += $" {ToolkitEnvironment.MSI.DefaultLoggingParameters} \"{path}\"";

            if (UninstallSettings.MSISettings.SuppressReboot)
            {
                _logger.Info($"Adding {ToolkitEnvironment.MSI.SuppressReboot} to command line");
                _commandLine += $" {ToolkitEnvironment.MSI.SuppressReboot}";
            }

            _logger.Info($"Final command line: {_commandLine}");
        }

        public override void SequenceBegin()
        {
            _logger.Trace("Preparing process execution...");

            _uninstallerProcess = new Process()
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

            _uninstallerProcess.Exited += OnInstallationEnded;
            _uninstallerProcess.Start();

            _logger.Info($"Started MSI uninstallation process. {_uninstallerProcess.Id} in {_uninstallerProcess.SessionId}");
        }

        public override void SequenceEnd()
        {

        }

        private void OnInstallationEnded(object sender, EventArgs e)
        {
            _logger.Info("MSI uninstallation process ended. Validating...");

            if (!Enum.TryParse<MSIReturnCode>(_uninstallerProcess.ExitCode.ToString(), true, out var exitCode))
            {
                _logger.Error($"Failed to decode return code ({_uninstallerProcess.ExitCode}). Passing return code straight to parent...");
                BeforeSequenceComplete(false);
                OnSequenceCompleted?.BeginInvoke(
                    this,
                    new SequenceCompletedEventArgs()
                    {
                        // We don't know the exit code so it's probably a non default exit code. Meaning it probably failed
                        SequenceSuccessful = false,
                        ReturnCode = _uninstallerProcess.ExitCode,
                        SequenceErrors = new List<Exception>()
                        {
                            new Exception($"Failed to decode return code ({_uninstallerProcess.ExitCode}). Assuming installation failure")
                        }
                    },
                    OnSequenceCompleted.EndInvoke,
                    null
                );
                return;
            }

            if (exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED && UninstallSettings.MSISettings.SuppressReboot)
            {
                _logger.Info($"MSI returned {MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED} and SuppressReboot specified. Overwriting return code...");
                exitCode = MSIReturnCode.ERROR_SUCCESS;
            }

            var uninstallCompletedEventArgs = new SequenceCompletedEventArgs()
            {
                ReturnCode = _uninstallerProcess.ExitCode
            };

            switch (exitCode)
            {
                case MSIReturnCode.ERROR_SUCCESS:
                case MSIReturnCode.ERROR_SUCCESS_REBOOT_INITIATED:
                case MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED:
                    {
                        _logger.Info($"Uninstallation successful. Return code {exitCode}({_uninstallerProcess.ExitCode})");
                        uninstallCompletedEventArgs.SequenceSuccessful = true;

                        if (exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_INITIATED)
                        {
                            _logger.Warn("MSI initiated a reboot!");
                            uninstallCompletedEventArgs.SequenceWarnings.Add(new Exception("MSI intiated a reboot"));
                        }
                        else if (exitCode == MSIReturnCode.ERROR_SUCCESS_REBOOT_REQUIRED && UninstallSettings.MSISettings.SupressMSIRestartReturnCode)
                        {
                            _logger.Info($"Replacing return code with {MSIReturnCode.ERROR_SUCCESS}({(int)MSIReturnCode.ERROR_SUCCESS}) because MSISettings->SupressMSIRestartReturnCode was specified");
                            uninstallCompletedEventArgs.ReturnCode = (int)MSIReturnCode.ERROR_SUCCESS;
                        }
                    }
                    break;

                default:
                    {
                        _logger.Error($"Uninstallation failed. Return code {exitCode}({_uninstallerProcess.ExitCode})");
                        uninstallCompletedEventArgs.SequenceSuccessful = false;
                        uninstallCompletedEventArgs.SequenceErrors.Add(new Exception($"Uninstall failed with code {exitCode}({(int)exitCode})"));

                        // Output some more information to the log file for faster troubleshooting
                        if (
                            exitCode == MSIReturnCode.ERROR_INSTALL_TRANSFORM_FAILURE ||
                            exitCode == MSIReturnCode.ERROR_INSTALL_TRANSFORM_REJECTED)
                        {
                            _logger.Error("There seems to be a problem with the transform. Please check the transform");
                        }

                        if (
                            exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_INVALID ||
                            exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_OPEN_FAILED)
                        {
                            _logger.Error("There seems to be a problem with the msi package. Please check the msi file");
                        }

                        if (exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_REJECTED)
                        {
                            _logger.Error("The msi file was rejected by the system. Please check the msi file (requirements)");
                        }

                        if (exitCode == MSIReturnCode.ERROR_INSTALL_PACKAGE_VERSION)
                        {
                            _logger.Error("The msi file is created for a newer version of Windows Installer. Please update the Windows Installer service");
                        }
                    }
                    break;
            }

            BeforeSequenceComplete(uninstallCompletedEventArgs.SequenceSuccessful);

            OnSequenceCompleted?.BeginInvoke(
                this,
                uninstallCompletedEventArgs,
                OnSequenceCompleted.EndInvoke,
                null
            );
        }
    }
}
