using System;

namespace DeploymentToolkit.Messaging.Events
{
    public class NewMessageEventArgs : EventArgs
    {
        public MessageId MessageId { get; set; }
        public IMessage Message { get; set; }
    }
}
