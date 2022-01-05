using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.Modals.Settings.Uninstall;
using DeploymentToolkit.RegistryWrapper;
using DeploymentToolkit.ToolkitEnvironment;
using NLog;
using System;

namespace DeploymentToolkit.Uninstaller
{
    public abstract class Uninstaller : ISequence
    {
        public abstract UninstallerType UninstallerType { get; }

        public abstract event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        public UninstallSettings UninstallSettings { get; set; }

        private string _unqiueName;
        public string UniqueName
        {
            get
            {
                if(string.IsNullOrEmpty(_unqiueName))
                {
                    _unqiueName = $"{EnvironmentVariables.Configuration.Name}_{EnvironmentVariables.Configuration.Version}_Uninstall";
                }

                return _unqiueName;
            }
        }
        public CloseProgramsSettings CloseProgramsSettings => UninstallSettings.CloseProgramsSettings;
        public DeferSettings DeferSettings => UninstallSettings.DeferSettings;
        public RestartSettings RestartSettings => UninstallSettings.RestartSettings;
        public LogoffSettings LogoffSettings => UninstallSettings.LogoffSettings;
        public CustomActions CustomActions => UninstallSettings.CustomActions;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public ISequence SubSequence => throw new NotSupportedException();

        public Uninstaller(UninstallSettings installSettings)
        {
            UninstallSettings = installSettings;
        }

        public abstract void SequenceBegin();

        public abstract void SequenceEnd();

        public void BeforeSequenceBegin()
        {

        }

        public void BeforeSequenceComplete(bool success)
        {
            _logger.Trace("Running BeforeSequenceComplete actions ...");

            if(UninstallSettings.ActiveSetupSettings != null && UninstallSettings.ActiveSetupSettings.UseActiveSetup)
            {
                ProcessActiveSetup();
            }
        }

        private void ProcessActiveSetup()
        {
            _logger.Trace("Deleting ActiveSetup entry ...");

            var activeSetupSettings = UninstallSettings.ActiveSetupSettings;

            if(string.IsNullOrEmpty(activeSetupSettings.Name))
            {
                if(UninstallerType == UninstallerType.Executable)
                {
                    activeSetupSettings.Name = UniqueName;
                }
                else
                {
                    activeSetupSettings.Name = ToolkitEnvironment.MSI.ProductCode;
                }
            }

            var registry = new Win64Registry();
            try
            {
                _logger.Trace("Deleting Machine keys ...");
                if(!registry.DeleteSubKey(ToolkitEnvironment.MSI.ActiveSetupPath, activeSetupSettings.Name))
                {
                    _logger.Error("Failed to delete active setup entry");
                    return;
                }

                _logger.Trace("Deleting User keys ...");
                var users = registry.GetSubKeys("HKEY_USERS");
                foreach(var sId in users)
                {
                    if(sId == ".DEFAULT")
                    {
                        continue;
                    }

                    if(sId.EndsWith("_Classes"))
                    {
                        continue;
                    }

                    _logger.Trace($"Processing {sId}");

                    registry.DeleteSubKey(string.Format(ToolkitEnvironment.MSI.ActiveSetupUserPath, sId), activeSetupSettings.Name);
                }

                _logger.Info("Successfully deleted ActiveSetup keys");
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Failed to delete ActiveSetup entry");
            }
        }
    }
}
