using DeploymentToolkit.DeploymentEnvironment;
using NLog;
using System;
using System.IO;

namespace DeploymentToolkit.Modals.Actions
{
    public static class FileActions
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public static bool MoveFile(string source, string target, bool overwrite = false)
        {
            _logger.Trace($"MoveFile({source}, {target}, {overwrite})");
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(target));

            if(!Path.IsPathRooted(source))
            {
                source = Path.Combine(DeploymentEnvironmentVariables.FilesDirectory, source);
                _logger.Trace($"Source path was a non absolute path. Changed path to '{source}'");
            }
            if(!Path.IsPathRooted(target))
            {
                target = Path.GetFullPath(target);
                _logger.Trace($"Target path was a non absolute path. Changed path to '{target}'");
            }

            if(!File.Exists(source))
            {
                _logger.Warn($"Source file not found in '{source}'. Aborting.");
                return false;
            }

            if(!overwrite && File.Exists(target))
            {
                _logger.Info($"Overwrite not specified but target file exists. Not moveing '{source}' to {target}'");
                return true;
            }
            else if(overwrite && File.Exists(target))
            {
                _logger.Info($"Target file exists. Deleting '{target}'");
                File.Delete(target);
            }

            File.Move(source, target);
            return true;
        }

        public static bool CopyFile(string source, string target, bool overwrite = false)
        {
            _logger.Trace($"CopyFile({source}, {target}, {overwrite})");
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(source))
                throw new ArgumentNullException(nameof(target));

            if (!Path.IsPathRooted(source))
            {
                source = Path.Combine(DeploymentEnvironmentVariables.FilesDirectory, source);
                _logger.Trace($"Source path was a non absolute path. Changed path to '{source}'");
            }
            if (!Path.IsPathRooted(target))
            {
                target = Path.GetFullPath(target);
                _logger.Trace($"Target path was a non absolute path. Changed path to '{target}'");
            }

            if (!File.Exists(source))
            {
                _logger.Warn($"Source file not found in '{source}'. Aborting.");
                return false;
            }

            if (!overwrite && File.Exists(target))
            {
                _logger.Info($"Overwrite not specified but target file exists. Not copying '{source}' to {target}'");
                return true;
            }

            File.Copy(source, target, overwrite);
            return true;
        }

        public static bool DeleteFile(string target)
        {
            _logger.Trace($"DeleteFile({target})");
            if (string.IsNullOrEmpty(target))
                throw new ArgumentNullException(nameof(target));

            if (!Path.IsPathRooted(target))
            {
                target = Path.GetFullPath(target);
                _logger.Trace($"Target path was a non absolute path. Changed path to '{target}'");
            }

            if(!File.Exists(target))
            {
                _logger.Info($"No file to delete");
                return true;
            }

            File.Delete(target);
            return true;
        }
    }
}
