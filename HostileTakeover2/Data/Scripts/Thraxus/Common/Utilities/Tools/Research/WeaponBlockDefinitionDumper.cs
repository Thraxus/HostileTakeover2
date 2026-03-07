using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    /// <summary>
    /// Dumps all Weapon category block definitions:
    /// MyLargeTurretBaseDefinition (autonomous AI turrets) and
    /// MyOffensiveCombatBlockDefinition (offensive AI combat blocks).
    /// Fixed-mount weapons require a human operator and are out of scope.
    /// </summary>
    internal static class WeaponBlockDefinitionDumper
    {
        private class Record
        {
            public string DefinitionType;
            public string TypeId;
            public string SubtypeId;
            public string Source;
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

                    string definitionType = null;
                    if (def is MyLargeTurretBaseDefinition)
                        definitionType = "MyLargeTurretBaseDefinition";
                    else if (def is MyOffensiveCombatBlockDefinition)
                        definitionType = "MyOffensiveCombatBlockDefinition";

                    if (definitionType == null) continue;

                    records.Add(new Record
                    {
                        DefinitionType = definitionType,
                        TypeId         = cubeDef.Id.TypeId.ToString(),
                        SubtypeId      = cubeDef.Id.SubtypeId.String,
                        Source         = cubeDef.Context?.ModName ?? "Vanilla",
                        DisplayName    = cubeDef.DisplayNameText ?? string.Empty,
                    });
                }
                records.Sort((a, b) =>
                {
                    int c = string.Compare(a.DefinitionType, b.DefinitionType, StringComparison.Ordinal);
                    if (c != 0) return c;
                    return string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.Ordinal);
                });
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"WeaponBlockDefinitionDumper.Dump() collection exception: {e}");
            }

            var data = new DataLog("WeaponBlockDefinitions");
            try
            {
                data.WriteHeader("DefinitionType", "TypeId", "SubtypeId", "Source", "DisplayName");
                foreach (var r in records)
                    data.WriteRow(r.DefinitionType, r.TypeId, r.SubtypeId, r.Source, r.DisplayName);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"WeaponBlockDefinitionDumper.Dump() write exception: {e}");
            }
            finally
            {
                data.Close();
            }
        }
    }
}
