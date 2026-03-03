using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Interfaces;

namespace HostileTakeover2.Thraxus.Collections
{
    internal class ReusableCubeGridList<T> : HashSet<T>, IReset
    {
        public void Reset()
        {
            Clear();
        }
    }
}