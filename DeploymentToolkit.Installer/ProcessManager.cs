using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace DeploymentToolkit.Installer
{
    public partial class Installer
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private string _blockExecutionSubKey = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options";

        public bool CheckPrograms(out List<string> openProcesses)
        {
            openProcesses = new List<string>();

            if (InstallSettings.CloseProgramsSettings.Close.Length == 0)
            {
                _logger.Trace("No executables specified to close");
                return false;
            }

            foreach (var executable in InstallSettings.CloseProgramsSettings.Close)
            {
                var executableName = executable.ToLower();
                if (executable.EndsWith(".exe"))
                    executable.Substring(0, executable.Length - 3);

                _logger.Trace($"Searching for a process named {executable}");
                var processes = Process.GetProcessesByName(executable);
                if (processes.Length > 0)
                {
                    openProcesses.AddRange(
                        processes.Select(process => process.ProcessName)
                    );
                }
            }

            if (openProcesses.Count > 0)
                return true;
            return false;
        }

        public bool ClosePrograms()
        {
            if (InstallSettings.CloseProgramsSettings.Close.Length == 0)
            {
                _logger.Trace("No executables specified to close");
                return true;
            }

            foreach (var executable in InstallSettings.CloseProgramsSettings.Close)
            {
                try
                {
                    var executableName = executable.ToLower();
                    if (executable.EndsWith(".exe"))
                        executable.Substring(0, executable.Length - 3);

                    _logger.Trace($"Searching for a process named {executable}");
                    var processes = Process.GetProcessesByName(executable);
                    if (processes.Length > 0)
                    {
                        foreach (var process in processes)
                        {
                            _logger.Trace($"Trying to close [{process.Id}]{process.ProcessName}");
                            // Send a WM_CLOSE and wait for a gracefull exit
                            PostMessage(process.Handle, 0x0010, IntPtr.Zero, IntPtr.Zero);
                            var exited = process.WaitForExit(5000);
                            if (!exited)
                            {
                                _logger.Trace($"Process did not close after close message. Killing...");
                                process.Kill(); // If it does not exit gracefully then just kill it
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to process {executable}");
                }
            }

            return true;
        }

        public bool BlockExecution()
        {
            if (!InstallSettings.CloseProgramsSettings.DisableStartDuringInstallation)
            {
                _logger.Trace("CloseProgramsSettings->DisableStartDuringInstallation disabled");
                return true;
            }

            if (InstallSettings.CloseProgramsSettings.Close.Length == 0)
            {
                _logger.Trace("No executables specified to block");
                return true;
            }

            try
            {
                _logger.Trace("Opening registry");
                var registry = Registry.LocalMachine.OpenSubKey(_blockExecutionSubKey);
                if (registry == null)
                {
                    _logger.Error("Failed to open registry");
                    return false;
                }

                var subKeys = registry.GetSubKeyNames();
                var subKeysLowered = subKeys.ToList().ConvertAll(key => key.ToLower());

                var executableNames = InstallSettings.CloseProgramsSettings.Close.Distinct();
                foreach (var executable in executableNames)
                {
                    try
                    {
                        var executableName = executable.ToLower();
                        if (!executableName.EndsWith(".exe"))
                            executableName += ".exe";

                        _logger.Trace($"Blocking execution of {executableName}");

                        var subKey = registry.CreateSubKey(executableName);
                        subKey.SetValue("Debugger", ""); // TODO: Add debugger

                        registry.Flush();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process {executable}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to block execution of executables");
                return false;
            }
        }

        public bool UnblockExecution()
        {
            if (!InstallSettings.CloseProgramsSettings.DisableStartDuringInstallation)
            {
                _logger.Trace("CloseProgramsSettings->DisableStartDuringInstallation disabled");
                return true;
            }

            if (InstallSettings.CloseProgramsSettings.Close.Length == 0)
            {
                _logger.Trace("No executables specified to block");
                return true;
            }

            try
            {
                _logger.Trace("Opening registry");
                var registry = Registry.LocalMachine.OpenSubKey(_blockExecutionSubKey);
                if (registry == null)
                {
                    _logger.Error("Failed to open registry");
                    return false;
                }

                var subKeys = registry.GetSubKeyNames();
                var subKeysLowered = subKeys.ToList().ConvertAll(key => key.ToLower());

                var executableNames = InstallSettings.CloseProgramsSettings.Close.Distinct();
                foreach (var executable in executableNames)
                {
                    try
                    {
                        var executableName = executable.ToLower();
                        if (!executableName.EndsWith(".exe"))
                            executableName += ".exe";

                        _logger.Trace($"Unblocking execution of {executableName}");

                        if (subKeysLowered.Contains(executableName))
                        {
                            registry.DeleteSubKeyTree(executableName);
                        }
                        else
                            _logger.Trace($"No key for {executableName} found. Nothing to unblock ... ?");

                        registry.Flush();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, $"Failed to process {executable}");
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to unblock exection of executables");
                return false;
            }
        }
    }
}
