using DeploymentToolkit.Scripting.PreProcessorFunctions;
using System;
using System.Collections.Generic;

namespace DeploymentToolkit.Scripting
{
    public static partial class PreProcessor
    {
        private static readonly Dictionary<string, Func<string[], string>> _functions = new Dictionary<string, Func<string[], string>>()
        {
            #region FileFunctions
            {
                "FileExists",
                FileFunctions.Exists
            },
            #endregion
            #region DirectoryFunctions
            {
                "DirectoryExists",
                DirectoryFunctions.Exists
            },
            {
                "DirectoryMove",
                DirectoryFunctions.MoveDirectory
            },
            {
                "DirectoryCopy",
                DirectoryFunctions.CopyDirectory
            },
            {
                "DirectoryDelete",
                DirectoryFunctions.DeleteDirectory
            },
            {
                "DirectoryCreate",
                DirectoryFunctions.CreateDirectory
            },
            #endregion
            #region RegistryFunctions
            {
                "RegHasKey",
                RegistryFunctions.HasKey
            },
            {
                "RegCreateKey",
                RegistryFunctions.CreateKey
            },
            {
                "RegDeleteKey",
                RegistryFunctions.DeleteKey
            },
            {
                "RegHasValue",
                RegistryFunctions.HasValue
            },
            {
                "RegGetValue",
                RegistryFunctions.GetValue
            },
            {
                "RegSetValue",
                RegistryFunctions.SetValue
            },
            {
                "RegDeleteValue",
                RegistryFunctions.DeleteValue
            }
            #endregion
        };
    }
}
