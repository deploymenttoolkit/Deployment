using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.Modals.Settings.Uninstall;
using DeploymentToolkit.RegistryWrapper;
using DeploymentToolkit.ToolkitEnvironment;
using DeploymentToolkit.Uninstaller.MSI;
using Microsoft.Win32;
using NLog;
using System;
using System.IO;
using System.Threading;

namespace DeploymentToolkit.Installer
{
    public abstract class Installer : ISequence
    {
        public abstract InstallerType InstallerType { get; }
        public abstract event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        public InstallSettings InstallSettings { get; set; }

        private string _unqiueName;
        public string UniqueName
        {
            get
            {
                if(string.IsNullOrEmpty(_unqiueName))
                {
                    _unqiueName = $"{EnvironmentVariables.Configuration.Name}_{EnvironmentVariables.Configuration.Version}_Install";
                }

                return _unqiueName;
            }
        }
        public CloseProgramsSettings CloseProgramsSettings => InstallSettings.CloseProgramsSettings;
        public DeferSettings DeferSettings => InstallSettings.DeferSettings;
        public RestartSettings RestartSettings => InstallSettings.RestartSettings;
        public LogoffSettings LogoffSettings => InstallSettings.LogoffSettings;
        public CustomActions CustomActions => InstallSettings.CustomActions;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private volatile bool _uninstallCompleted = false;
        private volatile bool _uninstallSuccess = false;

        public ISequence SubSequence => throw new NotSupportedException();

        public Installer(InstallSettings installSettings)
        {
            InstallSettings = installSettings;
        }

        public abstract void SequenceBegin();

        public abstract void SequenceEnd();

        public void BeforeSequenceComplete(bool success)
        {
            _logger.Trace($"Running BeforeSequenceComplete actions ...");

            if(!success)
            {
                _logger.Debug("Install was not successfull. Skipping actions");
                return;
            }

            if(InstallSettings.ActiveSetupSettings != null && InstallSettings.ActiveSetupSettings.UseActiveSetup)
            {
                ProcessActiveSetup();
            }
        }

        private void ProcessActiveSetup()
        {
            _logger.Trace("Setting up ActiveSetup ...");

            var activeSetupSettings = InstallSettings.ActiveSetupSettings;

            if(string.IsNullOrEmpty(activeSetupSettings.Name))
            {
                if(InstallerType == InstallerType.Executable)
                {
                    activeSetupSettings.Name = UniqueName;
                }
                else
                {
                    activeSetupSettings.Name = ToolkitEnvironment.MSI.ProductCode;
                }
            }

            if(string.IsNullOrEmpty(activeSetupSettings.CommandLine))
            {
                if(InstallerType == InstallerType.Executable)
                {
                    _logger.Warn("No ActiveSetup entry created. Commandline cannot be empty");
                    return;
                }

                activeSetupSettings.CommandLine = $"msiexec.exe {ToolkitEnvironment.MSI.ActiveSetupParameters} {ToolkitEnvironment.MSI.ProductCode} {ToolkitEnvironment.MSI.DefaultSilentParameters}";
            }

            if(string.IsNullOrEmpty(activeSetupSettings.Version))
            {
                if(InstallerType == InstallerType.Executable)
                {
                    activeSetupSettings.Version = EnvironmentVariables.Configuration.Version;
                }
                else
                {
                    activeSetupSettings.Version = ToolkitEnvironment.MSI.ProductVersion;
                }
            }

            _logger.Info($"Creating ActiveSetup entry for [{activeSetupSettings.Version}]{activeSetupSettings.Name} and command line '{activeSetupSettings.CommandLine}'");

            var registry = new Win64Registry();
            try
            {
                if(!registry.CreateSubKey(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name))
                {
                    _logger.Error("Failed to create active setup entry");
                    return;
                }

                var activeSetupEntryPath = Path.Combine(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name);
                if(!registry.SetValue(activeSetupEntryPath, "StubPath", activeSetupSettings.CommandLine, RegistryValueKind.String))
                {
                    _logger.Error("Failed to set StubPath");
                    registry.DeleteSubKey(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name);
                }

                if(!registry.SetValue(activeSetupEntryPath, "Version", activeSetupSettings.Version.Replace('.', ','), RegistryValueKind.String))
                {
                    _logger.Error("Failed to set Version");
                    registry.DeleteSubKey(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name);
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Failed to create ActiveSetup entry");

                // Delete the key if it was created
                registry.DeleteSubKey(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name);
            }

            _logger.Info("ActiveSetup entry successfully created");
        }

        public void BeforeSequenceBegin()
        {
            _logger.Trace($"Running BeforeSequenceBegin actions ...");

            if(InstallSettings.UninstallSettings != null && InstallSettings.UninstallSettings.Uninstall.Count > 0)
            {
                var uninstallSettings = InstallSettings.UninstallSettings;
                if(uninstallSettings.IgnoreUninstallErrors)
                {
                    _logger.Debug($"Uninstall errors are being ignored");
                }

                foreach(var application in uninstallSettings.Uninstall)
                {
                    if(string.IsNullOrEmpty(application.Text))
                    {
                        _logger.Warn("Invalid Uninstall item");
                        continue;
                    }

                    var matches = RegistryManager.GetInstalledMSIProgramsByName(application.Text);
                    if(matches.Count == 0)
                    {
                        _logger.Debug($"No match found for {application.Text}. Skipping");
                        continue;
                    }

                    foreach(var match in matches)
                    {
                        _logger.Info($"Uninstalling [{match.ProductId}] {match.DisplayName}");

                        _uninstallCompleted = false;

                        var uninstaller = new MSIUninstaller(new UninstallSettings()
                        {
                            CommandLine = match.ProductId,
                            MSISettings = new MSISettings()
                            {
                                SuppressReboot = true,
                                SupressMSIRestartReturnCode = true,
                                UseDefaultMSIParameters = true
                            },
                            LogFileSuffix = match.DisplayName.Replace(' ', '-')
                        });

                        uninstaller.OnSequenceCompleted += OnUninstallComplete;
                        uninstaller.SequenceBegin();

                        do
                        {
                            Thread.Sleep(10);
                        }
                        while(!_uninstallCompleted);

                        _logger.Trace($"Finished uninstalling {match.ProductId}");

                        if(!_uninstallSuccess && !uninstallSettings.IgnoreUninstallErrors)
                        {
                            _logger.Warn($"Uninstallation of {match.ProductId} failed. Aborting");
                            throw new Exception($"Failed to uninstall {match.ProductId}");
                        }
                    }
                }
            }
        }

        private void OnUninstallComplete(object sender, SequenceCompletedEventArgs e)
        {
            try
            {
                var uninstaller = (MSIUninstaller)sender;
                _logger.Trace($"Finished uninstalling. Result: {e.SequenceSuccessful}");
                _uninstallSuccess = e.SequenceSuccessful;

                if(!e.SequenceSuccessful && InstallSettings.UninstallSettings.IgnoreUninstallErrors)
                {
                    _logger.Info("Uninstallation reported errors but 'IgnoreUninstallErrors' flag is set. Ignoring errors");
                    _uninstallSuccess = true;
                }

                if(!e.SequenceSuccessful)
                {
                    if(e.CountErrors > 0)
                    {
                        foreach(var error in e.SequenceErrors)
                        {
                            if(InstallSettings.UninstallSettings.IgnoreUninstallErrors)
                            {
                                _logger.Info(error);
                            }
                            else
                            {
                                _logger.Error(error);
                            }
                        }
                    }

                    if(e.CountWarnings > 0)
                    {
                        foreach(var warning in e.SequenceWarnings)
                        {
                            if(InstallSettings.UninstallSettings.IgnoreUninstallErrors)
                            {
                                _logger.Info(warning);
                            }
                            else
                            {
                                _logger.Warn(warning);
                            }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error during OnUninstallComplete");
            }
            finally
            {
                _uninstallCompleted = true;
            }
        }
    }
}
