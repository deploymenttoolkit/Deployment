using DeploymentToolkit.Deployment.Settings;
using DeploymentToolkit.Deployment.Settings.Install;

namespace DeploymentToolkit.Modals
{
    public interface IInstallUninstallSequence : ISequence
    {
        string UniqueName { get; }

        CloseProgramsSettings CloseProgramsSettings { get; }
        DeferSettings DeferSettings { get; }
        RestartSettings RestartSettings { get; }
        LogoffSettings LogoffSettings { get; }
    }
}
