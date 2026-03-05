using System.Xml.Serialization;

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Models
{
    [XmlRoot(nameof(UserSettings), IsNullable = true)]
    public class UserSettings
    {
        [XmlElement(nameof(SettingsDescription))]
        public string SettingsDescription;

        [XmlElement(nameof(EntityDetectionRange))]
        public string EntityDetectionRange;

        [XmlElement(nameof(AllowPlayerHacking))]
        public string AllowPlayerHacking;

        [XmlElement(nameof(MirrorEasyNpcTakeovers))]
        public string MirrorEasyNpcTakeovers;

        [XmlElement(nameof(UseHighlights))]
        public string UseHighlights;

        [XmlElement(nameof(HighlightAllGridsInRange))]
        public string HighlightAllGridsInRange;

        [XmlElement(nameof(UseWeaponGroup))]
        public string UseWeaponGroup;

        [XmlElement(nameof(UseMedicalGroup))]
        public string UseMedicalGroup;

        [XmlElement(nameof(UseTrapGroup))]
        public string UseTrapGroup;

        [XmlElement(nameof(HighlightAllBlocks))]
        public string HighlightAllBlocks;

        [XmlElement(nameof(HighlightSingleNearestBlock))]
        public string HighlightSingleNearestBlock;

        [XmlElement(nameof(HighlightSingleNearestBlockInActiveGroup))]
        public string HighlightSingleNearestBlockInActiveGroup;

        [XmlElement(nameof(UseGrinderTierHighlighting))]
        public string UseGrinderTierHighlighting;

        [XmlElement(nameof(DebugMode))]
        public string DebugMode;

        [XmlElement(nameof(VerboseMode))]
        public string VerboseMode;

        [XmlElement(nameof(ActiveDebugCategories))]
        public string ActiveDebugCategories;
    }
}
