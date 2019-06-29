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

        [XmlIgnore]
        public List<IExecutableAction> Actions { get; set; } = new List<IExecutableAction>();

        [XmlAttribute(AttributeName = "Conditon")]
        public string Condition { get; set; }
        [XmlAttribute(AttributeName = "ExectionOrder")]
        public ExectionOrder ExectionOrder { get; set; }

        [XmlIgnore()]
        public bool ConditionResults { get; set; }

        public void ExecuteActions()
        {
            _logger.Trace($"Starting execution of {Actions.Count} actions ...");

            foreach (var action in Actions)
            {
                _logger.Trace($"Executing {action.GetType().Name} ...");
                try
                {
                    action.Execute();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Exeuction of action failed");
                }
            }

            _logger.Trace($"Execution successfully finished");
        }
    }
}
