using DeploymentToolkit.Messaging.Events;
using DeploymentToolkit.Messaging.Messages;
using DeploymentToolkit.Modals;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace DeploymentToolkit.Messaging
{
    public class PipeClientManager : IDisposable
    {

        public int ConnectedClients => _clients.Count;
        public event EventHandler<NewMessageEventArgs> OnNewMessage;

        internal const string TrayAppExeName = "DeploymentToolkit.TrayApp.exe";
        internal readonly string TrayAppExeNameLowered;
        internal const string TrayAppExeNameWithoutExtension = "DeploymentToolkit.TrayApp";

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private ManagementEventWatcher _startWatcher;
        private ManagementEventWatcher _stopWatcher;

        private readonly object _collectionLock = new object();
        /// <summary>
        /// Key: ProcessID
        /// Value: PipeClient(Manager)
        /// </summary>
        private Dictionary<int, PipeClient> _clients = new Dictionary<int, PipeClient>();

        private readonly object _messageLock = new object();
        private bool _hasReceivedDeferMessage = false;

        public PipeClientManager()
        {
            TrayAppExeNameLowered = TrayAppExeName.ToLower();

            var processes = Process.GetProcessesByName(TrayAppExeNameWithoutExtension);
            if(processes.Length == 0)
            {
                _logger.Info("There is currently no tray app running on this system");
            }
            else
            {
                _logger.Info($"Found {processes.Length} running instances of {TrayAppExeName}");
                foreach (var process in processes)
                {
                    var clientPipe = new PipeClient(process.Id);
                    if (clientPipe.IsConnected)
                    {
                        _clients.Add(
                            process.Id,
                            clientPipe
                        );

                        clientPipe.OnNewMessage += OnNewMessageReceived;
                    }
                }
                _logger.Info($"Successfully connected to {_clients.Count} tray apps");
            }

            _logger.Info($"Watching for new starts or stopps of {TrayAppExeName}");
            MonitorWMI();
        }

        private void OnNewMessageReceived(object sender, NewMessageEventArgs e)
        {
            if (!(sender is PipeClient client))
            {
                _logger.Warn("Received message from unknown source");
                return;
            }
            
            switch(e.MessageId)
            {
                case MessageId.ContinueDeployment:
                    {
                        var message = e.Message as ContinueMessage;
                        lock(_messageLock)
                        {
                            if(message.DeploymentStep == DeploymentStep.DeferDeployment)
                            {
                                if(_hasReceivedDeferMessage)
                                {
                                    _logger.Trace($"Ignoring answer from session {client.SessionId} as there was already a prior response");
                                    return;
                                }

                                _hasReceivedDeferMessage = true;
                            }
                            else if(message.DeploymentStep == DeploymentStep.CloseApplications)
                            {
                                // TODO
                            }
                            else if(message.DeploymentStep == DeploymentStep.Restart)
                            {
                                // TODO
                            }
                        }

                        OnNewMessage.BeginInvoke(
                            client,
                            e,
                            OnNewMessage.EndInvoke,
                            null
                        );
                    }
                    break;

                case MessageId.DeferDeployment:
                    {
                        lock (_messageLock)
                        {
                            if (_hasReceivedDeferMessage)
                            {
                                _logger.Trace($"Ignoring answer from session {client.SessionId} as there was already a prior response");
                                return;
                            }

                            _hasReceivedDeferMessage = true;
                        }

                        // Notify our installation about the deferal
                        OnNewMessage.BeginInvoke(
                            client,
                            e,
                            OnNewMessage.EndInvoke,
                            null
                        );
                    }
                    break;
            }
        }

        private void MonitorWMI()
        {
            if(_startWatcher != null)
            {
                _startWatcher.Dispose();
                _startWatcher = null;
            }

            _startWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")
            );
            _startWatcher.EventArrived += OnProcessStarted;
            _startWatcher.Start();

            _stopWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace")
            );
            _stopWatcher.EventArrived += OnProcessStopped;
            _stopWatcher.Start();
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            _logger.Trace($"New process started: {processName}");
            if(processName.ToLower() == TrayAppExeNameLowered)
            {
                _logger.Info("New Tray app started. Initiating connection...");
                var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);

                lock (_collectionLock)
                {
                    try
                    {
                        var client = new PipeClient(processId);
                        if(client.IsConnected)
                        {
                            _clients.Add(
                                processId,
                                client
                            );
                        }
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process {processId}");
                    }
                }
            }
        }

        private void OnProcessStopped(object sender, EventArrivedEventArgs e)
        {
            var processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            _logger.Trace($"Process ended: {processId}");
            if (_clients.ContainsKey(processId))
            {
                _logger.Info($"Tray app closed. Disposing pipe");
                lock (_collectionLock)
                {
                    try
                    {
                        _clients[processId].Dispose();
                        _clients.Remove(processId);
                    }
                    catch(Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process {processId}");
                    }
                }
            }
        }

        public void SendMessage(IMessage message)
        {
            _logger.Trace($"Sending {message.MessageId} to {_clients.Count} clients");
            var data = Serializer.SerializeMessage(message);
            lock (_collectionLock)
            {
                foreach (var client in _clients.Values)
                {
                    client.SendMessage(data);
                    _logger.Trace($"Sent message to {client.SessionId}");
                }
            }
        }

        public void Dispose()
        {
            _logger.Trace("Disposing...");

            _logger.Trace("Stopping WMI watchers...");
            _startWatcher?.Stop();
            _startWatcher?.Dispose();
            _stopWatcher?.Stop();
            _stopWatcher?.Dispose();

            _logger.Trace("Stopping clients...");
            foreach(var client in _clients.Values)
            {
                client.Dispose();
            }

            _logger.Trace("Disposed");
        }
    }
}
