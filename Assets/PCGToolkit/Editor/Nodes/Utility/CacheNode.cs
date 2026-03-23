using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Utility
{
    public class CacheNode : PCGNodeBase
    {
        public override string Name => "Cache";
        public override string DisplayName => "Cache";
        public override string Description => "缓存几何体到磁盘，避免上游重复计算";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "缓存模式：auto / always_write / always_read / bypass", "auto")
                { EnumOptions = new[] { "auto", "always_write", "always_read", "bypass" } },
            new PCGParamSchema("cacheName", PCGPortDirection.Input, PCGPortType.String,
                "Cache Name", "自定义缓存名称（留空则自动生成）", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string mode = GetParamString(parameters, "mode", "auto");
            string cacheName = GetParamString(parameters, "cacheName", "");

            if (mode == "bypass")
            {
                ctx.Log("Cache: bypass mode, pass-through");
                return SingleOutput("geometry", geo);
            }

            // Compute cache key
            string cacheKey;
            if (!string.IsNullOrEmpty(cacheName))
            {
                cacheKey = "cache_node_" + cacheName;
            }
            else
            {
                cacheKey = PCGCacheManager.ComputeCacheKey("CacheNode", parameters, inputGeometries);
            }

            if (mode == "always_read")
            {
                if (PCGCacheManager.TryGetGeometry(cacheKey, out var cached))
                {
                    ctx.Log($"Cache: always_read hit (key={cacheKey})");
                    return SingleOutput("geometry", cached);
                }
                ctx.LogError($"Cache: always_read mode but cache miss (key={cacheKey})");
                return SingleOutput("geometry", geo);
            }

            if (mode == "always_write")
            {
                if (geo != null && geo.Points.Count > 0)
                {
                    PCGCacheManager.PutGeometry(cacheKey, geo, CachePersistence.Disk,
                        "CacheNode", null, ctx.CurrentNodeId);
                    ctx.Log($"Cache: always_write (key={cacheKey}, points={geo.Points.Count})");
                }
                return SingleOutput("geometry", geo);
            }

            // auto mode
            if (PCGCacheManager.TryGetGeometry(cacheKey, out var autoCache))
            {
                ctx.Log($"Cache: auto hit (key={cacheKey})");
                return SingleOutput("geometry", autoCache);
            }

            if (geo != null && geo.Points.Count > 0)
            {
                PCGCacheManager.PutGeometry(cacheKey, geo, CachePersistence.Disk,
                    "CacheNode", null, ctx.CurrentNodeId);
                ctx.Log($"Cache: auto miss, written to disk (key={cacheKey}, points={geo.Points.Count})");
            }
            else
            {
                ctx.Log("Cache: auto miss, empty geometry, skipped write");
            }

            return SingleOutput("geometry", geo);
        }
    }
}
