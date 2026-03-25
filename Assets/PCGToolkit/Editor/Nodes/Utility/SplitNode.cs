using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 按 Group 拆分几何体为 matched/unmatched 两路输出。
    /// 唯一需要双 Geometry 输出的节点。
    /// </summary>
    public class SplitNode : PCGNodeBase
    {
        public override string Name => "Split";
        public override string DisplayName => "Split";
        public override string Description => "按 Group 拆分几何体为 matched 和 unmatched 两路";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "用于拆分的 PrimGroup 名称", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("matched", PCGPortDirection.Output, PCGPortType.Geometry,
                "Matched", "属于指定 Group 的面"),
            new PCGParamSchema("unmatched", PCGPortDirection.Output, PCGPortType.Geometry,
                "Unmatched", "不属于指定 Group 的面"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx, Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string group = GetParamString(parameters, "group", "");

            if (string.IsNullOrEmpty(group) || !geo.PrimGroups.TryGetValue(group, out var groupSet))
            {
                ctx.LogWarning($"Split: Group '{group}' 不存在，全部归入 unmatched");
                return new Dictionary<string, PCGGeometry>
                {
                    { "matched", new PCGGeometry() },
                    { "unmatched", geo.Clone() }
                };
            }

            var matchedPrimIndices = new HashSet<int>(groupSet);

            var matched = ExtractPrims(geo, matchedPrimIndices);
            var unmatchedIndices = new HashSet<int>();
            for (int i = 0; i < geo.Primitives.Count; i++)
                if (!matchedPrimIndices.Contains(i))
                    unmatchedIndices.Add(i);
            var unmatched = ExtractPrims(geo, unmatchedIndices);

            ctx.Log($"Split: group='{group}', matched={matched.Primitives.Count}, unmatched={unmatched.Primitives.Count}");
            return new Dictionary<string, PCGGeometry>
            {
                { "matched", matched },
                { "unmatched", unmatched }
            };
        }

        private PCGGeometry ExtractPrims(PCGGeometry source, HashSet<int> primIndices)
        {
            var result = new PCGGeometry();
            if (primIndices.Count == 0) return result;

            // B14 fix: 复制DetailAttribs
            result.DetailAttribs = source.DetailAttribs.Clone();

            // 收集引用的点
            var usedPoints = new HashSet<int>();
            var sortedPrimIndices = new List<int>(primIndices);
            sortedPrimIndices.Sort();
            foreach (int pi in sortedPrimIndices)
            {
                if (pi < source.Primitives.Count)
                    foreach (int vi in source.Primitives[pi])
                        usedPoints.Add(vi);
            }

            // 建立旧 -> 新索引映射
            var indexMap = new Dictionary<int, int>();
            var sortedUsedPoints = new List<int>(usedPoints);
            sortedUsedPoints.Sort();
            foreach (int oldIdx in sortedUsedPoints)
            {
                indexMap[oldIdx] = result.Points.Count;
                result.Points.Add(source.Points[oldIdx]);
            }

            // 建立旧面索引到新面索引的映射
            var primIndexMap = new Dictionary<int, int>();
            for (int i = 0; i < sortedPrimIndices.Count; i++)
            {
                primIndexMap[sortedPrimIndices[i]] = i;
            }

            // 复制面
            int newPrimIdx = 0;
            foreach (int pi in sortedPrimIndices)
            {
                if (pi >= source.Primitives.Count) continue;
                var prim = source.Primitives[pi];
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = indexMap[prim[i]];
                result.Primitives.Add(newPrim);
                newPrimIdx++;
            }

            // B14 fix: 复制点属性 - 按indexMap重建
            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in sortedUsedPoints)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            // B14 fix: 复制面属性 - 按primIndexMap重建
            foreach (var attr in source.PrimAttribs.GetAllAttributes())
            {
                var newAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in sortedPrimIndices)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            // B14 fix: 复制点分组 - 使用indexMap更新索引
            foreach (var kvp in source.PointGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int idx in kvp.Value)
                {
                    if (indexMap.TryGetValue(idx, out int newIdx))
                        newGroup.Add(newIdx);
                }
                if (newGroup.Count > 0)
                    result.PointGroups[kvp.Key] = newGroup;
            }

            // B14 fix: 复制面分组 - 使用primIndexMap更新索引
            foreach (var kvp in source.PrimGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int idx in kvp.Value)
                {
                    if (primIndexMap.TryGetValue(idx, out int newIdx))
                        newGroup.Add(newIdx);
                }
                if (newGroup.Count > 0)
                    result.PrimGroups[kvp.Key] = newGroup;
            }

            return result;
        }
    }
}
