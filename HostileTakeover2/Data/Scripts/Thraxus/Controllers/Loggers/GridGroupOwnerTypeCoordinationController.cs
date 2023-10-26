using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupOwnerTypeCoordinationController : BaseLoggingClass
    {
        public readonly Dictionary<IMyGridGroupData, OwnerType> MyGridGroupOwnershipDictionary = new Dictionary<IMyGridGroupData, OwnerType>();

        public void AddOrUpdateOwnership(IMyGridGroupData myGridGroupData, OwnerType ownershipType)
        {
            if (!MyGridGroupOwnershipDictionary.ContainsKey(myGridGroupData))
            {
                MyGridGroupOwnershipDictionary.Add(myGridGroupData, ownershipType);
                return;
            }
            MyGridGroupOwnershipDictionary[myGridGroupData] = ownershipType;
        }

        public override void Reset()
        {
            MyGridGroupOwnershipDictionary.Clear();
            base.Reset();
        }
    }
}