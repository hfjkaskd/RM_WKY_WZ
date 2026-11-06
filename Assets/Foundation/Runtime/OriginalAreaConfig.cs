using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable] public sealed class OriginalAreaReplayConfig
    { public int order; public float blending; public bool hasAudio, disableHaptic; }
    [Serializable] public sealed class OriginalAreaTaskConfig
    { public int id, price, dependency; public int[] dependencies; public OriginalAreaReplayConfig replayConfig; public float positionOffset; }
    [Serializable] public sealed class OriginalAreaBundleCrcNumbers { public uint ios, android; }

    // Original AreaConfig serialized data. Large textures stay path references
    // so restoring account/task progress does not load every area's artwork.
    public sealed class OriginalAreaConfig : ScriptableObject
    {
        public int areaId;
        public string areaName;
        public OriginalAreaTaskConfig[] tasks;
        public float tallDeviceReplayDelay, positionOffset;
        public bool useGameBackground, useGameBackgroundAlways;
        public string mainTexturePath, shadowTexturePath, distortTexturePath;
        public OriginalAreaBundleCrcNumbers crcNumbers;
    }
}
