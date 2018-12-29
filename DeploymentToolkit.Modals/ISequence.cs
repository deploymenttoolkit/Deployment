using System;

namespace DeploymentToolkit.Modals
{
    public interface ISequence
    {
        event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        IInstallUninstallSequence SubSequence { get; }
        void SequenceBegin();
        void SequenceEnd();
    }
}
