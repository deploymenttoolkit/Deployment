using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "RegistryDeleteValue")]
    public class RegistryDeleteValue : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Architecture")]
        public Architecture Architecture { get; set; }
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
        [XmlAttribute(AttributeName = "ValueName")]
        public string ValueName { get; set; }

        public bool Execute()
        {
            return DeleteValue(Architecture, Path, ValueName);
        }
    }
}