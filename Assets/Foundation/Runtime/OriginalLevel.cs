using System;
using UnityEngine;
using Whitebox.Foundation.ConfigData;

namespace Whitebox.Foundation
{
    public static class OriginalLevelReader
    {
        public static FTiledLevel Load(string resourcePath)
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null) throw new InvalidOperationException("Missing level resource: " + resourcePath);
            try
            {
                var level = JsonUtility.FromJson<FTiledLevel>(asset.text);
                if (level == null || level.Grid == null || level.Grid.Width <= 0 || level.Grid.Height <= 0 ||
                    level.Grid.Items == null || level.Grid.Items.Length != level.Grid.Width * level.Grid.Height)
                    throw new InvalidOperationException("Invalid original level grid.");
                return level;
            }
            finally { Resources.UnloadAsset(asset); }
        }
    }
}
