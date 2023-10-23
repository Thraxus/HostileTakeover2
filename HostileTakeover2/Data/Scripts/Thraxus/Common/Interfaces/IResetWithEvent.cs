using System;

namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    public interface IResetWithEvent : IReset
    {
        event Action<IResetWithEvent> OnReset;
    }
}