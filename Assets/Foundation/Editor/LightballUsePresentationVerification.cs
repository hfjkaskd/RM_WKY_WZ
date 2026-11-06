using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    public static class LightballUsePresentationVerification
    {
        [MenuItem("Recovery/Verify Lightball Use Presentation")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run in the formal Play scene.");
            string reportPath = Path.Combine(Application.dataPath, "../lightball_use_presentation_verification.json");
            var report = new Report();
            try
            {
                var board = Object.FindObjectOfType<OriginalInitialBoardView>();
                var owner = board.Presentation;
                int itemLayer = SortingLayer.NameToID("Item");
                Require(owner.Pool.GetPoolLength(25) == 50 && owner.Pool.GetPoolLength(30) == 5 && owner.Pool.GetPoolLength(29) == 1, "Native requested pool sizes");
                string[] sprites = { "shield", "leaf", "book", "crown", "gem", "gem" };
                int[] colorIndices = { 0, 1, 3, 5, 4, 2 };
                for (int type = 1; type <= 6; type++)
                {
                    var bg = owner.SpawnLightballBackground(1, type, new OriginalSwapSorting(itemLayer, 11000, false));
                    try
                    {
                        Require(bg.colorBg.enabled && bg.whiteBg.enabled && bg.colorBg.sprite == bg.whiteBg.sprite, "Both source background sprites enabled");
                        Require(bg.colorBg.sprite.name == "Items-SpecialItems-LightballItem-lightball_" + sprites[type - 1] + "_bg", "Native sprite-table slot");
                        Require(bg.colorBg.color == bg.spriteColors[colorIndices[type - 1]], "Native six-color jump table");
                        Require(bg.colorBg.transform.localScale == new Vector3(2.3f, 2.3f, 1f), "Original color scale");
                        float scale = type == 5 ? 2.2f : 2.1f;
                        Require(bg.whiteBg.transform.localScale == new Vector3(scale, scale, 1f), "Original white scale");
                        Require(bg.whiteBg.sortingOrder == 11001 && bg.colorBg.sortingOrder == 11000
                            && bg.whiteBg.sortingLayerID == itemLayer && bg.colorBg.sortingLayerID == itemLayer, "Native background sorting offset");
                        bg.transform.localScale = Vector3.one * 2f;
                        bg.transform.localRotation = Quaternion.Euler(0, 0, 15f);
                    }
                    finally { bg.RecycleSelf(); }
                    Require(bg.transform.localScale == Vector3.one && bg.transform.localRotation == Quaternion.identity, "Recycled background resets transform");
                    var effect = owner.Particles.GetGlobalParticle<OriginalLightballEffectedItemExplodeParticles>(29);
                    for (int i = 0; i < effect.particles.Length; i++) effect.particles[i].Clear(false);
                    var point = board.Grid.GetCell(type, 7).GetViewPosition();
                    owner.PlayLightballEffectedExplosion(point, type);
                    for (int i = 0; i < effect.particles.Length; i++)
                    {
                        var ps = effect.particles[i];
                        int expected = (int)ps.emission.GetBurst(0).count.constant;
                        Require(ps.particleCount == expected, "Native explicit burst amount: " + ps.name + ", expected=" + expected
                            + ", actual=" + ps.particleCount + ", active=" + ps.gameObject.activeInHierarchy + ", lifetime=" + ps.main.startLifetime.constant
                            + ", stopped=" + ps.isStopped + ", paused=" + ps.isPaused);
                        var emitted = new ParticleSystem.Particle[expected];
                        Require(ps.GetParticles(emitted) == expected, "Actual Unity particles emitted");
                        Color32 color = effect.particleColors[colorIndices[type - 1]];
                        for (int p = 0; p < expected; p++) Require(emitted[p].startColor.Equals(color), "Native emitted color");
                        // The first source emitter owns the other two as children.
                        // Clear only this emitter so the next assertion reads its live particles.
                        ps.Clear(false); report.emittedParticles += expected;
                    }
                    report.colors++;
                }
                var hidden = owner.SpawnLightballBackground(2, 1, new OriginalSwapSorting(itemLayer, 11000, false));
                try { Require(!hidden.colorBg.enabled && !hidden.whiteBg.enabled, "Special target hides both sprites"); }
                finally { hidden.RecycleSelf(); }
                var use = owner.SpawnLightballUseParticles();
                try
                {
                    use.Play(itemLayer);
                    Require(use.backGlowBg.isPlaying && use.backGlow.isPlaying && use.ring.isPlaying, "Original three use emitters play");
                    Require(use.backGlowBg.GetComponent<ParticleSystemRenderer>().sortingOrder == 11008
                        && use.backGlow.GetComponent<ParticleSystemRenderer>().sortingOrder == 11009
                        && use.ring.GetComponent<ParticleSystemRenderer>().sortingOrder == 11009, "Native use-emitter sorting");
                }
                finally { use.StopEmitting(); }
                Require(owner.Pool.GetPoolLength(25) == 50 && owner.Pool.GetPoolLength(30) == 5, "Owned views return to their actual pools");
                var selected = new HashSet<int>();
                var targeting = new OriginalLightballTargeting(board.Grid, board.Layout, selected);
                int mostCommon = targeting.FindMostCommonItem();
                Require(mostCommon > 0, "Formal first board has an available color");
                targeting.FindItemsByType(mostCommon);
                try
                {
                    Require(targeting.Items.Count > 0, "Native target search reserves actual live items");
                    int previousY = int.MaxValue, previousX = -1;
                    for (int i = 0; i < targeting.Items.Count; i++)
                    {
                        var item = targeting.Items[i]; var cell = item.Mediator.CurrentCell.Owner;
                        Require(item.IsReserved && item.MatchType == mostCommon, "Target reservation/color");
                        Require(cell.Y < previousY || cell.Y == previousY && cell.X > previousX, "Original iterator3 top-row-first order");
                        previousY = cell.Y; previousX = cell.X;
                    }
                    int before = targeting.Items.Count;
                    targeting.FindItemsByType(mostCommon);
                    Require(targeting.Items.Count == before, "Reserved targets cannot be added twice");
                    report.reservedTargets = before;
                }
                finally
                {
                    while (targeting.Items.Count > 0) targeting.RemoveFromItems(targeting.Items[0]);
                }
                selected.UnionWith(new[] { 1, 2, 3, 4, 5, 6 });
                Require(targeting.FindMostCommonItem() == 0, "Shared active-lightball colors are excluded");
                report.passed = true;
                Debug.Log("Lightball use presentation/targeting passed: six colors, native pools, actual emitted particles and reservation release.");
            }
            catch (Exception error) { report.failure = error.ToString(); throw; }
            finally { File.WriteAllText(reportPath, JsonUtility.ToJson(report, true) + "\n"); }
        }
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        [Serializable] private sealed class Report
        {
            public bool passed;
            public string failure;
            public int colors, emittedParticles, reservedTargets;
            public string scope = "Actual formal-scene pools25/29/30, six original sprite/color mappings/scales/sortings, Unity particle burst/color data, view recycle/reset and native targeting reserve/order/release. Does not verify lightball activation, ray-flight/timing, TNT-chain completion, full session startup or rendered-original parity.";
        }
    }
}
