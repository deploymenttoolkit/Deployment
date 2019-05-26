﻿using DeploymentToolkit.Actions;
using DeploymentToolkit.Scripting.Extensions;

namespace DeploymentToolkit.Scripting.PreProcessorFunctions
{
    public static class DirectoryFunctions
    {
        public static string Exists(string[] parameters)
        {
            if (parameters.Length == 0)
                return false.ToIntString();

            var target = parameters[0];
            if (string.IsNullOrEmpty(target))
                return false.ToIntString();

            return DirectoryActions.DirectoryExists(target).ToIntString();
        }

        public static string MoveDirectory(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var source = parameters[0];
            if (string.IsNullOrEmpty(source))
                return false.ToIntString();

            var target = parameters[1];
            if (string.IsNullOrEmpty(target))
                return false.ToIntString();

            var overwrite = false;
            var recursive = false;

            if(parameters.Length > 2 && !string.IsNullOrEmpty(parameters[2]))
            {
                bool.TryParse(parameters[2], out overwrite);
            }
            if(parameters.Length > 3 && !string.IsNullOrEmpty(parameters[3]))
            {
                bool.TryParse(parameters[3], out recursive);
            }

            return DirectoryActions.MoveDirectory(source, target, overwrite, recursive).ToIntString();
        }

        public static string CopyDirectory(string[] parameters)
        {
            if (parameters.Length <= 1)
                return false.ToIntString();

            var source = parameters[0];
            if (string.IsNullOrEmpty(source))
                return false.ToIntString();

            var target = parameters[1];
            if (string.IsNullOrEmpty(target))
                return false.ToIntString();

            var overwrite = false;
            var recursive = false;

            if (parameters.Length > 2 && !string.IsNullOrEmpty(parameters[2]))
            {
                bool.TryParse(parameters[2], out overwrite);
            }
            if (parameters.Length > 3 && !string.IsNullOrEmpty(parameters[3]))
            {
                bool.TryParse(parameters[3], out recursive);
            }

            return DirectoryActions.CopyDirectory(source, target, overwrite, recursive).ToIntString();
        }

        public static string DeleteDirectory(string[] parameters)
        {
            if (parameters.Length < 1)
                return false.ToIntString();

            var target = parameters[0];
            if (string.IsNullOrEmpty(target))
                return false.ToIntString();

            var recursive = false;

            if (parameters.Length > 1 && !string.IsNullOrEmpty(parameters[1]))
            {
                bool.TryParse(parameters[1], out recursive);
            }

            return DirectoryActions.DeleteDirectory(target, recursive).ToIntString();
        }

        public static string CreateDirectory(string[] parameters)
        {
            if (parameters.Length < 1)
                return false.ToIntString();

            var target = parameters[0];
            if (string.IsNullOrEmpty(target))
                return false.ToIntString();

            return DirectoryActions.CreateDirectory(target).ToIntString();
        }
    }
}
