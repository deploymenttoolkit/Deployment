using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "DirectoryDelete")]
    public class DirectoryDelete : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }
        [XmlAttribute(AttributeName = "Recursive")]
        public bool Recursive { get; set; }

        public bool Execute()
        {
            return DirectoryActions.DeleteDirectory(Target, Recursive);
        }
    }
}
