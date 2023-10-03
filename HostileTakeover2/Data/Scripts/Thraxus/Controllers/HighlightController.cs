using System.Collections.Generic;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class HighlightController
    {
        private Utilities _utilities;

        public void Init(Utilities utilities)
        {
            _utilities = utilities;
        }

        public void HighlightBlocks(HashSet<Block> importantBlock)
        {
            throw new System.NotImplementedException();
        }
    }
}