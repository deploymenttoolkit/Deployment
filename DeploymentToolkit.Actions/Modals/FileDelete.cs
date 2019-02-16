using DeploymentToolkit.Modals.Actions;
using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "FileDelete")]
    public class FileDelete : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Source")]
        public string Source { get; set; }

        public bool Execute()
        {
            return FileActions.DeleteFile(Source);
        }
    }
}
