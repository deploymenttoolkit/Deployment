using NLog;
using System;
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
        [XmlArray(ElementName = "FileMove")]
        public List<FileMove> FileMove { get; set; }
        [XmlArray(ElementName = "FileCopy")]
        public List<FileCopy> FileCopy { get; set; }
        [XmlArray(ElementName = "FileDelete")]
        public List<FileDelete> FileDelete { get; set; }
        #endregion
        #region Directories
        [XmlArray(ElementName = "DirectoryMove")]
        public List<DirectoryMove> DirectoryMove { get; set; }
        [XmlArray(ElementName = "DirectoryCopy")]
        public List<DirectoryCopy> DirectoryCopy { get; set; }
        [XmlArray(ElementName = "DirectoryDelete")]
        public List<DirectoryDelete> DirectoryDelete { get; set; }
        [XmlArray(ElementName = "DirectoryCreate")]
        public List<DirectoryCreate> DirectoryCreate { get; set; }
        #endregion
        #region Registry
        [XmlArray(ElementName = "RegistryCreateKey")]
        public List<RegistryCreateKey> RegistryCreateKey { get; set; }
        [XmlArray(ElementName = "RegistryDeleteKey")]
        public List<RegistryDeleteKey> RegistryDeleteKey { get; set; }
        [XmlArray(ElementName = "RegistryDeleteValue")]
        public List<RegistryDeleteValue> RegistryDeleteValue { get; set; }
        [XmlArray(ElementName = "RegistrySetValue")]
        public List<RegistrySetValue> RegistrySetValue { get; set; }
        #endregion 
        [XmlIgnore()]
        public bool ConditionResults { get; set; }

        public void ExecuteActions()
        {
            ExecuteFileActions();

            ExecuteDirectoryActions();

            ExecuteRegistryActions();

            _logger.Trace($"Execution successfully finished");
        }

        private void ExecuteFileActions()
        {
            _logger.Trace($"Executing {FileMove.Count} FileMove actions ...");
            foreach (var action in FileMove)
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
        }

        private void ExecuteDirectoryActions()
        {
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
        }

        private void ExecuteRegistryActions()
        {
            _logger.Trace($"Executing {RegistryCreateKey.Count} RegistryCreateKey actions ...");
            foreach (var action in RegistryCreateKey)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {RegistryDeleteKey.Count} RegistryDeleteKey actions ...");
            foreach (var action in RegistryDeleteKey)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {RegistryDeleteValue.Count} RegistryDeleteValue actions ...");
            foreach (var action in RegistryDeleteValue)
            {
                action.Execute();
            }

            _logger.Trace($"Executing {RegistrySetValue.Count} RegistrySetValue actions ...");
            foreach (var action in RegistrySetValue)
            {
                action.Execute();
            }
        }
    }
}
