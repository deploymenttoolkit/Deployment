using Microsoft.Win32;
using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "RegistrySetValue")]
    public class RegistrySetValue : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Architecture")]
        public Architecture Architecture { get; set; }
        [XmlAttribute(AttributeName = "Path")]
        public string Path { get; set; }
        [XmlAttribute(AttributeName = "ValueName")]
        public string ValueName { get; set; }
        [XmlAttribute(AttributeName = "Value")]
        public string Value { get; set; }
        [XmlAttribute(AttributeName = "Type")]
        public RegistryValueKind Type { get; set; }

        public bool Execute()
        {
            return SetValue(Architecture, Path, ValueName, Value, Type);
        }
    }
}