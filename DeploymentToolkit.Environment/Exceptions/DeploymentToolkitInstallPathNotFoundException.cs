using System.IO;

namespace DeploymentToolkit.DTEnvironment.Exceptions
{
    public class DeploymentToolkitInstallPathNotFoundException : FileNotFoundException
    {
        public DeploymentToolkitInstallPathNotFoundException(string message, string fileName) : base(message, fileName)
        {
        }
    }
}
