namespace DeploymentToolkit.Environment
{
    public static partial class DTEnvironment
    {
        public static bool ForceDisableGUI = false;
        public static bool IsRunningInTaskSequence = false;
        public static bool GUIEnabled
        {
            get
            {
                if (ForceDisableGUI)
                    return false;

                if (IsRunningInTaskSequence)
                    return false;

                return true;
            }
        }
    }
}
