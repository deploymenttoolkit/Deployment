﻿using DeploymentToolkit.Actions.Modals;
using DeploymentToolkit.ToolkitEnvironment;
using DeploymentToolkit.Messaging;
using DeploymentToolkit.Messaging.Events;
using DeploymentToolkit.Messaging.Messages;
using DeploymentToolkit.Modals;
using DeploymentToolkit.Scripting.Exceptions;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DeploymentToolkit.Deployment
{
    public class MainSequence : ISequence
    {
        public IInstallUninstallSequence SubSequence { get => EnvironmentVariables.ActiveSequence; }

        public event EventHandler<SequenceCompletedEventArgs> OnSequenceCompleted;

        private PipeClientManager _pipeClient;
        private Logger _logger = LogManager.GetCurrentClassLogger();

        public MainSequence(IInstallUninstallSequence subSequence)
        {
            _logger.Trace("Sequence initializing...");
            EnvironmentVariables.ActiveSequence = subSequence;
            EnvironmentVariables.ActiveSequenceType = subSequence is Installer.Installer ? SequenceType.Installation : SequenceType.Uninstallation;
            _logger.Trace($"Sequence is {EnvironmentVariables.ActiveSequenceType}");

            _logger.Trace("Setting event...");
            SubSequence.OnSequenceCompleted += OnSubSequenceInstallCompleted;

            _logger.Trace("Sequence initialized");
        }

        public void SequenceBegin()
        {
            _logger.Trace("Sequence started");

            _logger.Trace("Preparing environment...");
            EnvironmentVariables.Initialize();

            if (EnvironmentVariables.ActiveSequence.CustomActions != null)
            {
                _logger.Trace("Processing CustomActions...");
                var actions = EnvironmentVariables.ActiveSequence.CustomActions.Actions;
                if (actions != null && actions.Count > 0)
                {
                    var compiledActions = new List<Actions.Modals.Action>();
                    foreach (var action in actions)
                    {
                        if (string.IsNullOrEmpty(action.Condition))
                            continue;

                        // We do not pre-compile AfterDeployment actions
                        if (action.ExectionOrder == ExectionOrder.AfterDeployment)
                            return;

                        try
                        {
                            _logger.Trace($"Processing {action.Condition}");
                            var preprocessed = Scripting.PreProcessor.Process(action.Condition);
                            _logger.Trace($"Preprocessed: {preprocessed}");
                            action.ConditionResults = Scripting.Evaluation.Evaluate(preprocessed);
                            _logger.Trace($"Result: {action.ConditionResults}");
                            compiledActions.Add(action);
                        }
                        catch (ScriptingException ex)
                        {
                            _logger.Error(ex, "Failed to process CustomAction");
                            _logger.Error("Action will be ignored");
                        }
                    }

                    EnvironmentVariables.ActiveSequence.CustomActions.Actions = compiledActions;
                }
            }

            if (!EnableGUI())
            {
                // In non-GUI mode we just straight start the installation
                StartDeployment();
            }
        }

        public void SequenceEnd()
        {
            _pipeClient?.Dispose();

            SubSequence.SequenceEnd();
        }

        private void OnSubSequenceInstallCompleted(object sender, SequenceCompletedEventArgs e)
        {
            if(e.SequenceSuccessful)
            {
                _logger.Info("Sequence reported a successful install");

                if (EnvironmentVariables.ActiveSequence.DeferSettings != null)
                {
                    // delete deferal settings from registry as we have a successfull installation
                    _logger.Trace("Removing deferal settings from registry ...");
                    RegistryManager.RemoveDeploymentDeferalSettings();
                }

                if(EnvironmentVariables.IsGUIEnabled)
                {
                    // Informing tray apps about successful installation
                    _pipeClient.SendMessage(new BasicMessage(MessageId.DeploymentSuccess));

                    var restartSettings = EnvironmentVariables.ActiveSequence.RestartSettings;
                    var logoffSettings = EnvironmentVariables.ActiveSequence.LogoffSettings;
                    if (restartSettings != null && restartSettings.ForceRestart)
                    {
                        _logger.Trace("Force restart specified. Show restart dialog ...");
                        _pipeClient.SendMessage(new DeploymentRestartMessage()
                        {
                            TimeUntilForceRestart = restartSettings.TimeUntilForcedRestart
                        });

                        return; // Do not perform cleanup or similar
                    }
                    else if (logoffSettings != null && logoffSettings.ForceLogoff)
                    {
                        // TODO: Implement
                    }
                }
            }
            else
            {
                _logger.Error("Sequence reported errors during installation");

                if (e.CountErrors > 0)
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

                if (EnvironmentVariables.IsGUIEnabled)
                {
                    // Informing tray apps about failed installation
                    _pipeClient.SendMessage(new BasicMessage(MessageId.DeploymentError));
                }
            }

            Cleanup();

            OnSequenceCompleted?.Invoke(sender, e);
        }

        private void Cleanup()
        {
            _logger.Trace("Performing cleanup tasks ...");

            var closeProgramSettings = EnvironmentVariables.ActiveSequence.CloseProgramsSettings;
            if (closeProgramSettings != null && closeProgramSettings.DisableStartDuringInstallation && closeProgramSettings.Close.Length > 0)
            {
                try
                {
                    _logger.Info("Unblocking execution of apps ...");
                    ProcessManager.UnblockExecution(closeProgramSettings.Close);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to unblock blocked applications");
                }
            }

            try
            {
                _logger.Trace("Disconnecting from TrayApps...");
                _pipeClient?.Dispose();
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Failed to disconnect from TrayApps");
            }

            _logger.Trace("Successfully performed cleanup task");
        }

        public bool EnableGUI()
        {
            if(!EnvironmentVariables.IsGUIEnabled)
            {
                _logger.Info("GUI mode is not enabled");
                return false;
            }

            _logger.Info("GUI mode is enabled");

            if (!PrepareCommunicationWithTrayApps())
            {
                _logger.Warn("There was an error while trying to communicate with the tray apps");
                _logger.Warn("Switchting to non GUI mode");
                EnvironmentVariables.ForceDisableGUI = true;
                return false;
            }

            if(_pipeClient.ConnectedClients == 0)
            {
                // TODO: We are watching for starts and exits of the tray app. Maybe this is a bad idea to just assume no tray app will ever run during the installation ...
                _logger.Warn("No tray apps running. Can't continue with GUI deployment");
                _logger.Warn("Disabling GUI mode");
                EnvironmentVariables.ForceDisableGUI = true;
                return false;
            }

            // Show deferal window if neccessary
            if (CheckDeferal())
                return true;

            // Show close applications window if neccessary
            if (CheckCloseApplications())
                return true;

            // Otherwise continue with installation
            StartDeployment();
            return true;
        }

        private void StartDeployment()
        {
            Task.Factory.StartNew(delegate ()
            {
                try
                {
                    _logger.Info("Starting deployment ...");

                    if(EnvironmentVariables.IsGUIEnabled)
                    {
                        _logger.Trace("Informing tray apps about installation start");
                        _pipeClient.SendMessage(new BasicMessage(MessageId.DeploymentStarted));
                    }

                    if(EnvironmentVariables.ActiveSequence.CustomActions?.Actions?.Count > 0)
                    {
                        _logger.Trace("Running BeforeDeployment actions ...");
                        var beforeDeploymentActions = EnvironmentVariables.ActiveSequence.CustomActions.Actions.Where((a) => a.ExectionOrder == ExectionOrder.BeforeDeployment && a.ConditionResults).ToList();
                        if (beforeDeploymentActions.Count > 0)
                        {
                            _logger.Trace($"Executing {beforeDeploymentActions.Count} actions ...");
                            foreach(var action in beforeDeploymentActions)
                            {
                                try
                                {
                                    action.ExecuteActions();
                                }
                                catch(Exception ex)
                                {
                                    _logger.Error(ex, "Error during execution of CustomAction");
                                }
                            }
                            _logger.Trace("Execution ended");
                        }
                        else
                            _logger.Trace("No BeforeDeployment actions found");
                    }

                    SubSequence.SequenceBegin();
                }
                catch(Exception ex)
                {
                    if(EnvironmentVariables.IsGUIEnabled)
                    {
                        _logger.Trace("Informing tray apps about installation error");
                        _pipeClient.SendMessage(new BasicMessage(MessageId.DeploymentError));
                    }
                    _logger.Error(ex, "Error during installation");
                }
            }, TaskCreationOptions.LongRunning);
        }

        private bool CheckDeferal()
        {
            var showDeferWindow = true;
            var deferSettings = EnvironmentVariables.ActiveSequence.DeferSettings;

            if (deferSettings == null || (deferSettings.Days <= 0 && deferSettings.DeadlineAsDate == DateTime.MinValue))
            {
                _logger.Trace("No defer settings specified");
                // No defer settings specified so don't show defer window
                showDeferWindow = false;
            }
            else
            {
                // There is a deadline and/or days to install specified
                _logger.Trace("Evaluating defer settings...");
                if (deferSettings.DeadlineAsDate != DateTime.MinValue && deferSettings.DeadlineAsDate < DateTime.Now)
                {
                    _logger.Trace("Deadline reached. Not showing defer window");
                    // Deadline is reached so don't show defer window
                    showDeferWindow = false;
                }
                else if (deferSettings.Days > 0)
                {
                    var remainingDays = RegistryManager.GetDeploymentRemainingDays(EnvironmentVariables.ActiveSequence.UniqueName);
                    _logger.Trace($"{remainingDays} remaining days for user to install {EnvironmentVariables.ActiveSequence.UniqueName}");
                    if (remainingDays <= 0)
                    {
                        _logger.Trace("No days left for the user to install. Not showing defer window");
                        // There are no remaining days so don't show defer window
                        showDeferWindow = false;
                    }
                }
            }

            if (showDeferWindow)
            {
                _logger.Trace("Showing defer window to user(s)");
                var remainingDays = -1;
                if (deferSettings.Days > 0)
                    remainingDays = RegistryManager.GetDeploymentRemainingDays(EnvironmentVariables.ActiveSequence.UniqueName).Value;

                var message = new DeferMessage()
                {
                    DeadLine = deferSettings.DeadlineAsDate,
                    RemainingDays = remainingDays
                };
                _pipeClient.SendMessage(message);
            }

            return showDeferWindow;
        }

        private bool CheckCloseApplications()
        {
            var showCloseApplicationsWindow = true;
            var closeApplicationsSettings = EnvironmentVariables.ActiveSequence.CloseProgramsSettings;

            if(closeApplicationsSettings.Close.Length == 0)
            {
                showCloseApplicationsWindow = false;
                _logger.Trace($"No applications specified to close");
            }
            else
            {
                showCloseApplicationsWindow = ProcessManager.CheckPrograms(closeApplicationsSettings.Close, out var openProcesses);
                _logger.Trace($"Procceses running: {showCloseApplicationsWindow} ({openProcesses.Count}/{closeApplicationsSettings.Close.Length})");
            }

            if(showCloseApplicationsWindow)
            {
                // There seems to be at least one application running on that list. Notify tray app
                _logger.Trace("Showing close applications window to user(s)");
                var message = new CloseApplicationsMessage()
                {
                    ApplicationNames = closeApplicationsSettings.Close,
                    TimeUntilForceClose  = closeApplicationsSettings.TimeUntilForcedClose
                };
                _pipeClient.SendMessage(message);
            }
            else if(closeApplicationsSettings.DisableStartDuringInstallation)
            {
                // None of the apps are currently running so prevent the user from starting them
                _logger.Info("Blocking execution of apps ...");
                ProcessManager.BlockExecution(closeApplicationsSettings.Close);
            }

            return showCloseApplicationsWindow;
        }

        private bool PrepareCommunicationWithTrayApps()
        {
            _logger.Trace("Preparing communication with tray apps...");
            try
            {
                _pipeClient = new PipeClientManager();
                _pipeClient.OnNewMessage += OnNewMessage;
                _logger.Info("Successfully prepared communication with tray apps");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex, "Failed to prepare communication with tray apps");
                return false;
            }
        }

        private void OnNewMessage(object sender, NewMessageEventArgs e)
        {
            _logger.Trace($"Receive message of type {e.MessageId}");

            switch(e.MessageId)
            {
                case MessageId.DeferDeployment:
                    {
                        // User has choosen to defer deployment
                        _logger.Info("User choose to defer deployment. Saving defer state...");
                        RegistryManager.SaveDeploymentDeferalSettings();
                        _logger.Info("Notifying deployment process about deferal");

                        Cleanup();

                        OnSequenceCompleted.Invoke(this, new SequenceCompletedEventArgs()
                        {
                            SequenceSuccessful = true,
                            ReturnCode = 1618 //Fast-Retry -> installation is not shown as failed in sccm for example
                        });
                    }
                    break;

                case MessageId.ContinueDeployment:
                    {
                        var message = e.Message as ContinueMessage;

                        if(message.DeploymentStep == DeploymentStep.Welcome)
                        {
                            _logger.Trace("1: User choose to continue with installation");

                            // Check if deferal settings are specified. If so show that now
                            if (CheckDeferal())
                                return;

                            // Check if close applications settings are specified. If so show that now
                            if (CheckCloseApplications())
                                return;

                            // If no applications are running, then proceed with installation
                            StartDeployment();
                        }
                        else if(message.DeploymentStep == DeploymentStep.DeferDeployment)
                        {
                            // User chose to do the install now
                            _logger.Trace("2: User choose to continue with installation");
                            // Check if close applications settings are specified. If so show that now
                            if (CheckCloseApplications())
                                return;

                            // If no applications are running, then proceed with installation
                            StartDeployment();
                        }
                        else if(message.DeploymentStep == DeploymentStep.CloseApplications)
                        {
                            // User choose to do the install now
                            _logger.Trace("3: User choose to continue with installation");
                            var settings = EnvironmentVariables.ActiveSequence.CloseProgramsSettings;
                            if (settings.DisableStartDuringInstallation)
                            {
                                _logger.Trace("Blocking execution of apps ...");
                                if (!ProcessManager.BlockExecution(settings.Close))
                                    _logger.Warn("Error while trying to block execution of apps");
                                else
                                    _logger.Trace("Successfully blocked execution of apps");
                            }

                            // If no applications are running, then proceed with installation
                            StartDeployment();
                        }
                        else if(message.DeploymentStep == DeploymentStep.Restart)
                        {
                            // User choose to restart (or time ran out whatever)
                            _logger.Trace("4: User choose to restart");

                            Cleanup();

                            OnSequenceCompleted?.Invoke(sender, new SequenceCompletedEventArgs()
                            {
                                ReturnCode = 3010, // Restart return code
                                SequenceSuccessful = true
                            });
                        }
                    }
                    break;
            }
        }
    }
}