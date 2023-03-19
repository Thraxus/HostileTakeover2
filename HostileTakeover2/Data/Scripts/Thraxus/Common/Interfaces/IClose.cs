using System;

namespace HostileTakeover2.Thraxus.Common.Interfaces
{
    public interface IClose
    {
        event Action<IClose> OnClose;
        void Close();
        bool IsClosed { get; }
    }
}
