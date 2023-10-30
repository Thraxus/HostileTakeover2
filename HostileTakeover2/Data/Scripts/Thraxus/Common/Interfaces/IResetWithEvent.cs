using System;

namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    public interface IResetWithEvent<out T> : IReset
    {
        event Action<T> OnReset;
    }
}