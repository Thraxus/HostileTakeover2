using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class GridGroupOwnerTypeCoordinationController : BaseLoggingClass
    {
        public readonly Dictionary<IMyGridGroupData, OwnerType> MyGridGroupOwnershipDictionary = new Dictionary<IMyGridGroupData, OwnerType>();

        private Mediator _mediator;
        
        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        public OwnerType GetGridGroupOwnershipType(IMyGridGroupData myGridGroupData)
        {
            return !MyGridGroupOwnershipDictionary.ContainsKey(myGridGroupData) ? OwnerType.NotEvaluated : MyGridGroupOwnershipDictionary[myGridGroupData];
        }

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
            _mediator = null;
            base.Reset();
        }
    }
}