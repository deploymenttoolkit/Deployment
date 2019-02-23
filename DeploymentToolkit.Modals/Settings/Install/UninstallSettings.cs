using System.Xml.Serialization;

namespace DeploymentToolkit.Modals.Settings.Install
{
    [XmlRoot(ElementName = "UninstallSettings")]
    public class InstallerUninstallSettings
    {
        public bool IgnoreUninstallErrors { get; set; }
        [XmlArrayItemAttribute("Item", IsNullable = false)]
        public string[] Uninstall { get; set; }
    }
}
