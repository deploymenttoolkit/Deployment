using DeploymentToolkit.Deployment.Settings.Install;
using NLog;
using System;

namespace DeploymentToolkit.Installer.MSI
{
    public class MSIInstaller : Installer
    {
        public override InstallerType InstallerType
        {
            get => InstallerType.MicrosoftInstaller;
        }

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        public MSIInstaller(InstallSettings installSettings) : base(installSettings)
        {
            if (!installSettings.CommandLine.ToLower().EndsWith(".msi"))
                throw new Exception("MSIInstaller can only be used with MSI installation");

            if (InstallSettings.MSISettings.UseDefaultMSIParameters)
            {
                _logger.Info($"MSISettings->UseDefaultMSIParameters specified. Switching Parameters from '{InstallSettings.CommandLine}' to '/qn'");
                InstallSettings.CommandLine = "/qn";
            }

            if (InstallSettings.MSISettings.SupressMSIRestartReturnCode)
                _logger.Info("MSISettings->SupressMSIRestartReturnCode specified. Suppressing restarts after installation");
        }
    }
}
