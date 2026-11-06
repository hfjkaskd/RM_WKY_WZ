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
                bool below = false, above = false;
                try { catalog.Load(0); } catch (ArgumentOutOfRangeException) { below = true; }
                try { catalog.Load(101); } catch (ArgumentOutOfRangeException) { above = true; }
                if (!below || !above) throw new InvalidOperationException("Catalog must reject levels outside the requested scope.");
                var controller = UnityEngine.Object.FindObjectOfType<FoundationController>();
                if (controller == null || controller.Gameplay == null || controller.Gameplay.Session.Name != catalog.GetEntry(1).originalName)
                    throw new InvalidOperationException("Formal first-board entry must use the configured core catalog.");
                report.actual_resource_levels_loaded = catalog.Count; report.raw_cell_definitions_loaded = cells;
                report.rejects_0_and_101 = true; report.formal_first_level_uses_catalog = true; report.passed = true;
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText("live_core_catalog_verification.json", JsonUtility.ToJson(report, true) + "\n"); }
        }
        [Serializable] private sealed class Report
        {
            public bool passed, rejects_0_and_101, formal_first_level_uses_catalog;
            public int actual_resource_levels_loaded, raw_cell_definitions_loaded;
            public string failure;
            public string scope = "The actual configured ScriptableObject catalog loads each of the original100 ordinary levels through the production Resources/JSON reader. Out-of-range requests0/101 are rejected before loading; the actual formal entry uses the same catalog. This verifies resource/configuration availability, not100 playable levels. Core entry opens no account/area storage and uses configured presentation snapshots. The separate core-flow scenario checks actual end/retry/next-level owners and absence of peripheral UI. Remaining obstacle/static composition, complete natural playthroughs, original visual/audio/mobile parity remain pending.";
        }
    }
}
