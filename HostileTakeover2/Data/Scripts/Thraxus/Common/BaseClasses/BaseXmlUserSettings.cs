namespace HostileTakeover2.Thraxus.Common.BaseClasses
{
    /// <summary>
    /// Abstract base class for XML-backed user settings files.
    ///
    /// Extends <see cref="BaseLoggingClass"/> so that file I/O feedback (e.g. "file not
    /// found" or "writer was null") travels through the same <c>OnWriteToLog</c> event
    /// chain as the rest of the mod, without requiring static events on the file helpers.
    ///
    /// The filename is derived from the mod name at construction time so each mod using
    /// this base produces a uniquely named settings file
    /// (e.g. "MyMod_Settings.cfg") in world storage.
    ///
    /// Subclasses implement <see cref="SettingsMapper"/> to translate the raw
    /// deserialised XML object into typed settings fields.  The typical
    /// load-map-clean-save round-trip is:
    /// <code>
    ///   _userSettings = Get&lt;UserSettings&gt;();   // load or return null
    ///   SettingsMapper();                          // map XML fields to typed defaults
    ///   CleanUserSettings();                       // placeholder for future validation
    ///   Set(_userSettings);                        // write back (creates file on first run)
    /// </code>
    /// </summary>
    public abstract class BaseXmlUserSettings : BaseLoggingClass
    {
        /// <summary>
        /// Resolved filename for this settings file, including the ".cfg" extension.
        /// Format: <c>{modName}_Settings.cfg</c>.
        /// </summary>
        private readonly string _settingsFileName;
        private const string Extension = ".cfg";

        /// <summary>
        /// Initialises the settings file path using the mod name so the file is uniquely
        /// named per mod and does not clash with other mods' settings files.
        /// </summary>
        protected BaseXmlUserSettings(string modName)
        {
            _settingsFileName = modName + "_Settings" + Extension;
        }

        /// <summary>
        /// Subclass hook: maps fields from the raw deserialised XML object into strongly-
        /// typed settings on the concrete class.  Called after <see cref="Get{T}"/> returns
        /// so the raw strings/values from the file are available for parsing.
        /// </summary>
        protected abstract void SettingsMapper();

        /// <summary>
        /// Loads the settings file from world storage and deserialises it as
        /// <typeparamref name="T"/>.  Returns <c>default(T)</c> (usually <c>null</c>)
        /// if the file does not exist yet — the caller uses this as the "first run" signal.
        /// Any noteworthy I/O event (missing file, null reader) is logged via
        /// <see cref="BaseLoggingClass.WriteGeneral"/>.
        /// </summary>
        protected T Get<T>()
        {
            string message;
            T result = Utilities.FileHandlers.Load.ReadXmlFileInWorldStorage<T>(_settingsFileName, out message);
            if (!string.IsNullOrEmpty(message))
                WriteGeneral(nameof(Get), message);
            return result;
        }

        /// <summary>
        /// Serialises <paramref name="settings"/> to XML and writes it to world storage.
        /// The null guard prevents an unnecessary write when the object is invalid.
        /// Any noteworthy I/O event (null writer) is logged via
        /// <see cref="BaseLoggingClass.WriteGeneral"/>.
        /// </summary>
        protected void Set<T>(T settings)
        {
            if (settings == null) return;
            string message = Utilities.FileHandlers.Save.WriteXmlFileToWorldStorage(_settingsFileName, settings);
            if (!string.IsNullOrEmpty(message))
                WriteGeneral(nameof(Set), message);
        }
    }
}
