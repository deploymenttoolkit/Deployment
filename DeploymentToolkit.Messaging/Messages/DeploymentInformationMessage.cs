using DeploymentToolkit.Modals;

namespace DeploymentToolkit.Messaging.Messages
{
    public class DeploymentInformationMessage : IMessage
    {
        public MessageId MessageId => MessageId.DeploymentInformationMessage;

        public SequenceType SequenceType { get; set; }
        public string DeploymentName { get; set; }
    }
}
