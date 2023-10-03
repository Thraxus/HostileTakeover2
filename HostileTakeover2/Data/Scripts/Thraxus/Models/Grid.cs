using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Controllers;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace HostileTakeover2.Thraxus.Models
{
    internal class Grid : BaseLoggingClass
    {
        private MyCubeGrid _me;
        private IMyGridGroupData _myGridGroupData;
        public OwnershipController OwnershipController = new OwnershipController();

        private readonly BlockController _blockController = new BlockController();
        private Utilities _utilities;

        public OwnershipType Ownership => 
            _me.BigOwners.Count != 0 && MyAPIGateway.Players.TryGetSteamId(_me.BigOwners[0]) <= 0 ? 
                OwnershipType.Npc : OwnershipType.Other;

        public void Init(Utilities utilities, MyCubeGrid grid)
        {
            IsClosed = false;
            _me = grid;
            _utilities = utilities;
            _utilities.GridController.AddToGrids(_me.EntityId, this);
            _blockController.Init(utilities, OwnershipController);
            OwnershipController.SetOwnershipAction += SetOwnership;
            SetupGridGroup();
            RegisterEvents();
            EvaluateOwnership();
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
            EvaluateOwnership();
        }

        private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();

        private void EvaluateOwnership()
        {
            _reusableGridCollection.Clear();
            _myGridGroupData.GetGrids(_reusableGridCollection);

            bool disown = true;

            OwnershipController.SoftReset();
            foreach (IMyCubeGrid grid in _reusableGridCollection)
            {
                Grid someGrid = _utilities.GridController.GetGrid(grid.EntityId);
                if (someGrid.Ownership != OwnershipType.Npc) continue;
                disown = false; // We just need one hit to confirm this is still a NPC ship.

                // TODO Evaluate this, I think it can fail.  Need a good way to pool ownership and unite the grids with one owner if it does fail;
                OwnershipController.SetOwnership(someGrid.OwnershipController.OwnerId(_me) != 0 && MyAPIGateway.Players.TryGetSteamId(_me.BigOwners[0]) <= 0 ? someGrid.OwnershipController.OwnerId(_me) : 0, OwnershipType.Npc);
            }

            if (!disown) return; // No grids in the grid group was npc owned, so this collection needs neutral ownership

            foreach (IMyCubeGrid grid in _reusableGridCollection)
                _utilities.GridController.GetGrid(grid.EntityId).DisownGrid();

        }

        public void DisownGrid()
        {
            OwnershipController.Reset();
            foreach (var block in _me.GetFatBlocks())
                SetOwnership(block);
        }
         
        private void SetOwnership(MyCubeBlock block)
        {
            block.ChangeOwner(block.IsFunctional ? OwnershipController.RightfulOwner : 0, MyOwnershipShareModeEnum.None);
        }

        public void TriggerHighlights()
        {
            EnableHighlights();
        }

        public void EnableHighlights()
        {
            if (!DefaultSettings.UseHighlights.Current) return;
            _reusableGridCollection.Clear();
            _myGridGroupData.GetGrids(_reusableGridCollection);
            
            // TODO this operates on the assumption the grid group contains this grid.  If not, modify as needed.
            foreach (IMyCubeGrid grid in _reusableGridCollection)
            {
                Grid someGrid = _utilities.GridController.GetGrid(grid.EntityId);
                someGrid.EnableHighlights();
            }
        }

        private void AddBlock(MyCubeBlock block)
        {
            SetOwnership(block);
            _blockController.AddBlock(block);
        }

        private void RegisterEvents()
        {
            _me.OnFatBlockAdded += OnBlockAdded;
            _me.OnBlockOwnershipChanged += OnBlockOwnershipChanged;
            _me.OnMarkForClose += entity => Close();
            _me.OnClosing += entity => Close();
            _me.OnClose += entity => Close();
        }

        private void DeRegisterEvents()
        {
            _me.OnFatBlockAdded -= OnBlockAdded;
            _me.OnBlockOwnershipChanged -= OnBlockOwnershipChanged;
        }

        private void OnBlockOwnershipChanged(MyCubeGrid grid)
        {
            EvaluateOwnership();
        }

        private void OnBlockAdded(MyCubeBlock block)
        {
            if (Ownership == OwnershipType.Npc)
                _utilities.ActionQueue.Add(DefaultSettings.BlockAddTickDelay, () => AddBlock(block));
        }

        public override void Close()
        {
            if (IsClosed) return;
            base.Close();
            OwnershipController.Reset();
            DeRegisterEvents();
            _utilities.GridController.RemoveFromGrids(_me.EntityId);
        }
    }
}