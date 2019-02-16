using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.ToolkitEnvironment;
using NLog;
using System;

namespace DeploymentToolkit.Installer
{
    public abstract partial class Installer : IInstallUninstallSequence
    {
        public abstract InstallerType InstallerType { get; }
        public abstract event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        public InstallSettings InstallSettings { get; set; }

        private string _unqiueName;
        public string UniqueName
        {
            get
            {
                if (string.IsNullOrEmpty(_unqiueName))
                    _unqiueName = $"{EnvironmentVariables.Configuration.Name}_{EnvironmentVariables.Configuration.Version}_Install";
                return _unqiueName;
            }
        }
        public CloseProgramsSettings CloseProgramsSettings { get => InstallSettings.CloseProgramsSettings; }
        public DeferSettings DeferSettings { get => InstallSettings.DeferSettings; }
        public RestartSettings RestartSettings { get => InstallSettings.RestartSettings; }
        public LogoffSettings LogoffSettings { get => InstallSettings.LogoffSettings; }
        public CustomActions CustomActions { get => InstallSettings.CustomActions; }

        private Logger _logger = LogManager.GetCurrentClassLogger();

        public IInstallUninstallSequence SubSequence
        {
            get => throw new NotSupportedException();
        }

        public Installer(InstallSettings installSettings)
        {
            InstallSettings = installSettings;
        }

        public abstract void SequenceBegin();

        public abstract void SequenceEnd();
    }
}
