using System;

namespace HostileTakeover2.Thraxus.Utility.UserConfig.Types
{
    public class Setting<T>
    {
        public Type Type => typeof(T);
        public T Min { get; }
        public T Max { get; }

        public T Default;
        public T Current;

        public Setting(T min, T max, T @default, T current)
        {
            Min = min;
            Max = max;
            Default = @default;
            Current = current;
        }

        public Setting<T> CopyTo(Setting<T> setting)
        {
            if (setting.Type != Type) return setting;
            setting.Current = Current;
            return setting;
        }

        public void CopyFrom(Setting<T> setting)
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