using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Models
{
    internal class Grid : BaseLoggingClass
    {
        /// <summary>
        /// Systems required for different ownership types:
        /// None / Player
        /// - On ownership check, trigger Ignore Grid, which should set the following conditions
        /// - Event for OnBlockOwnershipChanged (grid registration)
        /// - BlockTypeController reset
        /// NPC - All systems engaged
        /// </summary>

        private MyCubeGrid _me;
        private IMyGridGroupData _myGridGroupData;

        public readonly GridOwnershipController GridOwnershipController = new GridOwnershipController();
        public readonly BlockTypeController BlockTypeController = new BlockTypeController();
        private Mediator _mediator;

        public long CurrentOwnerId => _me.BigOwners.Count != 0 ? _me.BigOwners[0] : 0;

        public void Init(Mediator mediator, MyCubeGrid grid)
        {
            IsClosed = false;
            _me = grid;
            _mediator = mediator;
            _mediator.GridGroupCollectionController.AddToGrids(_me.EntityId, this);
            BlockTypeController.Init(mediator, GridOwnershipController);
            GridOwnershipController.SetOwnershipAction += SetOwnership;
            GridOwnershipController.DisownGridAction += DisownGrid;
            GridOwnershipController.TakeOverGridAction += TakeOverGrid;
            GridOwnershipController.IgnoreGridAction += IgnoreGrid;
            SetupGridGroup();
            RegisterEvents();
            _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
        }

        private void SetupGridGroup()
        {
            if (IsClosed) return;
            _myGridGroupData = _me.GetGridGroup(GridLinkTypeEnum.Logical);
            _myGridGroupData.OnGridRemoved += OnGridRemoved;
            _myGridGroupData.OnReleased += delegate
            {
                _myGridGroupData.OnGridRemoved -= OnGridRemoved;
            };
        }

        private void OnGridRemoved(IMyGridGroupData thisGridGroup, IMyCubeGrid removedGrid, IMyGridGroupData newGridGroup)
        {
            if (removedGrid.EntityId == _me.EntityId)
            {
                _myGridGroupData.OnGridRemoved -= OnGridRemoved;
                SetupGridGroup();
            }
            _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
        }

        public void DisownGrid()
        {
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            SetOwnership();
        }

        private void IgnoreGrid()
        {

        }

        private void TakeOverGrid()
        {
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc && !BlockTypeController.IsClosed)
                SetOwnership();
        }

        private void SetOwnership()
        {
            foreach (var block in _me.GetFatBlocks())
                SetOwnership(block);
        }

        private void SetOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? GridOwnershipController.RightfulOwner : 0, MyOwnershipShareModeEnum.None);
        }

        public void TriggerHighlights(long grinderOwnerIdentityId)
        {
            _mediator.HighlightController.EnableHighlights(_myGridGroupData, grinderOwnerIdentityId);
        }

        private void AddBlock(MyCubeBlock block)
        {
            SetOwnership(block);
            BlockTypeController.AddBlock(block);
        }

        private void RegisterEvents()
        {
            if(DefaultSettings.CapturePlayerBlocks)
            {
                _me.OnFatBlockAdded += OnBlockAdded;
                _me.OnBlockOwnershipChanged += OnBlockOwnershipChanged;
            }
            _me.OnMarkForClose += entity => Close();
            //_me.OnClosing += entity => Close();
            //_me.OnClose += entity => Close();
        }
        
        private void DeRegisterEvents()
        {
            if (DefaultSettings.CapturePlayerBlocks)
            {
                _me.OnFatBlockAdded -= OnBlockAdded;
                _me.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
            }
        }
        private void OnBlockAdded(MyCubeBlock block)
        {
            // TODO need to check here for the connector being added from a player ship.  We shouldn't be taking that over.  At the same time, we don't want it connected either. 
            // TODO perhaps add logic that looks for store blocks on the NPC grid and if none found (or the mating connector has trade disabled?) then just unlatch the connectors
            if (GridOwnershipController.OwnershipType == OwnershipType.Npc)
                _mediator.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
        }

        private void OnBlockOwnershipChanged(MyCubeGrid unused)
        {
            if (GridOwnershipController.OwnershipType == OwnershipType.None)
                _mediator.GridGroupCoordinationController.CoordinateOwnership(_myGridGroupData);
        }

        public override void Close()
        {
            if (IsClosed) return;
            base.Close();
            DeRegisterEvents();
            GridOwnershipController.Reset();
            BlockTypeController.Reset();
            _mediator.GridGroupCollectionController.RemoveFromGrids(_me.EntityId);
        }
    }
}