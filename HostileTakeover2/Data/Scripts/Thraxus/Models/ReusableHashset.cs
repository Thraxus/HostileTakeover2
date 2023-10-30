using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Models
{
    public class ReusableHashset<T> : HashSet<T>, IReset
    {
        public bool IsReset { get; } = true;

        public void Reset()
        {
            Clear();
        }
    }
}