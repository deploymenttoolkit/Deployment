using DeploymentToolkit.Environment;
using DeploymentToolkit.Messaging;
using DeploymentToolkit.Messaging.Messages;
using DeploymentToolkit.Modals;
using NLog;
using System;

namespace DeploymentToolkit.Deployment
{
    internal class MainSequence : ISequence
    {
        public IInstallUninstallSequence SubSequence { get; }

        public event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private PipeClient _pipeClient;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public MainSequence(IInstallUninstallSequence subSequence)
        {
            _logger.Trace("Sequence initializing...");
            SubSequence = subSequence;
            _logger.Trace($"Sequence is {(subSequence is Installer.Installer ? "Install" : "Uninstall")}");

            _logger.Trace("Setting event...");
            SubSequence.OnSequenceCompleted += OnSubSequenceInstallCompleted;

            _logger.Trace("Preparing environment...");
            CheckRunningInTaskSequence();

            if (DTEnvironment.GUIEnabled)
            {
                _logger.Info("GUI mode is enabled");
                PrepareCommunicationWithTrayApps();
            }

            _logger.Trace("Sequence initiated");
        }

        public void SequenceBegin()
        {
            if(DTEnvironment.GUIEnabled)
            {
                var programsToClose = SubSequence.CloseProgramsSettings.Close;
                if(programsToClose != null && programsToClose.Length > 0)
                {
                    var message = new CloseApplicationsMessage()
                    {
                        ApplicationNames = programsToClose
                    };
                    _pipeClient.SendMessage(message);
                }
#if DEBUG //&& DEBUG_GUI
                _logger.Trace("GUI DEBUG PAUSE 5000");
                System.Threading.Thread.Sleep(5000);
#endif
            }

            try
            {
                SubSequence.SequenceBegin();
            }
            catch(Exception ex)
            {
                _logger.Fatal(ex, "Fatal error during execution of install/uninstall sequence");
            }

            _pipeClient.Dispose();
        }

        public void SequenceEnd()
        {
            SubSequence.SequenceEnd();
        }

        private void OnSubSequenceInstallCompleted(object sender, SequenceCompletedEventArgs e)
        {
            if(e.InstallSuccessful)
            {
                _logger.Info("Sequence reported a successful install");
            }
            else
            {
                _logger.Error("Sequence reported errors during installation");
            }

            if(e.CountErrors > 0)
            {
                _logger.Error("=================================================");
                _logger.Error($"{e.CountErrors} error reported");
                foreach (var error in e.SequenceErrors)
                {
                    _logger.Error("=================================================");
                    _logger.Error(error.Message);
                    _logger.Error(error.StackTrace);
                }
                _logger.Error("=================================================");
            }

            if (e.CountWarnings > 0)
            {
                _logger.Warn("=================================================");
                _logger.Warn($"{e.CountWarnings} error reported");
                foreach (var warnings in e.SequenceWarnings)
                {
                    _logger.Warn("=================================================");
                    _logger.Warn(warnings.Message);
                    _logger.Warn(warnings.StackTrace);
                }
                _logger.Warn("=================================================");
            }

            OnSequenceCompleted?.Invoke(sender, e);
        }

        private void PrepareCommunicationWithTrayApps()
        {
            _logger.Trace("Preparing communication with tray apps...");
            try
            {
                _pipeClient = new PipeClient();
                _logger.Info("Successfully prepared communication with tray apps");
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed to prepare communication with tray apps");
            }
        }

        private void CheckRunningInTaskSequence()
        {
            _logger.Trace("Checking if running in TaskSequence...");

            var tsEnvironment = Type.GetTypeFromProgID("Microsoft.SMS.TSEnvironment");
            if (tsEnvironment == null)
            {
                _logger.Info("Couldn't load 'Microsoft.SMS.TSEnvironment' therefore we are not in a task sequence");
                return;
            }

            _logger.Info("Successfully loaded 'Microsoft.SMS.TSEnvironment'. Task sequence mode enabled");

            DTEnvironment.IsRunningInTaskSequence = true;
        }
    }
}