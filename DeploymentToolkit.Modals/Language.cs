using System.Xml.Serialization;

namespace DeploymentToolkit.Modals
{
    [XmlRoot("Messages")]
    public class Language
    {
        public string DiskSpace_Message { get; set; }

        public string ClosePrompt_ButtonContinue { get; set; }
        public string ClosePrompt_ButtonContinueTooltip { get; set; }
        public string ClosePrompt_ButtonClose { get; set; }
        public string ClosePrompt_ButtonDefer { get; set; }
        public string ClosePrompt_Message { get; set; }
        public string ClosePrompt_CountdownMessage { get; set; }

        public string DeferPrompt_WelcomeMessage { get; set; }
        public string DeferPrompt_ExpiryMessage { get; set; }
        public string DeferPrompt_WarningMessage { get; set; }
        public string DeferPrompt_RemainingDeferrals { get; set; }
        public string DeferPrompt_Deadline { get; set; }

        public string WelcomePrompt_CountdownMessage { get; set; }
        public string WelcomePrompt_CustomMessage { get; set; }

        public string DeploymentType_Install { get; set; }
        public string DeploymentType_UnInstall { get; set; }

        public string BalloonText_Start { get; set; }
        public string BalloonText_Complete { get; set; }
        public string BalloonText_RestartRequired { get; set; }
        public string BalloonText_Error { get; set; }
        public string BalloonText_FastRetry { get; set; }
        public string Progress_MessageInstall { get; set; }
        public string Progress_MessageUninstall { get; set; }

        public string BlockExecution_Message { get; set; }

        public string RestartPrompt_Title { get; set; }
        public string RestartPrompt_Message { get; set; }
        public string RestartPrompt_MessageTime { get; set; }
        public string RestartPrompt_MessageRestart { get; set; }
        public string RestartPrompt_TimeRemaining { get; set; }
        public string RestartPrompt_ButtonRestartLater { get; set; }
        public string RestartPrompt_ButtonRestartNow { get; set; }
    }
}
