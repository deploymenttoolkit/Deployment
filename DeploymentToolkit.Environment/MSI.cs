namespace DeploymentToolkit.DTEnvironment
{
    public static class MSI
    {
        public const string SuppressReboot = "/norestart REBOOT=ReallySuppress";

        public const string DefaultInstallParameters = "/qn";
        public const string DefaultUninstallParameters = "/qn";

        public const string DefaultLoggingParameters = "/L*v";
    }
}
