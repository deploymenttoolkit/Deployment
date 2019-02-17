using DeploymentToolkit.Scripting.PreProcessorFunctions;
using System;
using System.Collections.Generic;

namespace DeploymentToolkit.Scripting
{
    public static partial class PreProcessor
    {
        private static readonly Dictionary<string, Func<string[], string>> _functions = new Dictionary<string, Func<string[], string>>()
        {
            {
                "FileExists",
                FileFunctions.Exists
            },
            {
                "DirectoryExists",
                DirectoryFunctions.Exists
            }
        };
    }
}
