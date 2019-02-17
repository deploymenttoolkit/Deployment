using DeploymentToolkit.Scripting.Extensions;
using System.IO;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class DirectoryFunctions
    {
        public static string Exists(string[] parameters)
        {
            if (parameters.Length == 0)
                return false.ToIntString();

            var path = parameters[0];
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(path);
            return Directory.Exists(path).ToIntString();
        }
    }
}
