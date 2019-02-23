using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.Modals.Settings.Uninstall;
using DeploymentToolkit.ToolkitEnvironment;
using NLog;
using System;

namespace DeploymentToolkit.Uninstaller
{
    public abstract class Uninstaller : IInstallUninstallSequence
    {
        public abstract UninstallerType UninstallerType { get; }

        public abstract event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        public UninstallSettings InstallSettings { get; set; }

        private string _unqiueName;
        public string UniqueName
        {
            get
            {
                if (string.IsNullOrEmpty(_unqiueName))
                    _unqiueName = $"{EnvironmentVariables.Configuration.Name}_{EnvironmentVariables.Configuration.Version}_Uninstall";
                return _unqiueName;
            }
        }
        public CloseProgramsSettings CloseProgramsSettings { get => InstallSettings.CloseProgramsSettings; }
        public DeferSettings DeferSettings { get => InstallSettings.DeferSettings; }
        public RestartSettings RestartSettings { get => InstallSettings.RestartSettings; }
        public LogoffSettings LogoffSettings { get => InstallSettings.LogoffSettings; }
        public CustomActions CustomActions { get => InstallSettings.CustomActions; }

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public IInstallUninstallSequence SubSequence
        {
            get => throw new NotSupportedException();
        }

        public Uninstaller(UninstallSettings installSettings)
        {
            InstallSettings = installSettings;
        }

        public abstract void SequenceBegin();

        public abstract void SequenceEnd();
    }
}
