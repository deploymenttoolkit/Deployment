using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;

namespace DeploymentToolkit.Util
{
    public sealed class TokenAdjuster
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        private const int GENERIC_ALL_ACCESS = 0x10000000;

        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        public const UInt32 STANDARD_RIGHTS_REQUIRED = 0x000F0000;
        public const UInt32 STANDARD_RIGHTS_READ = 0x00020000;
        public const UInt32 TOKEN_ASSIGN_PRIMARY = 0x0001;
        public const UInt32 TOKEN_DUPLICATE = 0x0002;
        public const UInt32 TOKEN_IMPERSONATE = 0x0004;
        public const UInt32 TOKEN_QUERY = 0x0008;
        public const UInt32 TOKEN_QUERY_SOURCE = 0x0010;
        public const UInt32 TOKEN_ADJUST_PRIVILEGES = 0x0020;
        public const UInt32 TOKEN_ADJUST_GROUPS = 0x0040;
        public const UInt32 TOKEN_ADJUST_DEFAULT = 0x0080;
        public const UInt32 TOKEN_ADJUST_SESSIONID = 0x0100;
        public const UInt32 TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);
        public const UInt32 TOKEN_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_IMPERSONATE | TOKEN_QUERY | TOKEN_QUERY_SOURCE | TOKEN_ADJUST_PRIVILEGES | TOKEN_ADJUST_GROUPS | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID);
        private const int PROCESS_QUERY_INFORMATION = 0X00000400;

        [DllImport("advapi32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern int OpenProcessToken(
            IntPtr ProcessHandle, // handle to process
            UInt32 DesiredAccess, // desired access to process
            ref IntPtr TokenHandle // handle to open access token
        );

        [DllImport("kernel32", SetLastError = true), SuppressUnmanagedCodeSecurity]
        private static extern bool CloseHandle(
            IntPtr handle
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int AdjustTokenPrivileges(
            IntPtr TokenHandle,
            int DisableAllPrivileges,
            IntPtr NewState,
            int BufferLength,
            IntPtr PreviousState,
            ref int ReturnLength
        );

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool LookupPrivilegeValue(
            string lpSystemName,
            string lpName,
            ref LUID lpLuid
        );

        [DllImport("advapi32.dll", EntryPoint = "CreateProcessAsUser", SetLastError = true, CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        internal static extern bool CreateProcessAsUser(
            IntPtr hToken,
            string lpApplicationName,
            string lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandle,
            uint dwCreationFlags,
            IntPtr lpEnvrionment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            ref PROCESS_INFORMATION lpProcessInformation
        );

        [DllImport("advapi32.dll", EntryPoint = "DuplicateTokenEx")]
        internal static extern bool DuplicateTokenEx(
            IntPtr hExistingToken,
            Int32 dwDesiredAccess,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            Int32 ImpersonationLevel,
            Int32 dwTokenType,
            ref IntPtr phNewToken
        );

        [DllImport("userenv.dll", SetLastError = true)]
        static extern bool CreateEnvironmentBlock(
            out IntPtr lpEnvironment,
            IntPtr hToken,
            bool bInherit
        );

        [DllImport("userenv.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DestroyEnvironmentBlock(
            IntPtr lpEnvironment
        );

        internal static bool EnablePrivilege(string privilegeName, bool bEnablePrivilege)
        {
            _logger.Trace($"Trying to enable {privilegeName}");

            int returnLength = 0;
            var token = IntPtr.Zero;
            var tokenPrivileges = new TOKEN_PRIVILEGES
            {
                Privileges = new int[3]
            };
            var oldTokenPrivileges = new TOKEN_PRIVILEGES
            {
                Privileges = new int[3]
            };
            var tLUID = new LUID();
            tokenPrivileges.PrivilegeCount = 1;
            if (bEnablePrivilege)
            {
                tokenPrivileges.Privileges[2] = SE_PRIVILEGE_ENABLED;
            }
            else
            {
                tokenPrivileges.Privileges[2] = 0;
            }

            var unmanagedTokenPrivileges = IntPtr.Zero;
            try
            {
                if (!LookupPrivilegeValue(null, privilegeName, ref tLUID))
                {
                    _logger.Warn($"Failed to Lookup {privilegeName}");
                    return false;
                }

                var process = Process.GetCurrentProcess();
                if (process.Handle == IntPtr.Zero)
                {
                    _logger.Warn($"Failed to get process handle");
                    return false;
                }


                if (OpenProcessToken(process.Handle, TOKEN_ALL_ACCESS, ref token) == 0)
                {
                    _logger.Warn($"Failed to open process token ({Marshal.GetLastWin32Error()})");
                    return false;
                }

                tokenPrivileges.PrivilegeCount = 1;
                tokenPrivileges.Privileges[2] = SE_PRIVILEGE_ENABLED;
                tokenPrivileges.Privileges[1] = tLUID.HighPart;
                tokenPrivileges.Privileges[0] = tLUID.LowPart;
                const int bufLength = 256;
                unmanagedTokenPrivileges = Marshal.AllocHGlobal(bufLength);
                Marshal.StructureToPtr(tokenPrivileges, unmanagedTokenPrivileges, true);
                if (AdjustTokenPrivileges(token, 0, unmanagedTokenPrivileges, bufLength, IntPtr.Zero, ref returnLength) == 0)
                {
                    _logger.Warn($"Failed to adjust privileges ({Marshal.GetLastWin32Error()})");
                    _logger.Warn(new Win32Exception(Marshal.GetLastWin32Error()));
                    return false;
                }

                if (Marshal.GetLastWin32Error() != 0)
                {
                    _logger.Warn($"Failed to adjust privileges ({Marshal.GetLastWin32Error()})");
                    return false;
                }

                _logger.Debug($"Successfully enabled privilege {privilegeName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error while trying to enable {privilegeName}");
                return false;
            }
            finally
            {
                if (token != IntPtr.Zero)
                    CloseHandle(token);
                if (unmanagedTokenPrivileges != IntPtr.Zero)
                    Marshal.FreeHGlobal(unmanagedTokenPrivileges);
            }
        }

        internal static List<IntPtr> GetLoggedOnUserTokens()
        {
            var result = new List<IntPtr>();

            var process = Process.GetProcessesByName("explorer");
            if (process == null || process.Length == 0)
            {
                _logger.Debug("No instances of explorer.exe found. Assuming no logged on users");
                return result;
            }

            var grouped = process.GroupBy((p) => p.SessionId);
            foreach (var group in grouped)
            {
                var sessionId = group.Key;
                var handle = group.First().Handle;
                var token = IntPtr.Zero;

                if (OpenProcessToken(handle, TOKEN_READ | TOKEN_QUERY | TOKEN_DUPLICATE | TOKEN_ASSIGN_PRIMARY, ref token) == 0)
                {
                    _logger.Warn($"Failed to open token from {sessionId} ({Marshal.GetLastWin32Error()}). Skipping ...");
                    continue;
                }

                result.Add(token);
            }

            return result;
        }

        internal static void StartProcessInSessions(List<IntPtr> tokens, string path, string arguments)
        {
            if (tokens == null || tokens.Count == 0)
                throw new ArgumentNullException(nameof(tokens));

            // Adjust token
            _logger.Trace("Adjusting token ...");
            if (!EnablePrivilege("SeAssignPrimaryTokenPrivilege", true))
            {
                _logger.Error("Failed to enable required privilege (SeAssignPrimaryTokenPrivilege)");
                return;
            }

            _logger.Trace($"Trying to start '{path}' with arguments '{arguments}' for {tokens.Count} sessions ...");

            foreach (var token in tokens)
            {
                var duplicatedToken = IntPtr.Zero;
                var environment = IntPtr.Zero;
                var processInformation = new PROCESS_INFORMATION();

                try
                {
                    var securityAttributes = new SECURITY_ATTRIBUTES();
                    securityAttributes.Length = Marshal.SizeOf(securityAttributes);

                    if (!DuplicateTokenEx(
                            token,
                            GENERIC_ALL_ACCESS,
                            ref securityAttributes,
                            (int)SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                            (int)TOKEN_TYPE.TokenPrimary,
                            ref duplicatedToken
                        )
                    )
                    {
                        _logger.Error($"Failed to duplicate token ({Marshal.GetLastWin32Error()})");
                        continue;
                    }

                    if (!CreateEnvironmentBlock(out environment, duplicatedToken, false))
                    {
                        _logger.Error($"Failed to get environment ({Marshal.GetLastWin32Error()})");
                        continue;
                    }

                    var startupInfo = new STARTUPINFO();
                    startupInfo.cb = Marshal.SizeOf(startupInfo);
                    startupInfo.lpDesktop = @"winsta0\default";
                    startupInfo.wShowWindow = 5; // SW_SHOW

                    if (!CreateProcessAsUser(
                            duplicatedToken,
                            path,
                            arguments,
                            ref securityAttributes,
                            ref securityAttributes,
                            false,
                            ProcessCreationFlags.NORMAL_PRIORITY_CLASS | ProcessCreationFlags.CREATE_UNICODE_ENVIRONMENT | ProcessCreationFlags.CREATE_NEW_CONSOLE | ProcessCreationFlags.CREATE_BREAKAWAY_FROM_JOB,
                            environment,
                            Path.GetDirectoryName(path),
                            ref startupInfo,
                            ref processInformation
                        )
                    )
                    {
                        _logger.Error($"Failed to start process ({Marshal.GetLastWin32Error()})");
                        continue;
                    }

                    _logger.Info($"Process started as {processInformation.dwProcessID} ({Marshal.GetLastWin32Error()})");
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Error while trying to start process as user");
                }
                finally
                {
                    if (processInformation.hProcess != IntPtr.Zero)
                        CloseHandle(processInformation.hProcess);
                    if (processInformation.hThread != IntPtr.Zero)
                        CloseHandle(processInformation.hThread);
                    if (duplicatedToken != IntPtr.Zero)
                        CloseHandle(duplicatedToken);
                    if (environment != IntPtr.Zero)
                        DestroyEnvironmentBlock(environment);
                    //if (token != IntPtr.Zero)
                    //    CloseHandle(token);
                }
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct LUID
        {
            internal int LowPart;
            internal int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            private LUID Luid;
            private int Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_PRIVILEGES
        {
            internal int PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            internal int[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct _PRIVILEGE_SET
        {
            private int PrivilegeCount;
            private int Control;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // ANYSIZE_ARRAY = 1
            private LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public Int32 dwProcessID;
            public Int32 dwThreadID;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SECURITY_ATTRIBUTES
        {
            public Int32 Length;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        internal enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        internal enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }
    }
}
