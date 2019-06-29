using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals.Actions
{

    [XmlRoot(ElementName = "FileDeleteForAllUsers")]
    public class FileDeleteForAllUsers : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }

        [XmlAttribute(AttributeName = "IncludeDefaultProfile")]
        public bool IncludeDefaultProfile { get; set; }
        [XmlAttribute(AttributeName = "IncludePublicProfile")]
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return FileActions.DeleteFileForAllUsers(Target, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}
