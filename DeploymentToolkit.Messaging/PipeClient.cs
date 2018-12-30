using DeploymentToolkit.Messaging.Messages;
using NLog;
using System;
using System.IO;
using System.IO.Pipes;

namespace DeploymentToolkit.Messaging
{
    internal class PipeClient : IDisposable
    {
        internal bool IsConnected = false;

        internal int SessionId;
        internal string Username;
        internal string Domain;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly NamedPipeClientStream _namedPipeClientStream;

        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;


        internal PipeClient(int processId)
        {
            var pipeName = $"DT_{processId}";

            _logger.Info($"Trying to connect to {pipeName}");

            try
            {
                _namedPipeClientStream = new NamedPipeClientStream(pipeName);
                _namedPipeClientStream.Connect(10000);
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

            _reader = new StreamReader(_namedPipeClientStream);
            _writer = new StreamWriter(_namedPipeClientStream)
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
            }
        }

        internal void SendMessage(string data)
        {
            if (!IsConnected)
                return;

            _writer.WriteLine(data);
        }

        public void Dispose()
        {
            IsConnected = false;
            _namedPipeClientStream?.Dispose();
        }
    }
}
