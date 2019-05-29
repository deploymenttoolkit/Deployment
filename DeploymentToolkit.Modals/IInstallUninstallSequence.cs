using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;

namespace DeploymentToolkit.Modals
{
    public interface IInstallUninstallSequence : ISequence
    {
        string UniqueName { get; }

        CloseProgramsSettings CloseProgramsSettings { get; }
        DeferSettings DeferSettings { get; }
        RestartSettings RestartSettings { get; }
        LogoffSettings LogoffSettings { get; }
        CustomActions CustomActions { get; }

        void BeforeSequenceBegin();
        void BeforeSequenceComplete(bool success);
    }
}
