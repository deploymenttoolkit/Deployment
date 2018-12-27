namespace DeploymentToolkit.Messaging
{
    public enum MessageId : byte
    {
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
