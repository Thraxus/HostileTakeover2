using System.Collections.Generic;
using HostileTakeover2.Thraxus.Common.BaseClasses;
using HostileTakeover2.Thraxus.Common.Interfaces;
using HostileTakeover2.Thraxus.Enums;
using HostileTakeover2.Thraxus.Models;
using HostileTakeover2.Thraxus.Models.Loggers;
using HostileTakeover2.Thraxus.Utility;
using Sandbox.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace HostileTakeover2.Thraxus.Controllers.Loggers
{
    internal class HighlightController : BaseLoggingClass
    {
        //private readonly List<IMyCubeGrid> _reusableGridCollection = new List<IMyCubeGrid>();
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

        private void HighlightBlock(Block block, BlockType type, long grinderOwnerIdentityId)
        {
            //HighlightSettings hls = _mediator.HighlightSettingsPool.Get();
            var hls = _mediator.GetHighlightSetting();
            hls.Name = block.Name;
            hls.Color = GetHighlightColor(type);
            hls.PlayerId = grinderOwnerIdentityId;
            hls.Enabled = true;
            hls.LineThickness = _mediator.DefaultSettings.EnabledThickness;
            hls.PulseDuration = _mediator.DefaultSettings.HighlightPulseDuration;
            SetHighlight(hls);
            _currentHighlightedBlocks.Add(block, hls);
            block.OnClose += RemoveFromHighlightedBlocks;
            block.OnReset += RemoveFromHighlightedBlocks;
            block.BlockHasBeenDisableAction += RemoveFromHighlightedBlocks;
            _mediator.ActionQueue.Add(_mediator.DefaultSettings.HighlightDuration, () => RemoveFromHighlightedBlocks(block));
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
            hls.LineThickness = _mediator.DefaultSettings.DisabledThickness;
            SetHighlight(hls);
            //_mediator.HighlightSettingsPool.Return(hls);
            _mediator.ReturnHighlightSetting(hls);
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
            MyVisualScriptLogicProvider.SetHighlight(settings.Name, settings.Enabled, settings.LineThickness, settings.PulseDuration, settings.Color, settings.PlayerId);
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

        public void EnableHighlights(IMyGridGroupData myGridGroupData, long grinderOwnerIdentityId)
        {
            //_reusableGridCollection.Clear();
            ClearReusableImportantBlockDictionary();
            WriteGeneral(nameof(EnableHighlights), $"Attempting to enable highlights for grid group against entity {grinderOwnerIdentityId:D18}");
            //myGridGroupData.GetGrids(_reusableGridCollection);
            var gridList = _mediator.GridGroupCollectionController.Get(myGridGroupData);
            int counter = 0;
            foreach (var myCubeGrid in gridList)
            {
                Grid grid = _mediator.GridCollectionController.GetGrid(myCubeGrid.EntityId);
                foreach (var kvp in grid.BlockTypeController.GetImportantBlockDictionary())
                {
                    foreach (var block in kvp.Value)
                    {
                        if (!block.IsFunctional || block.IsClosed) continue;
                        _reusableImportantBlocksDictionary[kvp.Key].Add(block);
                        counter++;
                    }
                }
            }
            WriteGeneral(nameof(EnableHighlights), $"Attempting to highlight {counter:D3} blocks");
            _mediator.GridGroupCollectionController.Return(gridList);
            HighlightNextSet(grinderOwnerIdentityId);
        }

        private void ClearReusableImportantBlockDictionary()
        {
            foreach (var kvp in _reusableImportantBlocksDictionary)
            {
                kvp.Value.Clear();
            }
        }

        private void HighlightNextSet(long grinderOwnerIdentityId)
        {
            BlockType type = BlockType.None;
            if (_reusableImportantBlocksDictionary[BlockType.Control].Count > 0)
            {
                type = BlockType.Control;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Medical].Count > 0 && _mediator.DefaultSettings.UseMedicalGroup.Current)
            {
                type = BlockType.Medical;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Weapon].Count > 0 && _mediator.DefaultSettings.UseWeaponGroup.Current)
            {
                type = BlockType.Weapon;
            }
            else if (_reusableImportantBlocksDictionary[BlockType.Trap].Count > 0 && _mediator.DefaultSettings.UseTrapGroup.Current)
            {
                type = BlockType.Trap;
            }

            if (type == BlockType.None) return;

            HighlightBlocks(_reusableImportantBlocksDictionary[type], type, grinderOwnerIdentityId);
        }

        private void HighlightBlocks(HashSet<Block> importantBlocks, BlockType type, long grinderOwnerIdentityId)
        {
            foreach (var block in importantBlocks)
            {
                HighlightBlock(block, type, grinderOwnerIdentityId);
            }
        }
    }
}