using DeploymentToolkit.DTEnvironment;
using DeploymentToolkit.Messaging.Events;
using DeploymentToolkit.Messaging.Messages;
using NLog;
using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;

namespace DeploymentToolkit.Messaging
{
    internal class PipeClient : IDisposable
    {
        internal event EventHandler<NewMessageEventArgs> OnNewMessage;

        public bool IsConnected = false;

        internal int SessionId;
        internal string Username;
        internal string Domain;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly NamedPipeClientStream _receiverPipe;
        private readonly NamedPipeClientStream _senderPipe;

        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        private readonly Thread _backgroundWorker;


        internal PipeClient(int processId)
        {
            var pipeName = $"DT_{processId}";

            _logger.Info($"Trying to connect to {pipeName}");

            try
            {
                _receiverPipe = new NamedPipeClientStream($"{pipeName}_In");
                _receiverPipe.Connect(10000);
                _receiverPipe.ReadMode = PipeTransmissionMode.Message;

                _senderPipe = new NamedPipeClientStream($"{pipeName}_Out");
                _senderPipe.Connect(10000);

                IsConnected = true;

                _logger.Info($"Successfuly connected to {pipeName}");
            }
            catch (TimeoutException)
            {
                _logger.Warn($"Connect to {pipeName} failed. Timeout after 10 seconds");
                Dispose();
                return;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to connec to {pipeName}");
                Dispose();
                return;
            }

            _reader = new StreamReader(_receiverPipe);
            _writer = new StreamWriter(_senderPipe)
            {
                AutoFlush = true
            };

            var data = _reader.ReadLine();
            var connectMessage = Serializer.DeserializeMessage<InitialConnectMessage>(data);
            if(connectMessage == null)
            {
                _logger.Error("Inital connect message was null!");
            }
            else
            {
                this.SessionId = connectMessage.SessionId;
                this.Username = connectMessage.Username;
                this.Domain = connectMessage.Domain;

                _logger.Info($"Connected to {Username}@{Domain} on session {SessionId}");

                _logger.Trace("Sending DeploymentInformationMessage ...");
                var message = new DeploymentInformationMessage()
                {
                    SequenceType = EnvironmentVariables.ActiveSequenceType,
                    DeploymentName = EnvironmentVariables.Configuration.Name
                };
                var messageData = Serializer.SerializeMessage(message);
                SendMessage(messageData);

                _logger.Trace("Starting background worker ...");
                _backgroundWorker = new Thread(delegate()
                {
                    Receive();
                });
                _backgroundWorker.Start();
            }
        }

        internal void SendMessage(string data)
        {
            if (!IsConnected)
            {
                _logger.Warn("Tried to send a message to a non-connected client!");
                return;
            }

            _writer.WriteLine(data);
            _writer.Flush();
        }

        private async void Receive()
        {
            try
            {
                do
                {
                    _logger.Trace("Waiting for new messages ...");
                    var data = await _reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(data))
                        continue;
                    _logger.Trace($"Received message from tray app ({data})");

                    try
                    {
                        var basicMessage = Serializer.DeserializeMessage<BasicMessage>(data);
                        _logger.Trace($"Received {basicMessage.MessageId}");
                        switch (basicMessage.MessageId)
                        {
                            case MessageId.DeferDeployment:
                                {
                                    var message = Serializer.DeserializeMessage<CloseApplicationsMessage>(data);
                                    OnNewMessage?.BeginInvoke(
                                        this,
                                        new NewMessageEventArgs()
                                        {
                                            MessageId = basicMessage.MessageId,
                                            Message = message
                                        },
                                        OnNewMessage.EndInvoke,
                                        null
                                    );
                                }
                                break;

                            case MessageId.ContinueDeployment:
                                {
                                    var message = Serializer.DeserializeMessage<ContinueMessage>(data);
                                    OnNewMessage?.BeginInvoke(
                                        this,
                                        new NewMessageEventArgs()
                                        {
                                            MessageId = basicMessage.MessageId,
                                            Message = message
                                        },
                                        OnNewMessage.EndInvoke,
                                        null
                                    );
                                }
                                break;

                            default:
                                {
                                    _logger.Warn($"Unhandeld message of type {basicMessage.MessageId}");
                                }
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to parse message");
                    }
                }
                while (_receiverPipe.IsConnected);
            }
            catch (ThreadAbortException) { }
            catch(Exception ex)
            {
                _logger.Error(ex, "Error while receiving messages");
            }
        }

        public void Dispose()
        {
            IsConnected = false;
            _receiverPipe?.Dispose();
            _senderPipe?.Dispose();
            _backgroundWorker?.Abort();
        }
    }
}
