namespace DeploymentToolkit.Messaging
{
    public enum MessageId : byte
    {
        // Client = TrayApp
        // Server = DeploymentToolkit executable
        // IFEObl = Image File Execution Options Blocker

        // Server <-  Client
        InitialConnectMessage,
        // Server  -> Client
        DeploymentInformationMessage,

        // Server  -> Client
        InstallationStarted,
        // Server  -> Client
        InstallationEnded,
        // Server  -> Client
        InstallationError,

        // Server  -> Client
        UninstallationStarted,
        // Server  -> Client
        UninstallationEnded,
        // Server  -> Client
        UninstallationError,

        // IFEObl  -> Server
        // Server  -> Client
        ExecutionBlocked,

        // Server  -> Client
        ShowBalloonTip,

        // Server <-  Client
        ContinueDeployment,

        // Server  -> Client
        DeferDeployment,
        // Server  -> Client
        CloseApplications
    }
}
