using DeploymentToolkit.Actions;
using DeploymentToolkit.Scripting.Extensions;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class DirectoryFunctions
    {
        public static string Exists(string[] parameters)
        {
            if (parameters.Length == 0)
                return false.ToIntString();

            return DirectoryActions.Exists(parameters[0]).ToIntString();
        }
    }
}
