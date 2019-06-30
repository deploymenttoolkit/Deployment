using static DeploymentToolkit.Actions.RegistryActions;

namespace DeploymentToolkit.Actions.Modals.Actions
{
    public class RegistryDeleteValueForAllUsers : IExecutableAction
    {
        public Architecture Architecture { get; set; }
        public string Path { get; set; }
        public string ValueName { get; set; }

        public bool IncludeDefaultProfile { get; set; }
        public bool IncludePublicProfile { get; set; }

        public bool Execute()
        {
            return DeleteValueForAllUsers(Architecture, Path, ValueName, IncludeDefaultProfile, IncludePublicProfile);
        }
    }
}