using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    internal static class BlockDefinitionDumper
    {
        private class BlockDefRecord
        {
            public string TypeName;
            public string TypeId;
            public string SubtypeId;
            public string CubeSize;
            public string Source;
            public string DisplayName;
        }

        public static void Dump()
        {
            var records = new List<BlockDefRecord>();
            try
            {
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    var cubeDef = def as MyCubeBlockDefinition;
                    if (cubeDef == null || !cubeDef.Public) continue;
                    records.Add(new BlockDefRecord
                    {
                        TypeName = cubeDef.GetType().Name,
                        TypeId = cubeDef.Id.TypeId.ToString(),
                        SubtypeId = cubeDef.Id.SubtypeId.String,
                        CubeSize = cubeDef.CubeSize.ToString(),
                        Source = cubeDef.Context?.ModName ?? "Vanilla",
                        DisplayName = cubeDef.DisplayNameText ?? string.Empty
                    });
                }
                records.Sort((a, b) =>
                {
                    int c = string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal);
                    if (c != 0) return c;
                    c = string.Compare(a.TypeId, b.TypeId, StringComparison.Ordinal);
                    if (c != 0) return c;
                    c = string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.Ordinal);
                    if (c != 0) return c;
                    return string.Compare(a.CubeSize, b.CubeSize, StringComparison.Ordinal);
                });
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"BlockDefinitionDumper.Dump() collection exception: {e}");
            }

            var data = new DataLog("BlockDefinitionResearch");
            try
            {
                data.WriteHeader("TypeName", "TypeId", "SubtypeId", "CubeSize", "Source", "DisplayName");
                foreach (var r in records)
                    data.WriteRow(r.TypeName, r.TypeId, r.SubtypeId, r.CubeSize, r.Source, r.DisplayName);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"BlockDefinitionDumper.Dump() write exception: {e}");
            }
            finally
            {
                data.Close();
            }
        }
    }
}
