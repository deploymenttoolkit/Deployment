using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "RegistryCreateKeyForAllUsers")]
    public class RegistryCreateKeyForAllUsers : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Architecture")]
        public Architecture Architecture { get; set; }
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
        [XmlAttribute(AttributeName = "KeyName")]
        public string KeyName { get; set; }

        [XmlAttribute(AttributeName = "IncludeDefaultProfile")]
        public bool IncludeDefaultProfile { get; set; }
        [XmlAttribute(AttributeName = "IncludePublicProfile")]
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return CreateKeyForAllUsers(Architecture, Path, KeyName, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}
