using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "DirectoryCreate")]
    public class DirectoryCreate : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }

        public bool Execute()
        {
            return DirectoryActions.CreateDirectory(Target);
        }
    }
}
