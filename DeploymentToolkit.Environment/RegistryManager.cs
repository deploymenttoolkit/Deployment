using Microsoft.Win32;
using NLog;
using System;
using System.IO;

namespace DeploymentToolkit.DTEnvironment
{
    public static class RegistryManager
    {
        private const string _deploymentsSavePath = @"SOFTWARE\DeploymentToolkit";

        private static Logger _logger = LogManager.GetCurrentClassLogger();

        private static string _lastDeploymentRegistryKeyPath;

        private static bool GetDeploymentRegistryKey(string deploymentName, out RegistryKey deploymentRegistryKey)
        {
            _lastDeploymentRegistryKeyPath = Path.Combine(_deploymentsSavePath, deploymentName);
            deploymentRegistryKey = Registry.LocalMachine.OpenSubKey(_lastDeploymentRegistryKeyPath);

            if (deploymentRegistryKey == null)
                return false;
            return true;
        }

        public static int? GetDeploymentRemainingDays(string deploymentName)
        {
            if (string.IsNullOrEmpty(deploymentName))
                throw new ArgumentException("Empty string not allowed", nameof(deploymentName));

            if(GetDeploymentRegistryKey(deploymentName, out var registryKey))
            {
                // This isn't the first time we are working with this deployment
                _logger.Trace($"Getting 'DeploymentEndDate' for {deploymentName}");
                var deploymentEndDayString = (string)registryKey.GetValue("DeploymentEndDate", string.Empty);
                if (string.IsNullOrEmpty(deploymentEndDayString) || !DateTime.TryParse(deploymentEndDayString, out var deploymentEndDay))
                {
                    _logger.Warn($"Invalid 'DeploymentEndDate' in registry or 'DeploymentEndDate' not found under {_lastDeploymentRegistryKeyPath}");
                }
                else
                {
                    var remainingDays = (int)Math.Ceiling((deploymentEndDay - DateTime.Now).TotalDays);
                    _logger.Trace($"{remainingDays} remaining days for deployment {deploymentName}");
                    return remainingDays;
                }
            }

            // The current deployment is being run the first time
            if(deploymentName == EnvironmentVariables.ActiveSequence.UniqueName)
            {
                _logger.Trace($"Getting remaining days of current deployment {EnvironmentVariables.ActiveSequence.DeferSettings.Days}");

                return EnvironmentVariables.ActiveSequence.DeferSettings.Days;
            }

            _logger.Warn($"Can't get remaining days for {deploymentName} as it was not found in the registry and isn't the current active deployment");
            return null;
        }

        public static DateTime? GetDeploymentDeadline(string deploymentName)
        {
            if (string.IsNullOrEmpty(deploymentName))
                throw new ArgumentException("Empty string not allowed", nameof(deploymentName));

            if (GetDeploymentRegistryKey(deploymentName, out var registryKey))
            {
                // This isn't the first time we are working with this deployment
                var deploymentDeadlineString = (string)registryKey.GetValue("Deadline", string.Empty);
                if(string.IsNullOrEmpty(deploymentDeadlineString) || !DateTime.TryParse(deploymentDeadlineString, out var deploymentDeadline))
                {
                    _logger.Warn($"Invalid 'Deadline' in registry or 'Deadline' not found under {_lastDeploymentRegistryKeyPath}");
                }
                else
                {
                    return deploymentDeadline;
                }
            }

            // The current deployment is being run the first time
            if (deploymentName == EnvironmentVariables.Configuration.Name)
            {
                _logger.Trace($"Getting deadline of current deployment {EnvironmentVariables.ActiveSequence.DeferSettings.Days}");
                var deadline = EnvironmentVariables.ActiveSequence.DeferSettings.DeadlineAsDate;
                if (deadline == DateTime.MinValue)
                    return null;
                return deadline;
            }

            return null;
        }

        public static void SaveDeploymentDeferalSettings()
        {
            var uniqueName = EnvironmentVariables.ActiveSequence.UniqueName;
            var deferalSettings = EnvironmentVariables.ActiveSequence.DeferSettings;
            var deploymentExists = GetDeploymentRegistryKey(uniqueName, out _);
            var deploymentRegistryPath = _lastDeploymentRegistryKeyPath;

            if (deploymentExists)
            {
                _logger.Trace($"{deploymentRegistryPath} already exists. Deleting...");
                Registry.LocalMachine.DeleteSubKeyTree(deploymentRegistryPath);
            }

            if(deferalSettings.DeadlineAsDate == DateTime.MinValue && deferalSettings.Days <= 0)
            {
                _logger.Trace($"No need to save deferal settings for {uniqueName} as no settings are specified");
                return;
            }

            _logger.Trace($"Creating '{deploymentRegistryPath}' ...");
            var deploymentRegistryKey = Registry.LocalMachine.CreateSubKey(deploymentRegistryPath);

            if (deferalSettings.Days > 0)
            {
                var endDate = DateTime.Now.AddDays(deferalSettings.Days).ToShortDateString();
                _logger.Trace($"Deployment can be defered for {deferalSettings.Days} days. Enddate: {endDate}");
                deploymentRegistryKey.SetValue("DeploymentEndDate", endDate);
            }

            if(deferalSettings.DeadlineAsDate != DateTime.MinValue)
            {
                var deadLine = deferalSettings.Deadline;
                _logger.Trace($"Deployment can be defered until {deadLine}.");
                deploymentRegistryKey.SetValue("Deadline", deadLine);
            }

            _logger.Trace("Successfully saved deferal settings to registry");
        }
    }
}
