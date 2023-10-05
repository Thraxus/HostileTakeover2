using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Utility;
using HostileTakeover2.Thraxus.Utility.UserConfig.Settings;
using Sandbox.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace HostileTakeover2.Thraxus.Controllers
{
    internal class HighlightController
    {
        

        private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();
        private readonly Dictionary<BlockType, HashSet<Block>> _reusableImportantBlocksDictionary =
            new Dictionary<BlockType, HashSet<Block>>
            {
                { BlockType.Control, new HashSet<Block>() },
                { BlockType.Medical, new HashSet<Block>() },
                { BlockType.Trap, new HashSet<Block>() },
                { BlockType.Weapon, new HashSet<Block>() }
            };

        private readonly Dictionary<Block, HighlightSettings> _currentHighlightedBlocks = new Dictionary<Block, HighlightSettings>();

        private Mediator _mediator;
        
        public void Init(Mediator mediator)
        {
            _mediator = mediator;
        }

        public void HighlightBlocks(HashSet<Block> importantBlocks, BlockType type, long grinderOwnerIdentityId)
        {
            foreach (var block in importantBlocks)
            {
                HighlightBlock(block, type, grinderOwnerIdentityId);
            }
        }

        private void HighlightBlock(Block block, BlockType type, long grinderOwnerIdentityId)
        {
            HighlightSettings hls = _mediator.HighlightSettingsPool.Get();
            hls.Name = block.Name;
            hls.Color = GetHighlightColor(type);
            hls.PlayerId = grinderOwnerIdentityId;
            hls.Enabled = true;
            SetHighlight(hls);
            _currentHighlightedBlocks.Add(block, hls);
            block.OnClose += RemoveFromHighlightedBlocks;
            block.OnReset += RemoveFromHighlightedBlocks;
            block.BlockHasBeenDisableAction += RemoveFromHighlightedBlocks;
            _mediator.ActionQueue.Add(DefaultSettings.HighlightDuration, () => RemoveFromHighlightedBlocks(block));
        }

        private void RemoveFromHighlightedBlocks(Block block)
        {
            if (!_currentHighlightedBlocks.ContainsKey(block)) return;
            HighlightSettings hls = _currentHighlightedBlocks[block];
            _currentHighlightedBlocks.Remove(block);
            block.OnClose -= RemoveFromHighlightedBlocks;
            block.OnReset -= RemoveFromHighlightedBlocks;
            block.BlockHasBeenDisableAction -= RemoveFromHighlightedBlocks;
            hls.Enabled = false;
            SetHighlight(hls);
            _mediator.HighlightSettingsPool.Return(hls);
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
            MyVisualScriptLogicProvider.SetHighlight(settings.Name, settings.Enabled, settings.Thickness, settings.Duration, settings.Color, settings.PlayerId);
        }

        private Color GetHighlightColor(BlockType type)
        {
            switch (type)
            {
                case BlockType.Medical:
                    return DefaultSettings.MedicalColor;
                    break;
                case BlockType.Trap:
                    return DefaultSettings.TrapColor;
                    break;
                case BlockType.Weapon:
                    return DefaultSettings.WeaponColor;
                    break;
                case BlockType.Control:
                case BlockType.None:
                default:
                    return DefaultSettings.ControlColor;
            }
        }

        public void EnableHighlights(IMyGridGroupData myGridGroupData, long grinderOwnerIdentityId)
        {
            _reusableGridCollection.Clear();
            ClearReusableImportantBlockDictionary();

            myGridGroupData.GetGrids(_reusableGridCollection);

            foreach (var myCubeGrid in _reusableGridCollection)
            {
                Grid grid = _mediator.GridGroupCollectionController.GetGrid(myCubeGrid.EntityId);
                foreach (var kvp in grid.BlockTypeController.GetImportantBlockDictionary())
                {
                    foreach (var block in kvp.Value)
                    {
                        if (!block.IsFunctional || block.IsClosed) continue;
                        _reusableImportantBlocksDictionary[kvp.Key].Add(block);
                    }
                }
            }

            HighlightNextSet(grinderOwnerIdentityId);
        }

        private void ClearReusableImportantBlockDictionary()
        {
            foreach (var kvp in _reusableImportantBlocksDictionary)
            {
                kvp.Value.Clear();
            }
        }

        public void HighlightNextSet(long grinderOwnerIdentityId)
        {
            BlockType type = BlockType.None;
            if (_reusableImportantBlocksDictionary[BlockType.Control].Count > 0)
            {
                type = BlockType.Control;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Medical].Count > 0 && DefaultSettings.UseMedicalGroup.Current)
            {
                type = BlockType.Medical;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Weapon].Count > 0 && DefaultSettings.UseWeaponGroup.Current)
            {
                type = BlockType.Weapon;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Trap].Count > 0 && DefaultSettings.UseTrapGroup.Current)
            {
                type = BlockType.Trap;
            }

            if (type == BlockType.None) return;

            HighlightBlocks(_reusableImportantBlocksDictionary[type], type, grinderOwnerIdentityId);
        }
    }
}