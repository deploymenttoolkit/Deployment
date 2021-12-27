using System.Xml.Serialization;

namespace DeploymentToolkit.Modals.Settings.Tray
{
    public class DefaultButtonSettings : IButtonSettings
    {
        [XmlElement(ElementName = "DefaultHeight", IsNullable = true)]
        public int? Height { get; set; } = 30;
        [XmlElement(ElementName = "DefaultWidth", IsNullable = true)]
        public int? Width { get; set; } = 120;
        [XmlElement(ElementName = "DefaultFontColor", IsNullable = true)]
        public string FontColor { get; set; } = string.Empty;
        [XmlElement(ElementName = "DefaultFontWeight", IsNullable = true)]
        public string FontWeight { get; set; } = string.Empty;
        [XmlElement(ElementName = "DefaultBackgroundColor", IsNullable = true)]
        public string BackgroundColor { get; set; } = "#F1F1F1";
        [XmlElement(ElementName = "DefaultBorderColor", IsNullable = true)]
        public string BorderColor { get; set; }

        public ButtonSettings UpgradeButton { get; set; }
        public ButtonSettings ScheduleButton { get; set; }
        public ButtonSettings MinimizeButton { get; set; }
    }
}
