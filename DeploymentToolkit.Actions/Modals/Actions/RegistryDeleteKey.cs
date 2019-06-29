using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "RegistryDeleteKey")]
    public class RegistryDeleteKey : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Architecture")]
        public Architecture Architecture { get; set; }
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
        [XmlAttribute(AttributeName = "KeyName")]
        public string KeyName { get; set; }

        public bool Execute()
        {
            return DeleteKey(Architecture, Path, KeyName);
        }
    }
}