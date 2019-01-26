namespace DeploymentToolkit.Messaging
{
    public enum MessageId : byte
    {
        InitialConnectMessage,
        DeploymentInformationMessage,

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
