namespace DeploymentToolkit.Deployment
{
    internal enum ExitCode
    {
        ExitOK = 0,
        FailedToInitilizeLogger = -1,
        SettingsFileMissing = -2,
        FailedToReadSettings = -3,
        MissingRequiredParameter = -4,
        InstallFileMissing = -5,
        UninstallFileMissing = -6,

        InvalidCommandLineSpecified = -7,
        ErrorDuringInstallation = -8
    }
}
