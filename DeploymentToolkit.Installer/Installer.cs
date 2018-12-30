﻿using DeploymentToolkit.Deployment.Settings.Install;
using DeploymentToolkit.Modals;
using NLog;
using System;

namespace DeploymentToolkit.Installer
{
    public abstract partial class Installer : IInstallUninstallSequence
    {
        public abstract InstallerType InstallerType { get; }
        public abstract event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        public InstallSettings InstallSettings { get; set; }
        public CloseProgramsSettings CloseProgramsSettings { get => InstallSettings.CloseProgramsSettings; }

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