using System.Xml.Serialization;

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Settings
{
    [XmlRoot(nameof(UserSettings), IsNullable = true)]
    public class UserSettings
    {
        [XmlElement(nameof(SettingsDescription))]
        public string SettingsDescription;

        [XmlElement(nameof(EntityDetectionRange))]
        public string EntityDetectionRange;
    }
}