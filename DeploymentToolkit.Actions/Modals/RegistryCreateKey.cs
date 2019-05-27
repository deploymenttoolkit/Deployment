using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "RegistryCreateKey")]
    public class RegistryCreateKey : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Architecture")]
        public Architecture Architecture { get; set; }
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
        [XmlAttribute(AttributeName = "KeyName")]
        public string KeyName { get; set; }

        public bool Execute()
        {
            return CreateKey(Architecture, Path, KeyName);
        }
    }
}
