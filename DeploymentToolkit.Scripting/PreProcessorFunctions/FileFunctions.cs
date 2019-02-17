using DeploymentToolkit.Scripting.Extensions;
using System.IO;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class FileFunctions
    {
        public static string Exists(string[] parameters)
        {
            if (parameters.Length == 0)
                return false.ToIntString();

            var path = parameters[0];
            if (!Path.IsPathRooted(path))
                path = Path.GetFullPath(path);
            return File.Exists(path).ToIntString();
        }
    }
}
