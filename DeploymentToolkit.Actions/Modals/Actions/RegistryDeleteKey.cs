﻿using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    public class RegistryDeleteKey : IExecutableAction
    {
        public Architecture Architecture { get; set; }
        public string Path { get; set; }
        public string KeyName { get; set; }

        public bool Execute()
        {
            return DeleteKey(Architecture, Path, KeyName);
        }
    }
}