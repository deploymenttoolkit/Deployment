using DeploymentToolkit.Actions;
using DeploymentToolkit.Scripting.Extensions;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class FileFunctions
    {
        public static string Exists(string[] parameters)
        {
            if (parameters.Length == 0)
                return false.ToIntString();

            return FileActions.Exists(parameters[0]).ToIntString();
        }
    }
}
