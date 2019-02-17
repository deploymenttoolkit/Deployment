using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "DirectoryCopy")]
    public class DirectoryCopy : IExecutableAction
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
            return DirectoryActions.CopyDirectory(Source, Target, Overwrite, Recursive);
        }
    }
}
