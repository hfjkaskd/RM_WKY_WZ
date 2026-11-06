using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalAreaConfigContainer { OriginalAreaConfig LoadAreaConfig(int areaId, bool forceReload = false); }
    public sealed class OriginalAreaConfigContainer : IOriginalAreaConfigContainer
    {
        private readonly Dictionary<int, OriginalAreaConfig> configs = new Dictionary<int, OriginalAreaConfig>();
        private readonly Func<int, OriginalAreaConfig> load;
        public OriginalAreaConfigContainer() : this(LoadFromResources) { }
        public OriginalAreaConfigContainer(Func<int, OriginalAreaConfig> load) { this.load = load; }
        public OriginalAreaConfig LoadAreaConfig(int areaId, bool forceReload = false) // 0x3d1be0c.
        {
            if (configs.TryGetValue(areaId, out var config))
            {
                if (!forceReload) return config;
                configs.Remove(areaId);
            }
            config = load(areaId);
            configs.Add(areaId, config);
            return config;
        }
        private static OriginalAreaConfig LoadFromResources(int areaId) =>
            Resources.Load<OriginalAreaConfig>("Foundation/Areas/Area" + areaId.ToString("00") + "AreaConfig");
    }
}
