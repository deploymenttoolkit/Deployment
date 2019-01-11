using DeploymentToolkit.Deployment.Settings;

namespace DeploymentToolkit.Modals
{
    public interface IInstallUninstallSequence : ISequence
    {
        string UniqueName { get; }

        CloseProgramsSettings CloseProgramsSettings { get; }
        DeferSettings DeferSettings { get; }
    }
}
