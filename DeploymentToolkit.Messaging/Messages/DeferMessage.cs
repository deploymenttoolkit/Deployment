namespace DeploymentToolkit.Messaging.Messages
{
    public class DeferMessage : IMessage
    {
        public MessageId MessageId => MessageId.DeferDeployment;
    }
}
