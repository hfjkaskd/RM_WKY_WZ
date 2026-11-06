using System;

namespace UnityExtensions
{
    [Serializable]
    public struct bool2
    {
        public bool x;
        public bool y;

        public bool anyTrue => x || y;
        public bool allTrue => x && y;
    }


    [Serializable]
    public struct bool3
    {
        public bool x;
        public bool y;
        public bool z;

        public bool anyTrue => x || y || z;
        public bool allTrue => x && y && z;

        public static bool3 AllTrue => new bool3() { x = true, y = true, z = true };
    }

    [Serializable]
    public struct bool4
    {
        public bool x;
        public bool y;
        public bool z;
        public bool w;

        public bool anyTrue => x || y || z || w;
        public bool allTrue => x && y && z && w;
    }

} // namespace UnityExtensions