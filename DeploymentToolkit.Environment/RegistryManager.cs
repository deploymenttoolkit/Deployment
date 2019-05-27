using DeploymentToolkit.Modals;
using DeploymentToolkit.RegistryWrapper;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DeploymentToolkit.ToolkitEnvironment
{
    public static class RegistryManager
    {
        private const string _deploymentsSavePath = @"SOFTWARE\DeploymentToolkit";
        private const string _applicationUninstallPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

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
                var deploymentDeadlineString = (string)registryKey.GetValue("DeploymentDeadline", string.Empty);
                if(string.IsNullOrEmpty(deploymentDeadlineString) || !DateTime.TryParse(deploymentDeadlineString, out var deploymentDeadline))
                {
                    _logger.Warn($"Invalid 'DeploymentDeadline' in registry or 'DeploymentDeadline' not found under {_lastDeploymentRegistryKeyPath}");
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

        public static void RemoveDeploymentDeferalSettings()
        {
            var uniqueName = EnvironmentVariables.ActiveSequence.UniqueName;
            var deploymentExists = GetDeploymentRegistryKey(uniqueName, out _);
            var deploymentRegistryPath = _lastDeploymentRegistryKeyPath;

            if (deploymentExists)
            {
                _logger.Trace($"{deploymentRegistryPath} exists. Deleting...");
                try
                {
                    Registry.LocalMachine.DeleteSubKey(deploymentRegistryPath);
                }
                catch(Exception ex)
                {
                    _logger.Error(ex, $"Error while trying to delete deferal settings for {uniqueName} -> {deploymentRegistryPath}");
                }
            }
            else
            {
                _logger.Trace($"No deferal settings found in registry for deployment {uniqueName}. Nothing to delete ...");
            }
        }

        public static List<UninstallInfo> GetInstalledMSIProgramsByName(string name, bool exact = false)
        {
            var installedPrograms = GetInstalledMSIPrograms();

            if (!exact)
            {
                var result = new List<UninstallInfo>();
                var regex = new Regex(name);
                foreach (var program in installedPrograms)
                {
                    if (regex.Match(program.DisplayName).Success)
                    {
                        _logger.Trace($"Found match: {program.DisplayName}");
                        result.Add(program);
                    }
                }

                return result;
            }
            else
            {
                return installedPrograms.Where((p) => string.Compare(p.DisplayName, name, StringComparison.InvariantCulture) == 0).ToList();
            }
        }

        public static List<UninstallInfo> GetInstalledMSIPrograms()
        {
            var win32Registry = new Win32Registry();
            var keys = win32Registry.GetSubKeys(_applicationUninstallPath);
            var msiPrograms = GetInstallMSIProgramsInHive(win32Registry, keys);

            if (Environment.Is64BitOperatingSystem)
            {
                var win64Registry = new Win64Registry();
                var win64Keys = win64Registry.GetSubKeys(_applicationUninstallPath);
                msiPrograms = msiPrograms.Union(GetInstallMSIProgramsInHive(win64Registry, win64Keys)).ToList();
            }

            return msiPrograms;
        }

        private static List<UninstallInfo> GetInstallMSIProgramsInHive(WinRegistryBase registry, string[] keys)
        {
            var result = new List<UninstallInfo>();

            foreach(var key in keys)
            {
                _logger.Trace($"Processing {key}");

                if(!Guid.TryParse(key, out var productId))
                {
                    _logger.Debug($"{key} could not be parsed as guid. Assuming non MSI installation. Skipping");
                    continue;
                }

                var keyPath = Path.Combine(_applicationUninstallPath, key);
                var program = new UninstallInfo()
                {
                    DisplayName = registry.GetValue(keyPath, "DisplayName"),
                    DisplayVersion = registry.GetValue(keyPath, "DisplayVersion"),
                    Publisher = registry.GetValue(keyPath, "Publisher"),
                    UninstallString = registry.GetValue(keyPath, "UninstallString"),
                    ProductId = $@"{{{productId.ToString()}}}"
                };

                if(!program.UninstallString.ToLower().Contains("msiexec"))
                {
                    _logger.Debug($"{key} does not contain 'msiexec' in UninstallString. Skipping");
                    continue;
                }

                result.Add(program);
            }

            return result;
        }
    }
}
