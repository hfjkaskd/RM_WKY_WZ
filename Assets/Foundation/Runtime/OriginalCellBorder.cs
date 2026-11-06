using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCellBorder : MonoBehaviour
    {
        [SerializeField] public OriginalCellView cellView;
        [SerializeField] public SpriteRenderer[] borderRenderers;
        [SerializeField] public SpriteRenderer[] fillRenderers;
        [SerializeField] public SpriteRenderer[] fadeOutMaskRenderers;
        [SerializeField] public OriginalCellViewSettings settings;
        private OriginalCellAssets cellAssets;
        private bool isWaterLevel;

        public void Init(OriginalCellAssets assets, bool water) // 0x3442894.
        {
            cellAssets = assets;
            isWaterLevel = water;
            if (water) throw new NotSupportedException("Water border preparation is not restored.");
        }

        public void HideParts() // 0x3442c14.
        {
            for (int i = 0; i < borderRenderers.Length; i++) borderRenderers[i].gameObject.SetActive(false);
        }

        public void ActivateBorders() // 0x3445a08.
        {
            for (int i = 0; i < borderRenderers.Length; i++) borderRenderers[i].gameObject.SetActive(true);
        }

        public void ShowParts(int gridId, bool shouldSeparateGrids) // 0x34430cc.
        {
            for (int i = 0; i < OriginalCellNeighbors.Sides.Length; i++)
            {
                int index = OriginalCellNeighbors.Sides[i];
                var part = borderRenderers[index];
                part.gameObject.SetActive(true);
                part.enabled = false;
                part.size = settings.sideBorderSize;
                PrepareBorder(index, part, gridId, shouldSeparateGrids);
            }
            for (int i = 0; i < borderRenderers.Length; i++)
            {
                var go = borderRenderers[i].gameObject;
                bool visible = borderRenderers[i].sprite != null && borderRenderers[i].enabled;
                go.SetActive(visible);
            }
        }

        // Native dispatch 0x3443004; preserve each direction's neighbor order.
        public void PrepareBorder(int index, SpriteRenderer part, int gridId, bool separate)
        {
            switch (index)
            {
                case 0: PrepareCorner(part, gridId, separate, 1, 7, 0, 0, true, true); break;
                case 1: PrepareSide(part, gridId, separate, 1, 7, 3, 0, 2, settings.topPosition, false); break;
                case 2: PrepareCorner(part, gridId, separate, 1, 3, 2, 1, false, true); break;
                case 3: PrepareSide(part, gridId, separate, 3, 1, 5, 2, 4, settings.rightPosition, true); break;
                case 4: PrepareCorner(part, gridId, separate, 5, 3, 4, 2, false, false); break;
                case 5: PrepareSide(part, gridId, separate, 5, 7, 3, 6, 4, settings.bottomPosition, false); break;
                case 6: PrepareCorner(part, gridId, separate, 5, 7, 6, 3, true, false); break;
                case 7: PrepareSide(part, gridId, separate, 7, 1, 5, 0, 6, settings.leftPosition, true); break;
            }
        }

        // 0x34450d8 compares the shifter grid, never the board identity.
        public static bool DoesCellExistAndNormal(OriginalGridCell cell, int gridId, bool separate)
        {
            return cell != null && cell.IsNormalCell && (!separate || cell.ShifterGridId == gridId);
        }

        private bool HasNormal(int direction, int gridId, bool separate)
        {
            // Do not cache the delegate/neighbors across native property calls.
            return DoesCellExistAndNormal(cellView.ViewDelegate.Neighbors.Get(direction), gridId, separate);
        }

        private void PrepareSide(SpriteRenderer part, int gridId, bool separate, int direction,
            int n1, int n2, int n3, int n4, Vector3 position, bool vertical)
        {
            if (HasNormal(direction, gridId, separate)) return;
            part.enabled = true;
            bool a = HasNormal(n1, gridId, separate);
            bool b = HasNormal(n2, gridId, separate);
            bool c = HasNormal(n3, gridId, separate);
            bool d = HasNormal(n4, gridId, separate);
            if (vertical) OriginalCellBorderGeometry.PrepareLeftRight(part, position, a, b, c, d, settings);
            else OriginalCellBorderGeometry.PrepareTopBottom(part, position, a, b, c, d, settings);
        }

        private void PrepareCorner(SpriteRenderer part, int gridId, bool separate,
            int n1, int n2, int diagonal, int fillIndex, bool flipX, bool flipY)
        {
            bool a = HasNormal(n1, gridId, separate);
            bool b = HasNormal(n2, gridId, separate);
            bool c = HasNormal(diagonal, gridId, separate);
            var fill = fillRenderers[fillIndex];
            fill.flipX = flipX;
            fill.flipY = flipY;
            fill.enabled = false;
            OriginalCellBorderGeometry.PrepareCorner(part, fill, a, b, c, settings, cellAssets, null, isWaterLevel);
        }

        public void OnRecycle() // 0x3442ab4, ordinary-level branch.
        {
            if (isWaterLevel) throw new NotSupportedException("Water border reset is not restored.");
            cellAssets = null;
            for (int i = 0; i < fadeOutMaskRenderers.Length; i++)
            {
                fadeOutMaskRenderers[i].gameObject.SetActive(false);
                var maskTransform = fadeOutMaskRenderers[i].transform;
                maskTransform.localRotation = Quaternion.identity;
                maskTransform.localPosition = Vector3.zero;
            }
        }
    }
}
