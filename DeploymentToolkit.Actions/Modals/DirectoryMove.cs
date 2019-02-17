using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "DirectoryMove")]
    public class DirectoryMove : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Source")]
        public string Source { get; set; }
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }
        [XmlAttribute(AttributeName = "Overwrite")]
        public bool Overwrite { get; set; }
        [XmlAttribute(AttributeName = "Recursive")]
        public bool Recursive { get; set; }

        public bool Execute()
        {
            return DirectoryActions.MoveDirectory(Source, Target, Overwrite, Recursive);
        }
    }
}
