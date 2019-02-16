using DeploymentToolkit.Actions.Modals;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DeploymentToolkit.Modals
{
    [XmlRoot(ElementName = "CustomActions")]
    public class CustomActions
    {
        [XmlElement(ElementName = "Action")]
        public List<Action> Actions { get; set; }
    }
}
