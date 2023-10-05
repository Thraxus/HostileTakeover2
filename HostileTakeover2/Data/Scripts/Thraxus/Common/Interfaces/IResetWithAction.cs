using System;

namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    public interface IResetWithAction : IReset
    {
        event Action<IResetWithAction> OnReset;
    }
}