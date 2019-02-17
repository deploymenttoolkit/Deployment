using DeploymentToolkit.Modals.Actions;
using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
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
