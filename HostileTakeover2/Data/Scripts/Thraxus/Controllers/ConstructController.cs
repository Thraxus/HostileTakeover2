using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Models;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class ConstructController : BaseLoggingClass
    {
        private readonly Dictionary<long, Construct> _constructs = new Dictionary<long, Construct>();

        public Construct GetConstruct(long entityId)
        {
            Construct c;
            return _constructs.TryGetValue(entityId, out c) ? c : null;
        }

        public void Add(long entityId, Construct construct)
        {
            if (_constructs.ContainsKey(entityId)) return;
            WriteGeneral(nameof(Add), $"Adding Construct with EntityId: {entityId:D18}");
            _constructs.Add(entityId, construct);
        }

        public void Remove(long entityId)
        {
            if (!_constructs.ContainsKey(entityId)) return;
            WriteGeneral(nameof(Remove), $"Removing Construct with EntityId: {entityId:D18}");
            _constructs.Remove(entityId);
        }
    }
}
