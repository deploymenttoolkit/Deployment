using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings.Install;
using NLog;
using System;
using System.Diagnostics;

namespace DeploymentToolkit.Installer.Executable
{
    public class ExeInstaller : Installer
    {
        public override InstallerType InstallerType => InstallerType.Executable;

        public override event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Process _installerProcess;

        private readonly string _fileName;
        private readonly string _arguments;

        public ExeInstaller(InstallSettings installSettings) : base(installSettings)
        {
            if(installSettings.CommandLine.ToLower().EndsWith(".msi"))
            {
                throw new Exception("ExeInstaller can only be used with non-MSI installations");
            }

            if(string.IsNullOrEmpty(installSettings.CommandLine))
            {
                throw new Exception($"{nameof(installSettings.CommandLine)} can't be empty!");
            }

            if(string.IsNullOrEmpty(installSettings.Parameters))
            {
                _logger.Warn($"No parameters specified");
            }

            _fileName = installSettings.CommandLine;
            _arguments = installSettings.Parameters;
        }

        public override void SequenceBegin()
        {
            _logger.Trace("Preparing process execution ...");

            _installerProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = _fileName,
                    Arguments = _arguments,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                },
                EnableRaisingEvents = true
            };

            _installerProcess.Exited += OnInstallationEnded;
            _installerProcess.Start();

            _logger.Info($"Started EXE installation process. {_installerProcess.Id} in {_installerProcess.SessionId}");
        }

        public override void SequenceEnd()
        {

        }

        private void OnInstallationEnded(object sender, EventArgs e)
        {
            _logger.Info("Installation process ended. Validating ...");

            var exitCode = _installerProcess.ExitCode;
            _logger.Info($"Return code is: {exitCode}");

            var successful = exitCode == 0;
            if(exitCode != 0)
            {
                _logger.Info($"Return code is not 0. Assuming failed installation");
            }
            else
            {
                _logger.Info($"Return code was 0. Assuming successful installation");
            }

            BeforeSequenceComplete(successful);

            OnSequenceCompleted?.BeginInvoke(
                this,
                new SequenceCompletedEventArgs()
                {
                    ReturnCode = exitCode,
                    SequenceSuccessful = successful
                },
                OnSequenceCompleted.EndInvoke,
                null
            );
        }
    }
}
