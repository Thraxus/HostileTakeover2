using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using VRage.Utils;

namespace HostileTakeover2.Thraxus.Common.Utilities.Tools.Research
{
    /// <summary>
    /// Dumps all MyWarheadDefinition entries — the only Trap category block type
    /// currently in scope for this mod.
    /// </summary>
    internal static class TrapBlockDefinitionDumper
    {
        private class Record
        {
            public string TypeName;
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
                    var warhead = def as MyWarheadDefinition;
                    if (warhead == null) continue;

                    records.Add(new Record
                    {
                        TypeName    = warhead.GetType().Name,
                        TypeId      = warhead.Id.TypeId.ToString(),
                        SubtypeId   = warhead.Id.SubtypeId.String,
                        Source      = warhead.Context?.ModName ?? "Vanilla",
                        DisplayName = warhead.DisplayNameText ?? string.Empty,
                    });
                }
                records.Sort((a, b) =>
                {
                    int c = string.Compare(a.TypeName, b.TypeName, StringComparison.Ordinal);
                    if (c != 0) return c;
                    return string.Compare(a.SubtypeId, b.SubtypeId, StringComparison.Ordinal);
                });
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"TrapBlockDefinitionDumper.Dump() collection exception: {e}");
            }

            var data = new DataLog("TrapBlockDefinitions");
            try
            {
                data.WriteHeader("TypeName", "TypeId", "SubtypeId", "Source", "DisplayName");
                foreach (var r in records)
                    data.WriteRow(r.TypeName, r.TypeId, r.SubtypeId, r.Source, r.DisplayName);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"TrapBlockDefinitionDumper.Dump() write exception: {e}");
            }
            finally
            {
                data.Close();
            }
        }
    }
}
