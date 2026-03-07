using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    /// <summary>
    /// Dumps all non-base-game MyConveyorSorterDefinition and MyUpgradeModuleDefinition
    /// entries, exposing their mod context (ModId, ModName) so modded weapon blocks can
    /// be identified and added to the classification system.
    /// Mirrors the two special-case checks in AssignBlock:
    ///   - MyConveyorSorter with a specific ModId (modded turret-style sorters)
    ///   - MyUpgradeModule with SubtypeId "BotSpawner"
    /// </summary>
    internal static class ModdedBlockDefinitionDumper
    {
        private class Record
        {
            public string DefinitionType;
            public string TypeId;
            public string SubtypeId;
            public string ModId;
            public string ModName;
            public string DisplayName;
        }

        public static void Dump()
        {
            var records = new List<Record>();
            try
            {
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    var cubeDef = def as MyCubeBlockDefinition;
                    if (cubeDef == null) continue;
                    if (cubeDef.Context == null || cubeDef.Context.IsBaseGame) continue;

                    string definitionType = null;
                    if (def is MyConveyorSorterDefinition)
                        definitionType = "MyConveyorSorterDefinition";
                    else if (def is MyUpgradeModuleDefinition)
                        definitionType = "MyUpgradeModuleDefinition";

                    if (definitionType == null) continue;

                    records.Add(new Record
                    {
                        DefinitionType = definitionType,
                        TypeId         = cubeDef.Id.TypeId.ToString(),
                        SubtypeId      = cubeDef.Id.SubtypeId.String,
                        ModId          = cubeDef.Context.ModId ?? string.Empty,
                        ModName        = cubeDef.Context.ModName ?? string.Empty,
                        DisplayName    = cubeDef.DisplayNameText ?? string.Empty,
                    });
                }
                records.Sort((a, b) =>
                {
                    int c = string.Compare(a.DefinitionType, b.DefinitionType, StringComparison.Ordinal);
                    if (c != 0) return c;
                    c = string.Compare(a.ModId, b.ModId, StringComparison.Ordinal);
                    if (c != 0) return c;
                    return string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.Ordinal);
                });
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"ModdedBlockDefinitionDumper.Dump() collection exception: {e}");
            }

            var data = new DataLog("ModdedBlockDefinitions");
            try
            {
                data.WriteHeader("DefinitionType", "TypeId", "SubtypeId", "ModId", "ModName", "DisplayName");
                foreach (var r in records)
                    data.WriteRow(r.DefinitionType, r.TypeId, r.SubtypeId, r.ModId, r.ModName, r.DisplayName);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"ModdedBlockDefinitionDumper.Dump() write exception: {e}");
            }
            finally
            {
                data.Close();
            }
        }
    }
}
