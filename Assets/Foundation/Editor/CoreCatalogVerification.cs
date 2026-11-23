using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    public static class CoreCatalogVerification
    {
        public static void Run()
        {
            var report = new Report();
            try
            {
                var settings = AssetDatabase.LoadAssetAtPath<FoundationSettings>("Assets/Foundation/Settings/FoundationSettings.asset");
                var catalog = settings.CoreLevels;
                if (catalog.Count != 100) throw new InvalidOperationException("Expected exactly100 original ordinary levels.");
                int cells = 0;
                for (int number = 1; number <= catalog.Count; number++)
                {
                    var entry = catalog.GetEntry(number); var level = catalog.Load(number);
                    if (entry.number != number || level.Move <= 0 || level.Goals == null || level.Goals.Length == 0)
                        throw new InvalidOperationException("Original level payload is incomplete: " + number);
                    if (level.Grid.Cells == null || level.Grid.Cells.Length != level.Grid.Items.Length)
                        throw new InvalidOperationException("Original cell definitions are incomplete: " + number);
                    cells += level.Grid.Items.Length;
                }
                bool below = false;
                try { catalog.Load(0); } catch (ArgumentOutOfRangeException) { below = true; }
                if (!below) throw new InvalidOperationException("Catalog must reject nonpositive levels.");
                int[] repeatedLevels = { 101, 102, 200, 201, 1000, int.MaxValue };
                int[] resourceLevels = { 1, 2, 100, 1, 100, 47 };
                for (int i = 0; i < repeatedLevels.Length; i++)
                {
                    var entry = catalog.GetEntry(repeatedLevels[i]);
                    var level = catalog.Load(repeatedLevels[i]);
                    if (entry.number != resourceLevels[i] || level.Name != catalog.GetEntry(resourceLevels[i]).originalName)
                        throw new InvalidOperationException("Looped level must load its original board: " + repeatedLevels[i]);
                }
                var controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (controller == null || controller.Gameplay == null || controller.Gameplay.Session.Name != catalog.GetEntry(1).originalName)
                    throw new InvalidOperationException("Formal first-board entry must use the configured core catalog.");
                report.actual_resource_levels_loaded = catalog.Count; report.raw_cell_definitions_loaded = cells;
                report.rejects_0_and_repeats_boards = true; report.formal_first_level_uses_catalog = true; report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText("live_core_catalog_verification.json", JsonUtility.ToJson(report, true) + "\n"); }
        }
        [Serializable] private sealed class Report
        {
            public bool passed, rejects_0_and_repeats_boards, formal_first_level_uses_catalog;
            public int actual_resource_levels_loaded, raw_cell_definitions_loaded;
            public string failure;
            public string scope = "The configured catalog loads all 100 original boards through the production Resources/JSON reader, rejects level 0, and repeats the correct boards for 101, 102, 200, 201, 1000 and int.MaxValue. The formal first-board entry uses the same catalog. This checks resource availability, not complete playable runs. Core-flow and natural-attempt scenarios separately exercise actual win, retry and continuation.";
        }
    }
}
