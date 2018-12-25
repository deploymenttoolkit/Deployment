using DeploymentToolkit.Deployment.Settings.Install;
using NLog;

namespace DeploymentToolkit.Installer
{
    public abstract partial class Installer
    {
        public abstract InstallerType InstallerType { get; }
        public InstallSettings InstallSettings { get; set; }

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public Installer(InstallSettings installSettings)
        {
            InstallSettings = installSettings;
        }
    }
}
