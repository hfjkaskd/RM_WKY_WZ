using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Whitebox.Foundation.Editor
{
    public static class CellViewVerification
    {
        [Serializable] private sealed class Report
        {
            public bool passed = true;
            public int original_sprite_paths = 13;
            public int formal_cell_views, available_cell_pool, visible_border_parts;
            public bool actual_transform_positions, freeze_countdown_completed;
            public bool stencil_rendering_verified;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope = "Formal fixed-grid normal CellView/CellBorder initialization through the native 100-cell pool, original hierarchy/materials/settings, all cell world/local Transform queries, boundary geometry, real frame-driven freeze completion, plus original mask sorting/reset and 13 cell sprite resources. Full water/shifter/static masks, staged application/session setup and player-driven resolution/fall/refill remain pending.";
        }
        private static Report report;
        private static OriginalInitialBoardView board;
        private static OriginalGridCell frozenCell;
        private static float started;
        private static int startFrame;

        [MenuItem("Recovery/Verify Cell View (Play Mode)")]
        public static void Run()
        {
            if (!EditorApplication.isPlaying || frozenCell != null) throw new InvalidOperationException("Run once in Play mode.");
            GameObject instance = null;
            try
            {
                var prefab = Resources.Load<GameObject>("Recovered/GameObject/CellPrefab");
                Require(prefab != null, "Original CellPrefab resource");
                instance = Object.Instantiate(prefab);
                var mask = instance.GetComponentInChildren<OriginalCellMask>(true);
                Require(mask != null && mask.settings != null, "Restored native mask component and settings");
                Require(mask.readMask.name == "Read" && mask.writeMask.name == "Write", "Original renderer references");
                Require(mask.readMask.sharedMaterial != null && mask.writeMask.sharedMaterial != null, "Original mask materials");
                Require(mask.readMask.sharedMaterial.shader.isSupported && mask.writeMask.sharedMaterial.shader.isSupported, "Supported original mask shaders");
                CellStencilVerification.Verify(mask);
                Require(mask.readMask.sprite != null && mask.readMask.sprite == mask.writeMask.sprite, "Original shared pixel sprite");
                mask.SetSorting(new OriginalSwapSorting(mask.settings.writeMaskLayer, 27, true));
                Require(mask.writeMask.sortingOrder == 26 && mask.readMask.sortingOrder == 27, "Writer precedes reader");
                mask.SetWriteMaskSortingOrder(new OriginalSwapSorting(mask.settings.readMaskLayer, 11, true));
                Require(mask.writeMask.sortingLayerID == mask.settings.writeMaskLayer && mask.writeMask.sortingOrder == 11 && mask.readMask.sortingOrder == 27, "Preserve current writer layer");
                mask.Enable(false);
                Require(!mask.gameObject.activeSelf, "Disable mask object");
                RequireMaskPose(mask);
                mask.Enable(true);
                Require(mask.gameObject.activeSelf, "Reenable mask object");
                RequireMaskPose(mask);

                var assets = AssetDatabase.LoadAssetAtPath<OriginalCellAssets>("Assets/Foundation/Settings/CellAssets.asset");
                Require(assets != null && assets.spritePaths.Length == 13, "Original CellAssets sprite array");
                for (int i = 0; i < assets.spritePaths.Length; i++) Require(Resources.Load<Sprite>(assets.spritePaths[i]) != null, "Cell sprite resource " + i);
                Require(assets.GetOnePixel() == mask.readMask.sprite, "Pixel asset matches original prefab");
                Require(assets.GetCornerConnectFadeOutMask(0) == assets.GetCornerFadeOutMask(0), "Native shared narrow corner slot");
                Require(assets.GetCornerConnectFadeOutMask(-1) == Resources.Load<Sprite>(assets.spritePaths[12]), "Native nonzero wide corner slot");
                SpriteRenderer part = null, fill = null;
                foreach (var r in instance.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r.name == "TopBorder") part = r;
                    else if (r.name == "TopLeftFill") fill = r;
                }
                Require(part != null && fill != null, "Original side and fill renderer topology");
                var settings = mask.settings;
                part.size = settings.sideBorderSize;
                OriginalCellBorderGeometry.PrepareTopBottom(part, new Vector3(0f,.5f,2f), false,false,false,false,settings);
                Require(Approximately(part.size.x, .88571376f) && Approximately(part.size.y,.3f), "Both native outer end trims");
                Require(part.transform.localPosition == new Vector3(0f,.5f,2f), "Symmetric side center and preserved z");
                part.size = settings.sideBorderSize;
                OriginalCellBorderGeometry.PrepareLeftRight(part,new Vector3(.5f,0f,3f),true,true,true,false,settings);
                Require(part.size == new Vector2(.5f,.3f) && part.transform.localPosition == new Vector3(.5f,-.25f,3f), "Vertical native inner trim and axis");
                fill.flipX = false; fill.flipY = true; fill.enabled = false; part.enabled = false;
                OriginalCellBorderGeometry.PrepareCorner(part,fill,true,true,false,settings,assets,null,false);
                Require(fill.enabled && part.enabled && !part.flipX && part.flipY && fill.color == settings.mainColor && part.sprite == assets.GetInnerCornerPart(), "Normal inner corner and main color");
                OriginalCellBorderGeometry.PrepareCorner(part,fill,false,false,true,settings,assets,null,false);
                Require(fill.flipX && part.flipX && part.flipY && fill.color == settings.altColor && part.sprite == assets.GetInnerCornerPart(), "Diagonal corner fill flip and alternate color");
                OriginalCellBorderGeometry.PrepareCorner(part,fill,false,false,false,settings,assets,null,false);
                Require(!part.flipX && !part.flipY && part.sprite == assets.GetOuterCornerPart(), "Original outer corner sprite and inverted flips");
                board = Object.FindObjectOfType<OriginalInitialBoardView>();
                Require(board != null && board.Grid != null, "Formal running board");
                report = new Report { formal_cell_views = board.CreatedCellViewCount, available_cell_pool = board.Presentation.Pool.GetPoolLength(0), stencil_rendering_verified = true };
                Require(report.formal_cell_views == board.Grid.Width * board.Grid.Height && report.formal_cell_views + report.available_cell_pool == 100, "Native 100-cell pool with every grid cell bound");
                int visibleParts = 0;
                for (int x = 0; x < board.Grid.Width; x++)
                for (int y = 0; y < board.Grid.Height; y++)
                {
                    var cell = board.Grid.GetCell(x,y);
                    var view = cell.CellView;
                    Require(view != null && ReferenceEquals(view.ViewDelegate,cell) && view.transform.parent == board.Presentation.CellParent, "Formal cell delegate/parent");
                    Require(view.transform.localPosition == new Vector3(x,y,0) && view.transform.localScale == Vector3.one, "Native cell initialization pose");
                    Require(cell.GetViewLocalPosition() == view.transform.localPosition && cell.GetViewPosition() == view.transform.position, "Actual cell position queries");
                    Require(view.front.color == settings.altColor && view.background.color == settings.mainColor && view.front.gameObject.activeSelf && view.background.gameObject.activeSelf, "Native formal cell colors/visibility");
                    Require(view.mask.gameObject.activeSelf == cell.IsFillingCell, "Native fill mask state");
                    foreach (var r in view.border.borderRenderers) if (r.gameObject.activeSelf && r.enabled && r.sprite != null) visibleParts++;
                }
                report.visible_border_parts = visibleParts;
                Require(visibleParts == 2*(board.Grid.Width+board.Grid.Height)+4, "Rectangle outer sides and four corners");
                var actualCell = board.Grid.GetCell(0,0);
                var parent = board.Presentation.CellParent;
                Vector3 before = actualCell.GetViewPosition(), parentPosition = parent.position;
                Vector3 displacement = new Vector3(.125f,-.25f,.5f);
                try
                {
                    parent.position = parentPosition + displacement;
                    Require(actualCell.GetViewPosition() == before + displacement, "Cell query follows transformed parent, including world Z");
                }
                finally { parent.position = parentPosition; }
                report.actual_transform_positions = true;
                frozenCell = board.Grid.GetCell(4,6);
                Require(!frozenCell.IsFallFrozen && !frozenCell.FreezeCountdown.IsCounting, "Unfrozen formal cell");
                frozenCell.FreezeFallForDuration(.05f);
                Require(frozenCell.IsFallFrozen && frozenCell.FreezeCountdown.IsCounting, "Model starts view-owned freeze countdown");
                startFrame = board.Clock.FrameCount; started = Time.realtimeSinceStartup;
                EditorApplication.update += Tick;
            }
            catch { Cleanup(); throw; }
            finally { if (instance != null) Object.Destroy(instance); }
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying) { Cleanup(); return; }
                if (frozenCell.IsFallFrozen || frozenCell.FreezeCountdown.IsCounting)
                {
                    Require(Time.realtimeSinceStartup-started < 3f, "Formal CellView frame-driven freeze completes");
                    return;
                }
                Require(board.Clock.FrameCount > startFrame && OriginalLevelClock.QuantizeDelta(Time.deltaTime) > 0f && frozenCell.FreezeCountdown.Remaining == 0f, "Actual level frame delta and countdown completion");
                report.freeze_countdown_completed = true;
                File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "cell_view_unity_verification.json"), JsonUtility.ToJson(report, true));
                Cleanup(); Debug.Log("Cell view Unity verification passed.");
            }
            catch { Cleanup(); throw; }
        }
        private static void Cleanup() { EditorApplication.update -= Tick; frozenCell = null; board = null; }
        private static void RequireMaskPose(OriginalCellMask mask)
        {
            Require(mask.readMask.sortingLayerID == mask.settings.readMaskLayer && mask.writeMask.sortingLayerID == mask.settings.writeMaskLayer && mask.readMask.sortingOrder == -1 && mask.writeMask.sortingOrder == -1, "Native mask default sorting");
            Require(mask.readMask.size == new Vector2(1.05f,1f) && mask.writeMask.size == mask.readMask.size && mask.readMask.transform.localPosition == Vector3.zero && mask.writeMask.transform.localPosition == Vector3.zero, "Native mask reset size and pose");
        }
        private static bool Approximately(float a,float b) => Mathf.Abs(a-b) < .000001f;
        private static void Require(bool value,string label) { if (!value) throw new InvalidOperationException(label); }
    }
}
