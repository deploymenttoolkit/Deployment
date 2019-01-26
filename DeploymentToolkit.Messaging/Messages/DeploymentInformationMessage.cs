namespace DeploymentToolkit.Messaging.Messages
{
    public class DeploymentInformationMessage : IMessage
    {
        public MessageId MessageId => MessageId.DeploymentInformationMessage;

        public string DeploymentName { get; set; }
    }
}
