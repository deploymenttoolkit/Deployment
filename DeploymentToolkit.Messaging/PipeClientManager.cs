using NLog;
using System;
using System.IO;
using System.IO.Pipes;

namespace DeploymentToolkit.Messaging
{
    internal class PipeClientManager : IDisposable
    {
        internal bool IsConnected = false;

        private Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly NamedPipeClientStream _namedPipeClientStream;

        private readonly StreamReader _reader;
        private readonly StreamWriter _writer;

        internal PipeClientManager(int processId)
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
