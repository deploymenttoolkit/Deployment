using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DeploymentToolkit.ToolkitEnvironment
{
    public static partial class EnvironmentVariables
    {
        #region DLLImports
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);
        #endregion

        #region Native-Variables
        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        public enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        public enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }
        #endregion

        private static bool? _isAdministrator;
        public static bool IsAdministrator
        {
            get
            {
                if (_isAdministrator.HasValue)
                    return _isAdministrator.Value;

                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                _isAdministrator = principal.IsInRole(WindowsBuiltInRole.Administrator);
                return _isAdministrator.Value;
            }
        }

        private static bool? _isElevated = null;
        public static bool IsElevated
        {
            get
            {
                if (_isElevated.HasValue)
                    return _isElevated.Value;

                try
                {
                    var tokenHandle = IntPtr.Zero;
                    if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_READ, out tokenHandle))
                    {
                        _logger.Error($"Failed to get process token. Win32 error: {Marshal.GetLastWin32Error()}");
                        _isElevated = false;
                        return false;
                    }

                    try
                    {

                        var elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;
                        var resultSize = Marshal.SizeOf((int)elevationResult); //Marshal.SizeOf(typeof(TOKEN_ELEVATION_TYPE));
                        uint returnedSize = 0;

                        var elevationPointer = Marshal.AllocHGlobal(resultSize);
                        try
                        {
                            var success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationPointer, (uint)resultSize, out returnedSize);
                            if (success)
                            {
                                elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationPointer);
                                _isElevated = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                                return _isElevated.Value;
                            }
                            else
                            {
                                _logger.Error($"Failed to get token information Win32 error: {Marshal.GetLastWin32Error()}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, $"Failed to process token. Win32 error: {Marshal.GetLastWin32Error()}");
                        }
                        finally
                        {
                            if (elevationPointer != IntPtr.Zero)
                                Marshal.FreeHGlobal(elevationPointer);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                    }
                    finally
                    {
                        if (tokenHandle != IntPtr.Zero)
                            CloseHandle(tokenHandle);
                    }

                    _isElevated = false;
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to process IsElevated");
                    return false;
                }
            }
        }
    }
}
