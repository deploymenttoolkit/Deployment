namespace DeploymentToolkit.Messaging
{
    public enum MessageId : byte
    {
        InitialConnectMessage,

        CloseApplications,

        InstallationStarted,
        InstallationEnded,
        InstallationError,

        UninstallationStarted,
        UninstallationEnded,
        UninstallationError,

        ExecutionBlocked
    }
}
