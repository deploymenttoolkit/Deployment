namespace DeploymentToolkit.Environment
{
    public static partial class DTEnvironment
    {
        public static bool IsRunningInTaskSequence = false;
        public static bool GUIEnabled
        {
            get
            {
                if (IsRunningInTaskSequence)
                    return false;

                return true;
            }
        }
    }
}
