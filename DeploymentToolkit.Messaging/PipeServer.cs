using DeploymentToolkit.Messaging.Events;
using DeploymentToolkit.Messaging.Messages;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace DeploymentToolkit.Messaging
{
    public class PipeServer : IDisposable
    {
        public event EventHandler<NewMessageEventArgs> OnNewMessage;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private CancellationTokenSource _cancellationToken = new CancellationTokenSource();
        private NamedPipeServerStream _namedPipeServerStream;

        private StreamReader _reader;
        private StreamWriter _writer;

        private readonly NotifyIcon _notifyIcon;

        public PipeServer(NotifyIcon notifyIcon)
        {
            _notifyIcon = notifyIcon;
            
            var process = Process.GetCurrentProcess();
            var processId = process.Id;

            var pipeName = $"DT_{processId}";

            try
            { 
                _namedPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
                WaitForClient();
            }
            catch(Exception ex)
            {
                _logger.Fatal(ex, "Failed to create pipe server");
                return;
            }

            _logger.Info($"Initialized pipe server ({pipeName})");
        }

        public void Dispose()
        {
            if (!_cancellationToken.IsCancellationRequested)
                _cancellationToken.Cancel();
            _namedPipeServerStream?.Dispose();
        }

        public async void WaitForClient()
        {
            do
            {
                try
                {
                    await _namedPipeServerStream.WaitForConnectionAsync(_cancellationToken.Token);
                }
                catch (IOException ex)
                {
                    _namedPipeServerStream.Disconnect();
                    _logger.Info(ex, "Disconnecting pipe");
                }
            }
            while (!_cancellationToken.IsCancellationRequested && !_namedPipeServerStream.IsConnected);
            _logger.Info($"Client connected");

            _reader = new StreamReader(_namedPipeServerStream);
            _writer = new StreamWriter(_namedPipeServerStream)
            {
                AutoFlush = true
            };

            ReadMessages();
        }

        private async void ReadMessages()
        {
            _logger.Info("Reading messages");
            do
            {
                var message = await _reader.ReadLineAsync();
                if (string.IsNullOrEmpty(message))
                    continue;

                ProcessMessage(message);
            }
            while (_namedPipeServerStream.IsConnected);
            _logger.Info("Client disconnected");
            _namedPipeServerStream.Disconnect();
            WaitForClient();
        }

        private void ProcessMessage(string data)
        {
            _logger.Trace($"Starting processing message ({data})");
            try
            {
                var simpleMessage = Serializer.DeserializeMessage<BasicMessage>(data);
                if (simpleMessage == null)
                    return;

                _logger.Info($"Received new message {simpleMessage.MessageId}");

                

                switch (simpleMessage.MessageId)
                {
                    case MessageId.CloseApplications:
                        {
                            var message = Serializer.DeserializeMessage<CloseApplicationsMessage>(data);
                            OnNewMessage?.BeginInvoke(
                                this,
                                new NewMessageEventArgs()
                                {
                                    MessageId = simpleMessage.MessageId,
                                    Message = message
                                },
                                OnNewMessage.EndInvoke,
                                null
                            );
                        }
                        break;

                    default:
                        {
                            _logger.Warn($"Unknown message type: {simpleMessage.MessageId}");
                        }
                        break;
                }
            }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error processing message");
            }
        }
    }
}
