﻿using DeploymentToolkit.Modals.Actions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace DeploymentToolkit.Actions
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

            var parent = (ActionBase)e.ObjectBeingDeserialized;
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
            AddBuiltInActions();
            AddActionsFromExtensions();
        }

        private static void AddBuiltInActions()
        {
            _logger.Trace("Reading built-in Actions ...");
            try
            {
                var actionInterface = typeof(IExecutableAction);
                var assembly = Assembly.GetAssembly(typeof(Modals.Action));
                var actions = assembly
                    .GetTypes()
                    .Where((t) =>
                        t.IsClass &&
                        actionInterface.IsAssignableFrom(t)
                    )
                    .ToList();

                _logger.Debug($"Found {actions.Count} actions. Processing ...");

                foreach (var action in actions)
                {
                    var properties = action.GetProperties().Where((e) => e.CanWrite).ToDictionary((e) => e.Name);
                    _customActions.Add(action.Name, new ActionInfo(action, properties));
                    _logger.Trace($"Added '{action.Name}' with {properties.Count} arguments");
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to read Actions");
            }
        }

        private static void AddActionsFromExtensions()
        {
            _logger.Trace($"Reading Actions from extensions ...");

            try
            {
                var globalExtensionsFiles = Directory.GetFiles(ToolkitEnvironment.EnvironmentVariables.DeploymentToolkitExtensionsPath, "*.dll");
                var deploymentExtensionsFiles = Directory.Exists(DeploymentEnvironment.DeploymentEnvironmentVariables.ExtensionsEnvironment)
                    ? Directory.GetFiles(DeploymentEnvironment.DeploymentEnvironmentVariables.ExtensionsEnvironment, "*.dll")
                    : new string[0];

                var combinedExtensionFiles = globalExtensionsFiles.Concat(deploymentExtensionsFiles).ToArray();

                _logger.Trace($"Found {combinedExtensionFiles.Length} files. Processing files ...");
                var appDomain = AppDomain.CreateDomain("LoaderDomain");
                var tunnelType = typeof(AppDomainTunnel);
                var tunnel = (AppDomainTunnel)appDomain.CreateInstanceAndUnwrap(tunnelType.Assembly.FullName, tunnelType.FullName);

                var validExtensionFiles = tunnel.GetValidFiles(combinedExtensionFiles);

                AppDomain.Unload(appDomain);

                _logger.Trace($"Found {validExtensionFiles.Length} extensions. Loading ...");

                var actionInterface = typeof(IExecutableAction);
                foreach (var extension in validExtensionFiles)
                {
                    try
                    {
                        var assembly = Assembly.LoadFile(extension);
                        var extensionName = assembly.GetName().Name;

                        _logger.Trace($"Loaded extension {extensionName}");

                        var actions = assembly
                            .GetTypes()
                            .Where((t) =>
                                t.IsClass &&
                                actionInterface.IsAssignableFrom(t)
                            )
                            .ToList();

                        foreach (var action in actions)
                        {
                            if (_customActions.ContainsKey(action.Name))
                            {
                                _logger.Warn($"There is already an action named '{action.Name}'. Action not added.");
                                continue;
                            }

                            var properties = action.GetProperties().Where((e) => e.CanWrite).ToDictionary((e) => e.Name);
                            _customActions.Add(action.Name, new ActionInfo(action, properties));
                            _logger.Trace($"[{extensionName}] Added '{action.Name}' with {properties.Count} arguments");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process extension '{extension}'");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load extensions");
            }
        }
    }
}