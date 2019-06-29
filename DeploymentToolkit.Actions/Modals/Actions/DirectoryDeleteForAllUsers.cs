using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "DirectoryDeleteForAllUsers")]
    public class DirectoryDeleteForAllUsers : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }
        [XmlAttribute(AttributeName = "Recursive")]
        public bool Recursive { get; set; }

        [XmlAttribute(AttributeName = "IncludeDefaultProfile")]
        public bool IncludeDefaultProfile { get; set; }
        [XmlAttribute(AttributeName = "IncludePublicProfile")]
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return DirectoryActions.DeleteDirectoryForAllUsers(Target, Recursive, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}
