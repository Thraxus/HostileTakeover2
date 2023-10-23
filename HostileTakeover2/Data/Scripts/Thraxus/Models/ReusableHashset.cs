using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Models
{
    internal class ReusableHashset<T> : HashSet<T>, IReset
    {
        public void Reset()
        {
            Clear();
        }
    }
}