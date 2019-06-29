using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "FileDelete")]
    public class FileDelete : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }

        public bool Execute()
        {
            return FileActions.DeleteFile(Target);
        }
    }
}
