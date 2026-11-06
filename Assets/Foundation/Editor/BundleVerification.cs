using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Whitebox.Authoring
{
    public static class BundleVerification
    {
        [Serializable] private sealed class Inventory { public Entry[] entries; }
        [Serializable] private sealed class Entry
        {
            public string name;
            public string sourcePath;
            public uint expectedCrc;
            public uint actualCrc;
            public bool readable;
            public bool matches;
        }
        public static void Run()
        {
            var file = Path.GetFullPath("../reports/manifest_bundle_inventory.json");
            var inventory = JsonUtility.FromJson<Inventory>(File.ReadAllText(file));
            int matched = 0;
            foreach (var entry in inventory.entries)
            {
                entry.readable = File.Exists(entry.sourcePath) && BuildPipeline.GetCRCForAssetBundle(entry.sourcePath, out entry.actualCrc);
                entry.matches = entry.readable && entry.actualCrc == entry.expectedCrc;
                if (entry.matches) matched++;
            }
            File.WriteAllText(Path.GetFullPath("../reports/manifest_bundle_crc_validation.json"), JsonUtility.ToJson(inventory, true));
            Debug.Log("BUNDLE_CRC_MATCHED " + matched + "/" + inventory.entries.Length);
        }
    }
}
