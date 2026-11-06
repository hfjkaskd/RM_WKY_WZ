using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalCellBorderSprites
    {
        Sprite GetInnerCornerPart();
        Sprite GetOuterCornerPart();
    }

    public static class OriginalCellBorderGeometry
    {
        // CellBorder.PrepareTopBottom 0x344511c / PrepareLeftRight 0x3445448.
        // Each trim reads and writes the renderer size separately, as in native.
        public static void PrepareTopBottom(SpriteRenderer part, Vector3 position,
            bool neighbor1, bool neighbor2, bool neighbor3, bool neighbor4, OriginalCellViewSettings settings)
        {
            PrepareSide(part, position, neighbor1, neighbor2, neighbor3, neighbor4, settings, false);
        }

        public static void PrepareLeftRight(SpriteRenderer part, Vector3 position,
            bool neighbor1, bool neighbor2, bool neighbor3, bool neighbor4, OriginalCellViewSettings settings)
        {
            PrepareSide(part, position, neighbor1, neighbor2, neighbor3, neighbor4, settings, true);
        }

        private static void PrepareSide(SpriteRenderer part, Vector3 position,
            bool n1, bool n2, bool n3, bool n4, OriginalCellViewSettings settings, bool vertical)
        {
            if (!n3 && !n4)
            {
                if (!n1) Trim(part, ref position, settings.outerCornerSizeReduction, settings.outerCornerHalfOffset, vertical);
                if (!n2) Trim(part, ref position, settings.outerCornerSizeReduction, -settings.outerCornerHalfOffset, vertical);
            }
            else
            {
                if (n3) Trim(part, ref position, settings.innerCornerSizeReduction, settings.innerCornerHalfOffset, vertical);
                if (n4) Trim(part, ref position, settings.innerCornerSizeReduction, -settings.innerCornerHalfOffset, vertical);
                if (n3 && !n4 && !n2) Trim(part, ref position, settings.outerCornerSizeReduction, -settings.outerCornerHalfOffset, vertical);
                if (!n3 && n4 && !n1) Trim(part, ref position, settings.outerCornerSizeReduction, settings.outerCornerHalfOffset, vertical);
            }
            part.transform.localPosition = position;
        }

        private static void Trim(SpriteRenderer part, ref Vector3 position, Vector2 reduction, float offset, bool vertical)
        {
            part.size = part.size - reduction;
            if (vertical) position.y += -offset;
            else position.x += offset;
        }

        // CellBorder.PrepareCorner 0x3445774. Sprite lookup deliberately follows
        // the renderer changes so failures preserve the native partial state.
        public static void PrepareCorner(SpriteRenderer part, SpriteRenderer fill,
            bool neighbor1, bool neighbor2, bool corner, OriginalCellViewSettings settings,
            IOriginalCellBorderSprites cellAssets, IOriginalCellBorderSprites waterAssets, bool isWaterLevel)
        {
            bool flip = fill.flipX;
            part.flipX = !flip;
            part.flipY = !fill.flipY;
            if (neighbor1 && neighbor2 && !corner)
            {
                fill.enabled = true;
                fill.color = settings.mainColor;
                part.enabled = true;
                part.flipX = fill.flipX;
                part.flipY = fill.flipY;
                part.sprite = (isWaterLevel ? waterAssets : cellAssets).GetInnerCornerPart();
            }
            else if (!neighbor1 && !neighbor2)
            {
                if (!corner)
                {
                    part.enabled = true;
                    part.sprite = (isWaterLevel ? waterAssets : cellAssets).GetOuterCornerPart();
                }
                else
                {
                    fill.enabled = true;
                    fill.flipX = !fill.flipX;
                    fill.color = settings.altColor;
                    part.enabled = true;
                    part.flipY = fill.flipY;
                    part.sprite = (isWaterLevel ? waterAssets : cellAssets).GetInnerCornerPart();
                }
            }
        }
    }
}
