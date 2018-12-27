using System;

namespace DeploymentToolkit.Modals
{
    public interface ISequence
    {
        event EventHandler<InstallCompletedEventArgs> OnInstallCompleted;

        IInstallUninstallSequence SubSequence { get; }
        void SequenceBegin();
        void SequenceEnd();
    }
}
