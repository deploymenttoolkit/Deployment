namespace DeploymentToolkit.Messaging
{
    public enum MessageId : byte
    {
        InitialConnectMessage,

        InstallationStarted,
        InstallationEnded,
        InstallationError,

        UninstallationStarted,
        UninstallationEnded,
        UninstallationError,

        ExecutionBlocked,

        ShowBalloonTip,

        ContinueDeployment,

        DeferDeployment,
        CloseApplications
    }
}
