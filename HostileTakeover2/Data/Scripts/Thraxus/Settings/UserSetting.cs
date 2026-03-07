using System;

namespace HostileTakeover2.Thraxus.Settings
{
    public class UserSetting<T>
    {
        public Type Type => typeof(T);
        public T Min { get; }
        public T Max { get; }

        public readonly T Default;
        public T Current;

        public UserSetting(T min, T max, T @default, T current)
        {
            Min = min;
            Max = max;
            Default = @default;
            Current = current;
        }

        public UserSetting<T> CopyTo(UserSetting<T> setting)
        {
            if (setting.Type != Type) return setting;
            setting.Current = Current;
            return setting;
        }

        public void CopyFrom(UserSetting<T> setting)
        {
            if (setting.Type != Type) return;
            Current = setting.Current;
        }

        public override string ToString()
        {
            return Current.ToString();
        }
    }
}