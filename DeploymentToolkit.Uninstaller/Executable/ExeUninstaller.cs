using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings.Uninstall;
using NLog;
using System;
using System.Diagnostics;

namespace DeploymentToolkit.Uninstaller.Executable
{
    public class ExeUninstaller : Uninstaller
    {
        public override UninstallerType UninstallerType => UninstallerType.Executable;

        public override event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Process _uninstallerProcess;

        private readonly string _fileName;
        private readonly string _arguments;

        public ExeUninstaller(UninstallSettings uninstallSettings) : base(uninstallSettings)
        {
            if(uninstallSettings.CommandLine.ToLower().EndsWith(".msi"))
            {
                throw new Exception("ExeUninstaller can only be used with non-MSI installations");
            }

            if(string.IsNullOrEmpty(uninstallSettings.CommandLine))
            {
                throw new Exception($"{nameof(uninstallSettings.CommandLine)} can't be empty!");
            }

            if(string.IsNullOrEmpty(uninstallSettings.Parameters))
            {
                _logger.Warn($"No parameters specified");
            }

            _fileName = uninstallSettings.CommandLine;
            _arguments = uninstallSettings.Parameters;
        }

        public override void SequenceBegin()
        {
            _logger.Trace("Preparing process execution ...");

            _uninstallerProcess = new Process()
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

            _uninstallerProcess.Exited += OnInstallationEnded;
            _uninstallerProcess.Start();

            _logger.Info($"Started EXE uninstallation process. {_uninstallerProcess.Id} in {_uninstallerProcess.SessionId}");
        }

        public override void SequenceEnd()
        {

        }

        private void OnInstallationEnded(object sender, EventArgs e)
        {
            _logger.Info("Uninstallation process ended. Validating ...");

            var exitCode = _uninstallerProcess.ExitCode;
            _logger.Info($"Return code is: {exitCode}");

            var successful = exitCode == 0;
            if(exitCode != 0)
            {
                _logger.Info($"Return code is not 0. Assuming failed uninstallation");
            }
            else
            {
                _logger.Info($"Return code was 0. Assuming successful uninstallation");
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
