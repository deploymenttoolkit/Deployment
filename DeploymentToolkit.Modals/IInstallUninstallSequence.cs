using DeploymentToolkit.Deployment.Settings.Install;

namespace DeploymentToolkit.Modals
{
    public interface IInstallUninstallSequence : ISequence
    {
        CloseProgramsSettings CloseProgramsSettings { get; }
    }
}
