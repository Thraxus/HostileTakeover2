using System.Text;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Utility.UserConfig.Models;

// ReSharper disable SpecifyACultureInStringConversionExplicitly

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Controllers
{
    public class SettingsController : BaseXmlUserSettings
    {
        private UserSettings _userSettings;
        public readonly DefaultSettings DefaultSettings = new DefaultSettings();
        
        public SettingsController(string modName) : base(modName) { }

        public void Initialize()
        {
            _userSettings = Get<UserSettings>();
            SettingsMapper();
            CleanUserSettings();
            Set(_userSettings);
        }

        private void CleanUserSettings()
        {
            // Nothing to do here, just leaving it in as a reminder.
        }

        private readonly StringBuilder _sb = new StringBuilder();

        public override string ToString()
        {
            _sb.AppendLine();
            _sb.AppendLine();
            return _sb.ToString();
        }

        private void AppendToLog(string str1, string str2, int messageNumber)
        {
            switch (messageNumber)
            {
                case 1:
                    _sb.AppendLine($"{str1} parsed! {str2}");
                    break;
                case 2:
                    _sb.AppendLine($"{str1} was within expected range! {str2}");
                    break;
                case 3:
                    _sb.AppendLine($"{str1} was not within expected range! {str2}");
                    break;
                case 4:
                    _sb.AppendLine($"{str1} failed to parse: {str2}");
                    break;
            }
        }

        protected sealed override void SettingsMapper()
        {
            _sb.AppendLine();
            _sb.AppendLine();

            if (_userSettings == null)
            {
                _userSettings = new UserSettings();
                _userSettings = DefaultSettings.CopyTo(_userSettings);
                return;
            }

            _userSettings.SettingsDescription = DefaultSettings.SettingsDescription;

            float entityDetectionRange;
            if (float.TryParse(_userSettings.EntityDetectionRange, out entityDetectionRange))
            {
                AppendToLog(nameof(entityDetectionRange), entityDetectionRange.ToString(), 1);
                if (entityDetectionRange >= DefaultSettings.EntityDetectionRange.Min && entityDetectionRange <= DefaultSettings.EntityDetectionRange.Max)
                {
                    DefaultSettings.EntityDetectionRange.Current = entityDetectionRange;
                    AppendToLog(nameof(entityDetectionRange), DefaultSettings.EntityDetectionRange.ToString(), 2);
                }
                else
                    AppendToLog(nameof(entityDetectionRange), DefaultSettings.EntityDetectionRange.ToString(), 3);

            }
            else
            {
                AppendToLog(nameof(entityDetectionRange), _userSettings.EntityDetectionRange, 4);
                _userSettings.EntityDetectionRange = DefaultSettings.EntityDetectionRange.ToString().ToLower();
            }

            //TODO If Use EasyNpcTakeovers == TRUE, then fuck every other setting and set all settings to do what ENT does.
            // No highlights, no traps, no weapons, no capture of blocks

        }
    }
}