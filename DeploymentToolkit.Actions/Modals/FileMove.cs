using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "FileMove")]
    public class FileMove : IExecutableAction
    {
        [XmlAttribute(AttributeName = "Source")]
        public string Source { get; set; }
        [XmlAttribute(AttributeName = "Target")]
        public string Target { get; set; }
        [XmlAttribute(AttributeName = "Overwrite")]
        public bool Overwrite { get; set; }

        public bool Execute()
        {
            return FileActions.MoveFile(Source, Target, Overwrite);
        }
    }
}
