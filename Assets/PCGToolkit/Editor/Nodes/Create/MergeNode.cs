using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 合并多个几何体为一个（对标 Houdini Merge SOP）
    /// </summary>
    public class MergeNode : PCGNodeBase
    {
        public override string Name => "Merge";
        public override string DisplayName => "Merge";
        public override string Description => "合并多个几何体为一个";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（支持多输入）", null, required: true, allowMultiple: true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "合并后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var result = new PCGGeometry();
            int pointOffset = 0;

            foreach (var kvp in inputGeometries)
            {
                var geo = kvp.Value;
                if (geo == null || geo.Points.Count == 0) continue;

                // 合并顶点
                int vertexCount = geo.Points.Count;
                result.Points.AddRange(geo.Points);

                // 合并面（调整索引）
                foreach (var prim in geo.Primitives)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                    {
                        newPrim[i] = prim[i] + pointOffset;
                    }
                    result.Primitives.Add(newPrim);
                }

                // 合并边（调整索引）
                foreach (var edge in geo.Edges)
                {
                    result.Edges.Add(new int[] { edge[0] + pointOffset, edge[1] + pointOffset });
                }

                // 合并属性（简化处理，按索引追加）
                MergeAttributes(result.PointAttribs, geo.PointAttribs, vertexCount);
                MergeAttributes(result.PrimAttribs, geo.PrimAttribs, geo.Primitives.Count);

                // 合并分组
                foreach (var group in geo.PointGroups)
                {
                    if (!result.PointGroups.ContainsKey(group.Key))
                        result.PointGroups[group.Key] = new HashSet<int>();
                    foreach (int idx in group.Value)
                        result.PointGroups[group.Key].Add(idx + pointOffset);
                }

                foreach (var group in geo.PrimGroups)
                {
                    if (!result.PrimGroups.ContainsKey(group.Key))
                        result.PrimGroups[group.Key] = new HashSet<int>();
                    foreach (int idx in group.Value)
                        result.PrimGroups[group.Key].Add(idx + result.Primitives.Count - geo.Primitives.Count);
                }

                pointOffset += vertexCount;
            }

            return SingleOutput("geometry", result);
        }

        private void MergeAttributes(AttributeStore dest, AttributeStore src, int elementCount)
        {
            foreach (var attr in src.GetAllAttributes())
            {
                var destAttr = dest.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = dest.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                }
                // 填充默认值以对齐之前的元素
                while (destAttr.Values.Count < dest.Points.Count - elementCount)
                {
                    destAttr.Values.Add(destAttr.DefaultValue);
                }
                destAttr.Values.AddRange(attr.Values);
            }
        }
    }
}