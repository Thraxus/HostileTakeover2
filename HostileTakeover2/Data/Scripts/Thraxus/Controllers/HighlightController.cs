using System;
using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Extensions;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Infrastructure;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Settings;
using Sandbox.Game;
using Sandbox.ModAPI.Weapons;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class HighlightController : BaseLoggingClass
    {
        private readonly Dictionary<BlockType, HashSet<Block>> _reusableImportantBlocksDictionary =
            new Dictionary<BlockType, HashSet<Block>>
            {
                { BlockType.Control, new HashSet<Block>() },
                { BlockType.Medical, new HashSet<Block>() },
                { BlockType.Trap, new HashSet<Block>() },
                { BlockType.Weapon, new HashSet<Block>() }
            };

        private readonly Dictionary<Block, HighlightSettings> _currentHighlightedBlocks = new Dictionary<Block, HighlightSettings>();
        private readonly List<Block> _sortBuffer = new List<Block>();

        private Mediator _mediator;

        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        private void HighlightBlock(Block block, BlockType type, long playerId)
        {
            if (_currentHighlightedBlocks.ContainsKey(block)) return;
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Highlight))
                WriteGeneral(DebugType.Highlight, nameof(HighlightBlock), $"Block highlighted [{type}] '{block.MyCubeBlock?.DisplayNameText}': block=[{block.EntityId:D18}] grid=[{block.MyCubeBlock?.CubeGrid?.EntityId:D18}]");
            var hls = _mediator.GetHighlightSetting();
            hls.Name = block.Name;
            hls.Color = GetHighlightColor(type);
            hls.PlayerId = playerId;
            hls.Enabled = true;
            hls.LineThickness = _mediator.DefaultSettings.EnabledThickness;
            hls.PulseDuration = _mediator.DefaultSettings.HighlightPulseDuration;
            SetHighlight(hls);
            _currentHighlightedBlocks.Add(block, hls);
            block.OnClose += RemoveFromHighlightedBlocks;
            block.OnReset += RemoveFromHighlightedBlocks;
            block.BlockHasBeenDisabledAction += RemoveFromHighlightedBlocks;
            _mediator.ActionQueue.Add(_mediator.DefaultSettings.HighlightDuration, () => RemoveFromHighlightedBlocks(block));
        }

        private void RemoveFromHighlightedBlocks(Block block)
        {
            try
            {
                if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Highlight))
                    WriteGeneral(DebugType.Highlight, nameof(RemoveFromHighlightedBlocks), $"Attempting to remove a block: [{(_currentHighlightedBlocks.ContainsKey(block)).ToSingleChar()}] {block.EntityId.ToEntityIdFormat()}");
                if (!_currentHighlightedBlocks.ContainsKey(block)) return;
                HighlightSettings hls = _currentHighlightedBlocks[block];
                _currentHighlightedBlocks.Remove(block);
                block.OnClose -= RemoveFromHighlightedBlocks;
                block.OnReset -= RemoveFromHighlightedBlocks;
                block.BlockHasBeenDisabledAction -= RemoveFromHighlightedBlocks;
                hls.Enabled = false;
                hls.LineThickness = _mediator.DefaultSettings.DisabledThickness;
                SetHighlight(hls);
                _mediator.ReturnHighlightSetting(hls);
            }
            catch (Exception e) { WriteGeneral(nameof(RemoveFromHighlightedBlocks), $"Exception: {e}"); }
        }

        private void RemoveFromHighlightedBlocks(IResetWithAction block)
        {
            RemoveFromHighlightedBlocks((Block)block);
        }

        private void RemoveFromHighlightedBlocks(IClose block)
        {
            RemoveFromHighlightedBlocks((Block)block);
        }

        private static void SetHighlight(HighlightSettings settings)
        {
            MyVisualScriptLogicProvider.SetHighlight(settings.Name, settings.Enabled, settings.LineThickness, 0, settings.Color, settings.PlayerId);
        }

        private Color GetHighlightColor(BlockType type)
        {
            switch (type)
            {
                case BlockType.Medical:
                    return _mediator.DefaultSettings.MedicalColor;
                case BlockType.Trap:
                    return _mediator.DefaultSettings.TrapColor;
                case BlockType.Weapon:
                    return _mediator.DefaultSettings.WeaponColor;
                case BlockType.Control:
                case BlockType.None:
                default:
                    return _mediator.DefaultSettings.ControlColor;
            }
        }

        public void EnableHighlights(IMyGridGroupData myGridGroupData, IMyAngleGrinder grinder)
        {
            ClearReusableImportantBlockDictionary();
            long playerId = grinder.OwnerIdentityId;
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Highlight))
                WriteGeneral(DebugType.Highlight, nameof(EnableHighlights), $"Attempting to enable highlights for grid group against entity {playerId.ToEntityIdFormat()}");
            var gridList = _mediator.GetReusableCubeGridList(myGridGroupData);
            int counter = 0;
            foreach (var myCubeGrid in gridList)
            {
                Construct construct = _mediator.ConstructController.GetConstruct(myCubeGrid.EntityId);
                if (construct == null) continue;
                foreach (var kvp in construct.BlockController.GetImportantBlockDictionary())
                {
                    Block block = kvp.Value;
                    if (!block.IsFunctional || block.IsClosed || block.BlockType == BlockType.None)
                    {
                        if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Highlight))
                            WriteGeneral(DebugType.Highlight, nameof(EnableHighlights), $"Block rejected!  Type: {block.BlockType} | Functional: {block.IsFunctional} | Closed: {block.IsClosed}");
                        continue;
                    }
                    _reusableImportantBlocksDictionary[block.BlockType].Add(block);
                    counter++;
                }
            }
            if (_mediator.DefaultSettings.IsDebugActiveFor(DebugType.Highlight))
                WriteGeneral(DebugType.Highlight, nameof(EnableHighlights), $"Attempting to highlight {counter:D3} blocks [{_reusableImportantBlocksDictionary[BlockType.Control].Count:D2}]  [{_reusableImportantBlocksDictionary[BlockType.Medical].Count:D2}]  [{_reusableImportantBlocksDictionary[BlockType.Weapon].Count:D2}]  [{_reusableImportantBlocksDictionary[BlockType.Trap].Count:D2}]");
            _mediator.ReturnReusableCubeGridList(gridList);
            HighlightNextSet(grinder, playerId);
        }

        private void ClearReusableImportantBlockDictionary()
        {
            foreach (var kvp in _reusableImportantBlocksDictionary)
                kvp.Value.Clear();
        }

        private void HighlightNextSet(IMyAngleGrinder grinder, long playerId)
        {
            Vector3D grinderPos = grinder.GetPosition();

            // 1. Single nearest block across all groups
            if (_mediator.DefaultSettings.HighlightSingleNearestBlock.Current)
            {
                Block nearest = FindNearestBlockInAll(grinderPos);
                if (nearest != null) HighlightBlock(nearest, nearest.BlockType, playerId);
                return;
            }

            // 2. All important blocks regardless of group
            if (_mediator.DefaultSettings.HighlightAllBlocks.Current)
            {
                foreach (var kvp in _reusableImportantBlocksDictionary)
                    HighlightBlocks(kvp.Value, kvp.Key, playerId);
                return;
            }

            // 3. Select active group by priority
            BlockType type = BlockType.None;
            if (_reusableImportantBlocksDictionary[BlockType.Control].Count > 0)
                type = BlockType.Control;
            else if (_reusableImportantBlocksDictionary[BlockType.Medical].Count > 0 && _mediator.DefaultSettings.UseMedicalGroup.Current)
                type = BlockType.Medical;
            else if (_reusableImportantBlocksDictionary[BlockType.Weapon].Count > 0 && _mediator.DefaultSettings.UseWeaponGroup.Current)
                type = BlockType.Weapon;
            else if (_reusableImportantBlocksDictionary[BlockType.Trap].Count > 0 && _mediator.DefaultSettings.UseTrapGroup.Current)
                type = BlockType.Trap;

            if (type == BlockType.None) return;

            var activeGroup = _reusableImportantBlocksDictionary[type];

            // 4. Single nearest block in active group
            if (_mediator.DefaultSettings.HighlightSingleNearestBlockInActiveGroup.Current)
            {
                Block nearest = FindNearestBlock(activeGroup, grinderPos);
                if (nearest != null) HighlightBlock(nearest, type, playerId);
                return;
            }

            // 5. Default: all in active group, tier-capped if enabled
            if (_mediator.DefaultSettings.UseGrinderTierHighlighting.Current)
            {
                int maxCount = GetHighlightBlockCount(grinder);
                if (maxCount != int.MaxValue)
                {
                    HighlightNearestBlocks(activeGroup, type, playerId, grinderPos, maxCount);
                    return;
                }
            }

            HighlightBlocks(activeGroup, type, playerId);
        }

        private void HighlightBlocks(HashSet<Block> blocks, BlockType type, long playerId)
        {
            foreach (var block in blocks)
                HighlightBlock(block, type, playerId);
        }

        // Partial selection sort: finds and highlights the nearest `maxCount` blocks
        // without allocating. For the small block sets typical in this mod (1-10 blocks),
        // this is faster than a full sort and avoids any heap pressure.
        private void HighlightNearestBlocks(HashSet<Block> blocks, BlockType type, long playerId, Vector3D position, int maxCount)
        {
            _sortBuffer.Clear();
            foreach (var block in blocks)
                _sortBuffer.Add(block);

            int count = Math.Min(maxCount, _sortBuffer.Count);
            for (int pass = 0; pass < count; pass++)
            {
                int nearestIdx = pass;
                double nearestDist = (_sortBuffer[pass].MyCubeBlock.PositionComp.GetPosition() - position).LengthSquared();
                for (int i = pass + 1; i < _sortBuffer.Count; i++)
                {
                    double dist = (_sortBuffer[i].MyCubeBlock.PositionComp.GetPosition() - position).LengthSquared();
                    if (dist >= nearestDist) continue;
                    nearestDist = dist;
                    nearestIdx = i;
                }
                Block tmp = _sortBuffer[pass];
                _sortBuffer[pass] = _sortBuffer[nearestIdx];
                _sortBuffer[nearestIdx] = tmp;
                HighlightBlock(_sortBuffer[pass], type, playerId);
            }
            _sortBuffer.Clear();
        }

        private static Block FindNearestBlock(HashSet<Block> blocks, Vector3D position)
        {
            Block nearest = null;
            double minDist = double.MaxValue;
            foreach (var block in blocks)
            {
                double dist = (block.MyCubeBlock.PositionComp.GetPosition() - position).LengthSquared();
                if (dist >= minDist) continue;
                minDist = dist;
                nearest = block;
            }
            return nearest;
        }

        private Block FindNearestBlockInAll(Vector3D position)
        {
            Block nearest = null;
            double minDist = double.MaxValue;
            foreach (var kvp in _reusableImportantBlocksDictionary)
            {
                foreach (var block in kvp.Value)
                {
                    double dist = (block.MyCubeBlock.PositionComp.GetPosition() - position).LengthSquared();
                    if (dist >= minDist) continue;
                    minDist = dist;
                    nearest = block;
                }
            }
            return nearest;
        }

        private int GetHighlightBlockCount(IMyAngleGrinder grinder)
        {
            var entity = grinder as MyEntity;
            if (entity == null)
            {
                int unk = _mediator.DefaultSettings.UnknownGrinderTierBlockCount.Current;
                return unk == 0 ? int.MaxValue : unk;
            }
            int perTier = _mediator.DefaultSettings.BlocksPerGrinderTier.Current;
            string subtype = entity.DefinitionId?.SubtypeId.String;
            switch (subtype)
            {
                case "AngleGrinder":  return perTier;
                case "AngleGrinder2": return 2 * perTier;
                case "AngleGrinder3": return 3 * perTier;
                case "AngleGrinder4": return int.MaxValue;
                default:
                    int unknown = _mediator.DefaultSettings.UnknownGrinderTierBlockCount.Current;
                    return unknown == 0 ? int.MaxValue : unknown;
            }
        }

        public override void Reset()
        {
            ClearReusableImportantBlockDictionary();
            _sortBuffer.Clear();
            base.Reset();
        }
    }
}
