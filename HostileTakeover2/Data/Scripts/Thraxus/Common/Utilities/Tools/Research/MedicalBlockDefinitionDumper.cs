using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    /// <summary>
    /// Dumps all block definitions that fall into the Medical category:
    /// MyMedicalRoomDefinition, MyCryoChamberDefinition, MySurvivalKitDefinition.
    /// There is no common base across these three types so each is cast separately.
    /// The DefinitionType column indicates which cast matched.
    /// </summary>
    internal static class MedicalBlockDefinitionDumper
    {
        private class Record
        {
            public string DefinitionType;
            public string TypeId;
            public string SubtypeId;
            public string Source;
            public string DisplayName;
            public string ResourceSinkGroup;
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
                    string resourceSinkGroup = string.Empty;

                    if (def is MyMedicalRoomDefinition)
                    {
                        definitionType = "MyMedicalRoomDefinition";
                    }
                    else if (def is MyCryoChamberDefinition)
                    {
                        var cryo = (MyCryoChamberDefinition)def;
                        definitionType = "MyCryoChamberDefinition";
                        resourceSinkGroup = cryo.ResourceSinkGroup.ToString();
                    }
                    else if (def is MySurvivalKitDefinition)
                    {
                        definitionType = "MySurvivalKitDefinition";
                    }

                    if (definitionType == null) continue;

                    records.Add(new Record
                    {
                        DefinitionType       = definitionType,
                        TypeId               = cubeDef.Id.TypeId.ToString(),
                        SubtypeId            = cubeDef.Id.SubtypeId.String,
                        Source               = cubeDef.Context?.ModName ?? "Vanilla",
                        DisplayName          = cubeDef.DisplayNameText ?? string.Empty,
                        ResourceSinkGroup = resourceSinkGroup,
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
                MyLog.Default.WriteLineAndConsole($"MedicalBlockDefinitionDumper.Dump() collection exception: {e}");
            }

            var data = new DataLog("MedicalBlockDefinitions");
            try
            {
                data.WriteHeader("DefinitionType", "TypeId", "SubtypeId", "Source", "DisplayName", "ResourceSinkGroup");
                foreach (var r in records)
                    data.WriteRow(r.DefinitionType, r.TypeId, r.SubtypeId, r.Source, r.DisplayName, r.ResourceSinkGroup);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"MedicalBlockDefinitionDumper.Dump() write exception: {e}");
            }
            finally
            {
                data.Close();
            }
        }
    }
}
