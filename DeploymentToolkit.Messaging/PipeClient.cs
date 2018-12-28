using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace DeploymentToolkit.Messaging
{
    public class PipeClient : IDisposable
    {
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
        private Dictionary<int, PipeClientManager> _clients = new Dictionary<int, PipeClientManager>();

        public PipeClient()
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
                    var clientPipe = new PipeClientManager(process.Id);
                    if (clientPipe.IsConnected)
                    {
                        _clients.Add(
                            process.Id,
                            clientPipe
                        );
                    }
                }
                _logger.Info($"Successfully connected to {_clients.Count} tray apps");
            }

            _logger.Info($"Watching for new starts or stopps of {TrayAppExeName}");
            MonitorWMI();
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
            _startWatcher.Stop();
        }

        private void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            var processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if(processName.ToLower() == TrayAppExeNameLowered)
            {
                _logger.Info("New Tray app started. Initiating connection...");
                var processId = unchecked((int)e.NewEvent.Properties["ProcessID"].Value);

                lock (_collectionLock)
                {
                    try
                    {
                        var client = new PipeClientManager(processId);
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
            var processId = unchecked((int)e.NewEvent.Properties["ProcessID"].Value);
            if(_clients.ContainsKey(processId))
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
            var data = Serializer.SerializeMessage(message);
            lock (_collectionLock)
            {
                foreach (var client in _clients.Values)
                {
                    client.SendMessage(data);
                }
            }
        }

        public void Dispose()
        {
            _logger.Trace("Disposing...");

            _logger.Trace("Stopping WMI watchers...");
            _startWatcher?.Dispose();
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
