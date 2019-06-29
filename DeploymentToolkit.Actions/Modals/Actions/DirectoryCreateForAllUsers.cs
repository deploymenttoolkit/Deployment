using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "DirectoryCreateForAllUsers")]
    public class DirectoryCreateForAllUsers : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }

        [XmlAttribute(AttributeName = "IncludeDefaultProfile")]
        public bool IncludeDefaultProfile { get; set; }
        [XmlAttribute(AttributeName = "IncludePublicProfile")]
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return DirectoryActions.CreateDirectoryForAllUsers(Target, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}
