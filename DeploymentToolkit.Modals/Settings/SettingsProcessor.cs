﻿using DeploymentToolkit.Actions.Modals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace DeploymentToolkit.Modals.Settings
{
    public static class SettingsProcessor
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private static bool _customActionsInitialized = false;
        private static Dictionary<string, ActionInfo> _customActions = new Dictionary<string, ActionInfo>();

        private class ActionInfo
        {
            internal Type Type { get; set; }
            internal Dictionary<string, PropertyInfo> Properties { get; set; }

            internal ActionInfo(Type type, Dictionary<string, PropertyInfo> properties)
            {
                Type = type;
                Properties = properties;
            }
        }

        public static T ReadSettings<T>(string path)
        {
            _logger.Trace($"Reading from {path}");

            var xmlReader = new XmlSerializer(typeof(T));
            xmlReader.UnknownElement += OnUnknownElement;
            var text = File.ReadAllText(path);
            using (var stringReader = new StringReader(text))
            {
                return (T)xmlReader.Deserialize(stringReader);
            }
        }

        private static void OnUnknownElement(object sender, XmlElementEventArgs e)
        {
            if (!_customActionsInitialized)
            {
                InitializeCustomActions();
                _customActionsInitialized = true;
            }

            var element = e.Element;

            if (!_customActions.ContainsKey(element.Name))
            {
                _logger.Warn($"Unknown element in Settings: {element.Name}");
                return;
            }

            var actionInfo = _customActions[element.Name];
            var instance = Activator.CreateInstance(actionInfo.Type);
            var type = instance.GetType();

            foreach (XmlAttribute attribute in element.Attributes)
            {
                if (!actionInfo.Properties.ContainsKey(attribute.Name))
                {
                    _logger.Warn($"{element.Name} does not contain {attribute.Name}. Check docs!");
                    return;
                }

                var propertyInfo = actionInfo.Properties[attribute.Name];
                var value = Convert.ChangeType(attribute.Value, propertyInfo.PropertyType);
                propertyInfo.SetValue(instance, value);
            }

            var parent = (Actions.Modals.Action)e.ObjectBeingDeserialized;
            if (parent == null)
            {
                _logger.Error("Invalid parent!");
                return;
            }

            parent.Actions.Add((IExecutableAction)instance);
            _logger.Debug($"Succcessfully processed {element.Name}");
        }

        private static void InitializeCustomActions()
        {
            _logger.Trace("Reading Actions ...");

            try
            {
                var assembly = Assembly.GetAssembly(typeof(Actions.Modals.Action));
                var actions = assembly
                    .GetTypes()
                    .Where((t) =>
                        t.IsClass &&
                        t.Namespace == "DeploymentToolkit.Actions.Modals.Actions"
                    )
                    .ToList();

                _logger.Trace($"Found {actions.Count} actions. Processing ...");

                foreach (var action in actions)
                {
                    var properties = action.GetProperties().Where((e) => e.CanWrite).ToDictionary((e) => e.Name);
                    _customActions.Add(action.Name, new ActionInfo(action, properties));
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read Actions");
            }
        }
    }
}
