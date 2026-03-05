using System;
using System.Globalization;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Settings;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Controllers
{
    /// <summary>
    /// Loads, validates, and saves user-configurable settings for the mod.
    ///
    /// <see cref="InitializeServer"/> performs a load-map-save round-trip:
    /// <list type="number">
    ///   <item><c>Get&lt;UserSettings&gt;()</c> reads the XML file from world storage
    ///     (returns null on first run when no file exists yet).</item>
    ///   <item><see cref="SettingsMapper"/> parses the raw string fields from the XML
    ///     object into typed values on <see cref="DefaultSettings"/> and repairs any
    ///     out-of-range values by writing the default back to the XML object.</item>
    ///   <item><c>Set(_userSettings)</c> writes the (possibly corrected) XML object back
    ///     to disk so the player sees the current effective values next time they edit it.</item>
    /// </list>
    /// </summary>
    public class UserConfigController : BaseXmlUserSettings
    {
        private UserSettings _userSettings;
        /// <summary>Strongly typed, runtime-active settings derived from the XML file.</summary>
        public readonly DefaultSettings DefaultSettings = new DefaultSettings();

        public UserConfigController(string modName) : base(modName) { }

        /// <summary>
        /// Server path: load XML → map → clean → save XML → write to sandbox so clients
        /// can read the authoritative values via <see cref="InitializeClient"/>.
        /// </summary>
        public void InitializeServer()
        {
            DefaultSettings.InitializeToDefaults();
            _userSettings = Get<UserSettings>();
            SettingsMapper();
            Set(_userSettings);
            WriteToSandbox();
        }

        /// <summary>
        /// Client path: reads the server-written sandbox variables and applies them
        /// directly to <see cref="DefaultSettings"/>, bypassing the XML file entirely.
        /// </summary>
        public void InitializeClient()
        {
            ReadFromSandbox();
        }

        /// <summary>
        /// Writes every active setting value to the SE sandbox variable store so
        /// clients can read them.  Called at the end of <see cref="InitializeServer"/>.
        /// Uses the <c>HT_</c> prefix to avoid collisions with other mods.
        /// <see cref="DefaultSettings.EntityDetectionRange"/> is cast to <c>float</c>
        /// because <c>SetVariable</c> does not support <c>double</c>; no meaningful
        /// precision is lost over the 100–250 m range.
        /// </summary>
        private void WriteToSandbox()
        {
            MyAPIGateway.Utilities.SetVariable("HT_EntityDetectionRange",                       (float)DefaultSettings.EntityDetectionRange.Current);
            MyAPIGateway.Utilities.SetVariable("HT_AllowPlayerHacking",                         DefaultSettings.AllowPlayerHacking.Current);
            MyAPIGateway.Utilities.SetVariable("HT_MirrorEasyNpcTakeovers",                     DefaultSettings.MirrorEasyNpcTakeovers.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UseHighlights",                              DefaultSettings.UseHighlights.Current);
            MyAPIGateway.Utilities.SetVariable("HT_HighlightAllGridsInRange",                   DefaultSettings.HighlightAllGridsInRange.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UseWeaponGroup",                             DefaultSettings.UseWeaponGroup.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UseMedicalGroup",                            DefaultSettings.UseMedicalGroup.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UseTrapGroup",                               DefaultSettings.UseTrapGroup.Current);
            MyAPIGateway.Utilities.SetVariable("HT_HighlightAllBlocks",                         DefaultSettings.HighlightAllBlocks.Current);
            MyAPIGateway.Utilities.SetVariable("HT_HighlightSingleNearestBlock",                DefaultSettings.HighlightSingleNearestBlock.Current);
            MyAPIGateway.Utilities.SetVariable("HT_HighlightSingleNearestBlockInActiveGroup",   DefaultSettings.HighlightSingleNearestBlockInActiveGroup.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UseGrinderTierHighlighting",                 DefaultSettings.UseGrinderTierHighlighting.Current);
            MyAPIGateway.Utilities.SetVariable("HT_BlocksPerGrinderTier",                       DefaultSettings.BlocksPerGrinderTier.Current);
            MyAPIGateway.Utilities.SetVariable("HT_UnknownGrinderTierBlockCount",               DefaultSettings.UnknownGrinderTierBlockCount.Current);
            MyAPIGateway.Utilities.SetVariable("HT_DebugMode",                                  DefaultSettings.DebugMode.Current);
            MyAPIGateway.Utilities.SetVariable("HT_VerboseMode",                                DefaultSettings.VerboseMode.Current);
            int mask = 0;
            foreach (var cat in DefaultSettings.ActiveDebugCategories)
                mask |= cat.Id;
            MyAPIGateway.Utilities.SetVariable("HT_ActiveDebugCategories", mask);
        }

        /// <summary>
        /// Reads every sandbox variable written by <see cref="WriteToSandbox"/> and
        /// applies the values directly to <see cref="DefaultSettings"/>.
        /// Called on clients instead of the full XML parse pipeline.
        /// </summary>
        private void ReadFromSandbox()
        {
            float entityDetectionRange;
            if (MyAPIGateway.Utilities.GetVariable("HT_EntityDetectionRange", out entityDetectionRange))
                DefaultSettings.EntityDetectionRange.Current = entityDetectionRange;

            bool b;
            if (MyAPIGateway.Utilities.GetVariable("HT_AllowPlayerHacking", out b))                        DefaultSettings.AllowPlayerHacking.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_MirrorEasyNpcTakeovers", out b))                    DefaultSettings.MirrorEasyNpcTakeovers.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_UseHighlights", out b))                             DefaultSettings.UseHighlights.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_HighlightAllGridsInRange", out b))                  DefaultSettings.HighlightAllGridsInRange.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_UseWeaponGroup", out b))                            DefaultSettings.UseWeaponGroup.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_UseMedicalGroup", out b))                           DefaultSettings.UseMedicalGroup.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_UseTrapGroup", out b))                              DefaultSettings.UseTrapGroup.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_HighlightAllBlocks", out b))                        DefaultSettings.HighlightAllBlocks.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_HighlightSingleNearestBlock", out b))               DefaultSettings.HighlightSingleNearestBlock.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_HighlightSingleNearestBlockInActiveGroup", out b))  DefaultSettings.HighlightSingleNearestBlockInActiveGroup.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_UseGrinderTierHighlighting", out b))               DefaultSettings.UseGrinderTierHighlighting.Current = b;

            int intVal;
            if (MyAPIGateway.Utilities.GetVariable("HT_BlocksPerGrinderTier", out intVal))               DefaultSettings.BlocksPerGrinderTier.Current = intVal;
            if (MyAPIGateway.Utilities.GetVariable("HT_UnknownGrinderTierBlockCount", out intVal))        DefaultSettings.UnknownGrinderTierBlockCount.Current = intVal;

            if (MyAPIGateway.Utilities.GetVariable("HT_DebugMode", out b))                                 DefaultSettings.DebugMode.Current = b;
            if (MyAPIGateway.Utilities.GetVariable("HT_VerboseMode", out b))                               DefaultSettings.VerboseMode.Current = b;

            int mask;
            if (MyAPIGateway.Utilities.GetVariable("HT_ActiveDebugCategories", out mask))
            {
                DefaultSettings.ActiveDebugCategories.Clear();
                foreach (var cat in LogCategory.AllRegistered)
                {
                    if (cat.Id != 0 && (mask & cat.Id) != 0)
                        DefaultSettings.ActiveDebugCategories.Add(cat);
                }
            }
        }

        /// <summary>
        /// Parses a comma-separated list of <see cref="LogCategory"/> names and populates
        /// <see cref="DefaultSettings.ActiveDebugCategories"/> accordingly.
        /// "All" activates every registered category; "None" clears the set.
        /// Any unrecognised token causes the whole setting to reset to All so the user
        /// always gets output rather than silence.
        /// Returns the canonical string to write back to the XML field.
        /// </summary>
        private string ParseDebugTypeSetting(string xmlValue)
        {
            if (string.IsNullOrEmpty(xmlValue))
            {
                DefaultSettings.InitializeToDefaults();
                return "All";
            }

            string trimmedValue = xmlValue.Trim();

            if (string.Equals(trimmedValue, "All", StringComparison.OrdinalIgnoreCase))
            {
                DefaultSettings.InitializeToDefaults();
                return "All";
            }

            if (string.Equals(trimmedValue, "None", StringComparison.OrdinalIgnoreCase))
            {
                DefaultSettings.ActiveDebugCategories.Clear();
                return "None";
            }

            DefaultSettings.ActiveDebugCategories.Clear();
            bool anyFailed = false;
            foreach (string part in xmlValue.Split(','))
            {
                string trimmed = part.Trim();
                LogCategory category;
                if (LogCategory.TryGetByName(trimmed, out category))
                    DefaultSettings.ActiveDebugCategories.Add(category);
                else
                    anyFailed = true;
            }

            if (anyFailed)
            {
                DefaultSettings.InitializeToDefaults();
                return "All";
            }

            return DefaultSettings.SerializeActiveCategories();
        }

        /// <summary>
        /// Parses a single integer setting from its XML string representation.
        /// Sets <paramref name="setting"/>.Current to the parsed value on success (when
        /// the value is within [Min, Max]), or leaves it at the existing value on error.
        /// Returns the canonical string to write back to the XML field.
        /// </summary>
        private string ParseIntSetting(string xmlValue, UserSetting<int> setting)
        {
            int value;
            if (int.TryParse(xmlValue, out value) && value >= setting.Min && value <= setting.Max)
            {
                setting.Current = value;
                return value.ToString();
            }
            return setting.Default.ToString();
        }

        /// <summary>
        /// Parses a single boolean setting from its XML string representation.
        /// Sets <paramref name="setting"/>.Current to the parsed value on success, or
        /// leaves it at the existing value on error.
        /// Returns the lower-case canonical string to write back to the XML field so the
        /// file stays normalised ("true"/"false") after the next <c>Set</c> call.
        /// </summary>
        private string ParseBoolSetting(string xmlValue, UserSetting<bool> setting)
        {
            bool value;
            if (bool.TryParse(xmlValue, out value))
            {
                setting.Current = value;
                return value.ToString().ToLower();
            }
            return setting.Default.ToString().ToLower();
        }

        /// <summary>
        /// Parses each string field in the loaded <see cref="UserSettings"/> XML object
        /// into the typed <see cref="DefaultSettings"/> fields.
        ///
        /// On first run (<c>_userSettings == null</c>): a new <see cref="UserSettings"/>
        /// object is created and populated from <see cref="DefaultSettings.CopyTo"/> so
        /// the file written to disk reflects all current defaults.
        ///
        /// For each subsequent run: each setting is attempted via <c>TryParse</c>.
        /// If parsing fails or the value is out of range, the XML field is corrected to
        /// the default string so the file stays valid after the next <c>Set</c> call.
        ///
        /// After all settings are parsed, if <c>MirrorEasyNpcTakeovers</c> is true the
        /// highlight and capture settings are force-overridden to their off values so
        /// the mod behaves identically to EasyNpcTakeovers regardless of the user's
        /// individual setting choices.
        /// </summary>
        protected sealed override void SettingsMapper()
        {
            // First run: no settings file exists yet; populate from defaults and return early.
            if (_userSettings == null)
            {
                _userSettings = new UserSettings();
                _userSettings.SettingsDescription = DefaultSettings.GetSettingsDescription(LogCategory.GetRegisteredNames());
                DefaultSettings.CopyTo(_userSettings);
                return;
            }

            // Always overwrite the description field so it stays current even if the
            // user edited it manually.
            _userSettings.SettingsDescription = DefaultSettings.GetSettingsDescription(LogCategory.GetRegisteredNames());

            // Parse EntityDetectionRange: a double that must fall within [Min, Max].
            // InvariantCulture ensures the decimal separator is always '.' regardless
            // of the server's locale (e.g. German servers use ',' as decimal separator).
            double entityDetectionRange;
            if (double.TryParse(_userSettings.EntityDetectionRange, NumberStyles.Float, CultureInfo.InvariantCulture, out entityDetectionRange))
            {
                if (entityDetectionRange >= DefaultSettings.EntityDetectionRange.Min && entityDetectionRange <= DefaultSettings.EntityDetectionRange.Max)
                    DefaultSettings.EntityDetectionRange.Current = entityDetectionRange;
                // Out of range: leave Current at default; file is corrected on next write.
            }
            else
            {
                // Parse failure: repair the XML field so it is corrected on next write.
                _userSettings.EntityDetectionRange = DefaultSettings.EntityDetectionRange.ToString().ToLower();
            }

            // Parse every boolean setting using the shared helper.
            _userSettings.AllowPlayerHacking = ParseBoolSetting(
                _userSettings.AllowPlayerHacking, DefaultSettings.AllowPlayerHacking);

            _userSettings.MirrorEasyNpcTakeovers = ParseBoolSetting(
                _userSettings.MirrorEasyNpcTakeovers, DefaultSettings.MirrorEasyNpcTakeovers);

            _userSettings.UseHighlights = ParseBoolSetting(
                _userSettings.UseHighlights, DefaultSettings.UseHighlights);

            _userSettings.HighlightAllGridsInRange = ParseBoolSetting(
                _userSettings.HighlightAllGridsInRange, DefaultSettings.HighlightAllGridsInRange);

            _userSettings.UseWeaponGroup = ParseBoolSetting(
                _userSettings.UseWeaponGroup, DefaultSettings.UseWeaponGroup);

            _userSettings.UseMedicalGroup = ParseBoolSetting(
                _userSettings.UseMedicalGroup, DefaultSettings.UseMedicalGroup);

            _userSettings.UseTrapGroup = ParseBoolSetting(
                _userSettings.UseTrapGroup, DefaultSettings.UseTrapGroup);

            _userSettings.HighlightAllBlocks = ParseBoolSetting(
                _userSettings.HighlightAllBlocks, DefaultSettings.HighlightAllBlocks);

            _userSettings.HighlightSingleNearestBlock = ParseBoolSetting(
                _userSettings.HighlightSingleNearestBlock, DefaultSettings.HighlightSingleNearestBlock);

            _userSettings.HighlightSingleNearestBlockInActiveGroup = ParseBoolSetting(
                _userSettings.HighlightSingleNearestBlockInActiveGroup, DefaultSettings.HighlightSingleNearestBlockInActiveGroup);

            _userSettings.UseGrinderTierHighlighting = ParseBoolSetting(
                _userSettings.UseGrinderTierHighlighting, DefaultSettings.UseGrinderTierHighlighting);

            _userSettings.BlocksPerGrinderTier = ParseIntSetting(
                _userSettings.BlocksPerGrinderTier, DefaultSettings.BlocksPerGrinderTier);

            _userSettings.UnknownGrinderTierBlockCount = ParseIntSetting(
                _userSettings.UnknownGrinderTierBlockCount, DefaultSettings.UnknownGrinderTierBlockCount);

            _userSettings.DebugMode = ParseBoolSetting(
                _userSettings.DebugMode, DefaultSettings.DebugMode);

            _userSettings.VerboseMode = ParseBoolSetting(
                _userSettings.VerboseMode, DefaultSettings.VerboseMode);

            _userSettings.ActiveDebugCategories = ParseDebugTypeSetting(
                _userSettings.ActiveDebugCategories);

            // MirrorEasyNpcTakeovers override: when enabled, force-disable all highlight
            // and capture features regardless of what the user set for those fields.
            if (DefaultSettings.MirrorEasyNpcTakeovers.Current)
            {
                DefaultSettings.UseHighlights.Current                             = false;
                DefaultSettings.UseWeaponGroup.Current                            = false;
                DefaultSettings.UseMedicalGroup.Current                           = false;
                DefaultSettings.UseTrapGroup.Current                              = false;
                DefaultSettings.AllowPlayerHacking.Current                        = false;
                DefaultSettings.HighlightAllGridsInRange.Current                  = false;
                DefaultSettings.HighlightAllBlocks.Current                        = DefaultSettings.HighlightAllBlocks.Default;
                DefaultSettings.HighlightSingleNearestBlock.Current               = DefaultSettings.HighlightSingleNearestBlock.Default;
                DefaultSettings.HighlightSingleNearestBlockInActiveGroup.Current  = DefaultSettings.HighlightSingleNearestBlockInActiveGroup.Default;
                DefaultSettings.UseGrinderTierHighlighting.Current                = DefaultSettings.UseGrinderTierHighlighting.Default;
                DefaultSettings.BlocksPerGrinderTier.Current                      = DefaultSettings.BlocksPerGrinderTier.Default;
                DefaultSettings.UnknownGrinderTierBlockCount.Current              = DefaultSettings.UnknownGrinderTierBlockCount.Default;
                DefaultSettings.EntityDetectionRange.Current                      = DefaultSettings.EntityDetectionRange.Default;
            }
        }
    }
}
