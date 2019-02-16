using NLog;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DeploymentToolkit.Actions.Modals
{
    [XmlRoot(ElementName = "Action")]
    public class Action : IOrderedAction
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        [XmlAttribute(AttributeName = "Conditon")]
        public string Condition { get; set; }
        [XmlAttribute(AttributeName = "ExectionOrder")]
        public ExectionOrder ExectionOrder { get; set; }

        [XmlElement(ElementName = "FileMove")]
        public List<FileMove> FileMove { get; set; }
        [XmlElement(ElementName = "FileCopy")]
        public List<FileCopy> FileCopy { get; set; }
        [XmlElement(ElementName = "FileDelete")]
        public List<FileDelete> FileDelete { get; set; }

        [XmlIgnore()]
        public bool ConditionResults { get; set; }

        public void ExecuteActions()
        {
            _logger.Trace($"Executing {FileMove.Count} FileMove actions ...");
            foreach(var action in FileMove)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {FileCopy.Count} FileCopy actions ...");
            foreach (var action in FileCopy)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {FileDelete.Count} FileDelete actions ...");
            foreach (var action in FileDelete)
            {
                action.Execute();
            }

            _logger.Trace($"Execution successfully finished");
        }
    }
}
