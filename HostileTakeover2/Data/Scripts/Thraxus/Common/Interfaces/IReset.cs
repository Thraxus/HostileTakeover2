using System;

namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    public interface IReset
    {
        event Action<IReset> OnReset;
        void Reset();
    }
}
