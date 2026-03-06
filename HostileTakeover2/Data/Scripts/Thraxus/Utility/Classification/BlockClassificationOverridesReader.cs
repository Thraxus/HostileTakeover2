using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Sandbox.ModAPI;

namespace HostileTakeover2.Thraxus.Utility.Classification
{
    // ---------------------------------------------------------------------------
    // DTOs — must be public for XmlSerializer reflection
    // ---------------------------------------------------------------------------

    [XmlRoot("BlockClassificationOverrides")]
    public class BlockClassificationOverridesXml
    {
        public AddSection ControlBlocks  = new AddSection();
        public AddSection MedicalBlocks  = new AddSection();
        public AddSection WeaponBlocks   = new AddSection();
        public AddSection TrapBlocks     = new AddSection();
        public ExcludeSection ExcludedBlocks = new ExcludeSection();
    }

    public class AddSection
    {
        [XmlArray("Add")]
        [XmlArrayItem("Block")]
        public List<string> Add = new List<string>();
    }

    public class ExcludeSection
    {
        [XmlArray("Exclude")]
        [XmlArrayItem("Block")]
        public List<string> Exclude = new List<string>();
    }

    // ---------------------------------------------------------------------------

    internal static class BlockClassificationOverridesReader
    {
        private const string FileName = "BlockClassificationOverrides.xml";

        public static void Read(BlockClassificationData data)
        {
            if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(FileName, typeof(BlockClassificationOverridesReader)))
            {
                WriteTemplate();
                return;
            }

            try
            {
                string xml;
                using (TextReader tr = MyAPIGateway.Utilities.ReadFileInWorldStorage(FileName, typeof(BlockClassificationOverridesReader)))
                    xml = tr.ReadToEnd();

                BlockClassificationOverridesXml overrides =
                    MyAPIGateway.Utilities.SerializeFromXML<BlockClassificationOverridesXml>(xml);

                if (overrides == null) return;

                ApplyAdds(overrides.ControlBlocks.Add, data.ControlBlocks);
                ApplyAdds(overrides.MedicalBlocks.Add, data.MedicalBlocks);
                ApplyAdds(overrides.WeaponBlocks.Add,  data.WeaponBlocks);
                ApplyAdds(overrides.TrapBlocks.Add,    data.TrapBlocks);
                ApplyExcludes(overrides.ExcludedBlocks.Exclude, data);
            }
            catch (Exception)
            {
                // Malformed file — base classification data remains unchanged.
            }
        }

        private static void ApplyAdds(List<string> adds, HashSet<string> set)
        {
            foreach (string key in adds)
            {
                string trimmed = key.Trim();
                if (!string.IsNullOrEmpty(trimmed)) set.Add(trimmed);
            }
        }

        private static void ApplyExcludes(List<string> excludes, BlockClassificationData data)
        {
            foreach (string key in excludes)
            {
                string trimmed = key.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                data.ControlBlocks.Remove(trimmed);
                data.MedicalBlocks.Remove(trimmed);
                data.WeaponBlocks.Remove(trimmed);
                data.TrapBlocks.Remove(trimmed);
            }
        }

        private static void WriteTemplate()
        {
            using (TextWriter w = MyAPIGateway.Utilities.WriteFileInWorldStorage(FileName, typeof(BlockClassificationOverridesReader)))
            {
                w.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
                w.WriteLine("<!--");
                w.WriteLine("  HostileTakeover2 - Block Classification Overrides");
                w.WriteLine();
                w.WriteLine("  Edit this file to add or remove blocks from any category.");
                w.WriteLine("  Changes take effect on the next session load.");
                w.WriteLine();
                w.WriteLine("  To find a block's TypeId/SubtypeId, open BlockClassification.xml.");
                w.WriteLine("  That file is written each session and lists every block currently");
                w.WriteLine("  detected per category.");
                w.WriteLine();
                w.WriteLine("  ControlBlocks / MedicalBlocks / WeaponBlocks / TrapBlocks");
                w.WriteLine("    Place blocks inside <Add> to include them in that category,");
                w.WriteLine("    even if they were not detected automatically.");
                w.WriteLine();
                w.WriteLine("  ExcludedBlocks");
                w.WriteLine("    Place blocks inside <Exclude> to remove them from whichever");
                w.WriteLine("    category they landed in. No need to know which category.");
                w.WriteLine();
                w.WriteLine("  Example:");
                w.WriteLine("    <WeaponBlocks>");
                w.WriteLine("      <Add>");
                w.WriteLine("        <Block>MyObjectBuilder_ConveyorSorter/MyModdedTurret</Block>");
                w.WriteLine("      </Add>");
                w.WriteLine("    </WeaponBlocks>");
                w.WriteLine("    <ExcludedBlocks>");
                w.WriteLine("      <Exclude>");
                w.WriteLine("        <Block>MyObjectBuilder_Warhead/LargeWarhead</Block>");
                w.WriteLine("      </Exclude>");
                w.WriteLine("    </ExcludedBlocks>");
                w.WriteLine("-->");
                w.WriteLine("<BlockClassificationOverrides>");
                w.WriteLine();
                WriteTemplateSection(w, "ControlBlocks");
                WriteTemplateSection(w, "MedicalBlocks");
                WriteTemplateSection(w, "WeaponBlocks");
                WriteTemplateSection(w, "TrapBlocks");
                w.WriteLine("  <ExcludedBlocks>");
                w.WriteLine("    <Exclude>");
                w.WriteLine("      <!-- <Block>TypeId/SubtypeId</Block> -->");
                w.WriteLine("    </Exclude>");
                w.WriteLine("  </ExcludedBlocks>");
                w.WriteLine();
                w.Write("</BlockClassificationOverrides>");
            }
        }

        private static void WriteTemplateSection(TextWriter w, string tag)
        {
            w.WriteLine($"  <{tag}>");
            w.WriteLine("    <Add>");
            w.WriteLine("      <!-- <Block>TypeId/SubtypeId</Block> -->");
            w.WriteLine("    </Add>");
            w.WriteLine($"  </{tag}>");
            w.WriteLine();
        }
    }
}
