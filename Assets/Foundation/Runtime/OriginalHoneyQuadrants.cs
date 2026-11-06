namespace Whitebox.Foundation
{
    // HoneyItemSprite TL/TR/BL/BR, RVAs 0x349ac84, 0x349b4fc, 0x3499fb0, 0x349a748.
    internal readonly struct OriginalHoneyQuadrant
    {
        public const int Empty = 0, Side = 1, Inside = 2, Corner = 3, CornerSide = 4, InsideCorner = 5;
        public readonly int Type, Angle;
        public readonly bool FlipX, FlipY, FirstCorner, Connect;

        public OriginalHoneyQuadrant(int type, bool flipX, bool flipY, int angle = 0, bool firstCorner = false, bool connect = false)
        {
            Type = type;
            Angle = angle;
            FlipX = flipX;
            FlipY = flipY;
            FirstCorner = firstCorner;
            Connect = connect;
        }

        public static OriginalHoneyQuadrant TopLeft(bool top, bool bottom, bool left, bool right, bool diagonal)
        {
            if (top && left) return new OriginalHoneyQuadrant(diagonal ? Inside : InsideCorner, false, false);
            if (!top && !left) return new OriginalHoneyQuadrant(Corner, false, false, firstCorner: diagonal);
            if (top) return bottom ? new OriginalHoneyQuadrant(Side, false, false) : new OriginalHoneyQuadrant(CornerSide, true, false, 90);
            return right ? new OriginalHoneyQuadrant(Side, false, false, -90) : new OriginalHoneyQuadrant(CornerSide, false, false);
        }

        public static OriginalHoneyQuadrant TopRight(bool top, bool bottom, bool left, bool right, bool diagonal)
        {
            if (top && right) return new OriginalHoneyQuadrant(diagonal ? Inside : InsideCorner, true, false);
            if (!top && !right) return new OriginalHoneyQuadrant(Corner, true, false, firstCorner: diagonal);
            if (top) return bottom ? new OriginalHoneyQuadrant(Side, true, false) : new OriginalHoneyQuadrant(CornerSide, true, true, 90);
            return new OriginalHoneyQuadrant(left ? Empty : CornerSide, true, false);
        }

        public static OriginalHoneyQuadrant BottomLeft(bool top, bool bottom, bool left, bool right, bool diagonal)
        {
            if (bottom && left) return new OriginalHoneyQuadrant(diagonal ? Inside : InsideCorner, false, true);
            if (!bottom && !left) return new OriginalHoneyQuadrant(Corner, false, true, firstCorner: diagonal, connect: diagonal);
            if (bottom)
            {
                if (top) return new OriginalHoneyQuadrant(Empty, false, true);
                return diagonal ? new OriginalHoneyQuadrant(CornerSide, false, false, 90) : new OriginalHoneyQuadrant(CornerSide, true, true, -90);
            }
            if (right) return new OriginalHoneyQuadrant(Side, true, true, -90);
            return diagonal ? new OriginalHoneyQuadrant(CornerSide, true, false, -180) : new OriginalHoneyQuadrant(CornerSide, false, true);
        }

        public static OriginalHoneyQuadrant BottomRight(bool top, bool bottom, bool left, bool right, bool diagonal)
        {
            if (bottom && right) return new OriginalHoneyQuadrant(diagonal ? Inside : InsideCorner, true, true);
            if (!bottom && !right) return new OriginalHoneyQuadrant(Corner, true, true, firstCorner: diagonal, connect: diagonal);
            if (bottom)
            {
                if (top) return new OriginalHoneyQuadrant(Empty, true, true);
                return diagonal ? new OriginalHoneyQuadrant(CornerSide, false, true, 90) : new OriginalHoneyQuadrant(CornerSide, true, false, -90);
            }
            return new OriginalHoneyQuadrant(left ? Empty : CornerSide, true, true);
        }
    }
}
