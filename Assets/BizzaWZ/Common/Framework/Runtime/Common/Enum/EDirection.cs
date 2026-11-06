

namespace Bizza.Library.Enum
{
      [Obfuz.ObfuzIgnore]
    public enum EDirection
    {
        Mid = 0,

        Right = 1,
        Left = -Right,

        Up = 3,
        Down = -Up,

        LeftUp = Left + Up,
        RightUp = Right + Up,
        LeftDown = Left + Down,
        RightDown = Right + Down,
    }
}
