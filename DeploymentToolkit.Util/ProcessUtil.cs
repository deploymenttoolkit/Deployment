
using DeploymentToolkit.ToolkitEnvironment;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace DeploymentToolkit.Util
{
    public static class ProcessUtil
    {
        private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

        public static void StartTrayAppForAllLoggedOnUsers()
        {
            var trayApps = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(EnvironmentVariables.DeploymentToolkitTrayExeName));
            var trayAppSessions = trayApps.Select((p) => p.SessionId);

            var session = TokenAdjuster.GetLoggedOnUserTokens(trayAppSessions);
            if (session.Count == 0)
            {
                _logger.Info("No session found to spawn Tray App");
                return;
            }

            _logger.Trace($"Trying to start Tray App in {session.Count} sessions ...");
            TokenAdjuster.StartProcessInSessions(session, EnvironmentVariables.DeploymentToolkitTrayExePath, "--startup");
        }
    }
}
