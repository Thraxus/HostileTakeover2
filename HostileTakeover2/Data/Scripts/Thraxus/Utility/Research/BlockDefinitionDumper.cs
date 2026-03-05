using System;
using HostileTakeover2.Thraxus.Common.Utilities.Tools.Logging;
using Sandbox.Definitions;

namespace HostileTakeover2.Thraxus.Utility.Research
{
    internal static class BlockDefinitionDumper
    {
        public static void Dump()
        {
            var log = new Log("BlockDefinitionResearch");
            try
            {
                log.WriteGeneral(nameof(Dump), "Block definition dump starting.");
                foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
                {
                    var cubeDef = def as MyCubeBlockDefinition;
                    if (cubeDef == null || !cubeDef.Public) continue;
                    string source = cubeDef.Context?.ModName ?? "Vanilla";
                    log.WriteGeneral(
                        cubeDef.GetType().Name,
                        $"[{cubeDef.Id.TypeId}] [{cubeDef.Id.SubtypeId.String}] [{cubeDef.CubeSize}] [{source}] [{cubeDef.DisplayNameText}]"
                    );
                }
            }
            catch (Exception e)
            {
                log.WriteException(nameof(Dump), $"Exception during block definition dump: {e}");
            }
            finally
            {
                log.Close(nameof(Dump), "Block definition dump complete.");
            }
        }
    }
}
