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

        #region Files
        [XmlElement(ElementName = "FileMove")]
        public List<FileMove> FileMove { get; set; }
        [XmlElement(ElementName = "FileCopy")]
        public List<FileCopy> FileCopy { get; set; }
        [XmlElement(ElementName = "FileDelete")]
        public List<FileDelete> FileDelete { get; set; }
        #endregion
        #region Directories
        [XmlElement(ElementName = "DirectoryMove")]
        public List<DirectoryMove> DirectoryMove { get; set; }
        [XmlElement(ElementName = "DirectoryCopy")]
        public List<DirectoryCopy> DirectoryCopy { get; set; }
        [XmlElement(ElementName = "DirectoryDelete")]
        public List<DirectoryDelete> DirectoryDelete { get; set; }
        [XmlElement(ElementName = "DirectoryCreate")]
        public List<DirectoryCreate> DirectoryCreate { get; set; }
        #endregion
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

            _logger.Trace($"Executing {DirectoryMove.Count} DirectoryMove actions ...");
            foreach (var action in DirectoryMove)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {DirectoryCopy.Count} DirectoryCopy actions ...");
            foreach (var action in DirectoryCopy)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {DirectoryDelete.Count} DirectoryDelete actions ...");
            foreach (var action in DirectoryDelete)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {DirectoryCreate.Count} DirectoryCreate actions ...");
            foreach (var action in DirectoryCreate)
            {
                action.Execute();
            }

            _logger.Trace($"Execution successfully finished");
        }
    }
}
