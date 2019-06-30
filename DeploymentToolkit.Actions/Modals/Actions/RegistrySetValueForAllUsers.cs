using Microsoft.Win32;
using System.Xml.Serialization;
using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    [XmlRoot(ElementName = "RegistrySetValueForAllUsers")]
    public class RegistrySetValueForAllUsers : IExecutableAction
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

        [XmlAttribute(AttributeName = "IncludeDefaultProfile")]
        public bool IncludeDefaultProfile { get; set; }
        [XmlAttribute(AttributeName = "IncludePublicProfile")]
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return SetValueForAllUsers(Architecture, Path, ValueName, Value, Type, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}