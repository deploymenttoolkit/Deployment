using DeploymentToolkit.Modals.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DeploymentToolkit.Actions
{
    internal class AppDomainTunnel : MarshalByRefObject
    {
        public string[] GetValidFiles(string[] files)
        {
            var actionType = typeof(IExecutableAction);
            var validFiles = new List<string>();
            foreach (var file in files)
            {
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    var implementsInteface = assembly
                        .GetTypes()
                        .Any(x => x.IsClass && actionType.IsAssignableFrom(x));

                    if (implementsInteface)
                        validFiles.Add(file);
                }
                catch (Exception) { }
            }
            return validFiles.ToArray();
        }
    }
}