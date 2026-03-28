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
        public override string Description => "合并多个几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        // M1: 10个显式输入端口
        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input0", PCGPortDirection.Input, PCGPortType.Geometry, "Input 0", "输入几何体 0", null, false),
            new PCGParamSchema("input1", PCGPortDirection.Input, PCGPortType.Geometry, "Input 1", "输入几何体 1", null, false),
            new PCGParamSchema("input2", PCGPortDirection.Input, PCGPortType.Geometry, "Input 2", "输入几何体 2", null, false),
            new PCGParamSchema("input3", PCGPortDirection.Input, PCGPortType.Geometry, "Input 3", "输入几何体 3", null, false),
            new PCGParamSchema("input4", PCGPortDirection.Input, PCGPortType.Geometry, "Input 4", "输入几何体 4", null, false),
            new PCGParamSchema("input5", PCGPortDirection.Input, PCGPortType.Geometry, "Input 5", "输入几何体 5", null, false),
            new PCGParamSchema("input6", PCGPortDirection.Input, PCGPortType.Geometry, "Input 6", "输入几何体 6", null, false),
            new PCGParamSchema("input7", PCGPortDirection.Input, PCGPortType.Geometry, "Input 7", "输入几何体 7", null, false),
            new PCGParamSchema("input8", PCGPortDirection.Input, PCGPortType.Geometry, "Input 8", "输入几何体 8", null, false),
            new PCGParamSchema("input9", PCGPortDirection.Input, PCGPortType.Geometry, "Input 9", "输入几何体 9", null, false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry, "Geometry", "合并后的几何体", null, false),
        };

        // M2: 固定端口名顺序
        private static readonly string[] InputPortNames = { "input0", "input1", "input2", "input3", "input4", "input5", "input6", "input7", "input8", "input9" };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var result = new PCGGeometry();

            // M2: 按固定顺序收集非空 geometry
            var geoList = new List<PCGGeometry>();
            foreach (var portName in InputPortNames)
            {
                if (inputGeometries.TryGetValue(portName, out var geo) && geo != null && geo.Points.Count > 0)
                    geoList.Add(geo);
            }

            if (geoList.Count == 0)
            {
                ctx.LogWarning("Merge: 没有有效的输入几何体");
                return SingleOutput("geometry", result);
            }

            // M3: 逐个合并 geometry
            int pointOffset = 0;
            int primOffset = 0;
            foreach (var geo in geoList)
                MergeOne(result, geo, ref pointOffset, ref primOffset);

            return SingleOutput("geometry", result);
        }

        /// <summary>
        /// M3: 合并单个 geometry 到 result
        /// </summary>
        private void MergeOne(PCGGeometry result, PCGGeometry geo, ref int pointOffset, ref int primOffset)
        {
            if (geo == null || geo.Points.Count == 0) return;

            int pointCount = geo.Points.Count;
            int primCount = geo.Primitives.Count;

            // 复制顶点
            result.Points.AddRange(geo.Points);

            // 复制面（调整索引）
            foreach (var prim in geo.Primitives)
            {
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = prim[i] + pointOffset;
                result.Primitives.Add(newPrim);
            }

            // 复制边（调整索引）
            foreach (var edge in geo.Edges)
                result.Edges.Add(new[] { edge[0] + pointOffset, edge[1] + pointOffset });

            // 合并属性
            MergeAttribs(result.PointAttribs, geo.PointAttribs, pointOffset, primOffset);
            MergeAttribs(result.PrimAttribs, geo.PrimAttribs, pointOffset, primOffset);
            MergeAttribs(result.VertexAttribs, geo.VertexAttribs, pointOffset, primOffset);
            MergeDetailAttribs(result, geo);

            // 合并 PointGroups
            foreach (var kvp in geo.PointGroups)
            {
                if (!result.PointGroups.ContainsKey(kvp.Key))
                    result.PointGroups[kvp.Key] = new HashSet<int>();
                foreach (int idx in kvp.Value)
                    result.PointGroups[kvp.Key].Add(idx + pointOffset);
            }

            // M4 fix: 合并 PrimGroups（使用 primOffset 变量）
            foreach (var kvp in geo.PrimGroups)
            {
                if (!result.PrimGroups.ContainsKey(kvp.Key))
                    result.PrimGroups[kvp.Key] = new HashSet<int>();
                foreach (int idx in kvp.Value)
                    result.PrimGroups[kvp.Key].Add(idx + primOffset);
            }

            pointOffset += pointCount;
            primOffset += primCount;
        }

        private void MergeAttribs(AttributeStore dest, AttributeStore src, int pointOffset, int primOffset)
        {
            var processed = new HashSet<string>();

            foreach (var attr in src.GetAllAttributes())
            {
                processed.Add(attr.Name);
                var destAttr = dest.GetAttribute(attr.Name) ?? dest.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                while (destAttr.Values.Count < pointOffset)
                    destAttr.Values.Add(destAttr.DefaultValue);
                destAttr.Values.AddRange(attr.Values);
            }

            // 补齐 dest 有但 src 没有的属性
            foreach (var destAttr in dest.GetAllAttributes())
            {
                if (!processed.Contains(destAttr.Name))
                {
                    for (int i = 0; i < pointOffset; i++)
                        destAttr.Values.Add(destAttr.DefaultValue);
                }
            }
        }

        private void MergeDetailAttribs(PCGGeometry result, PCGGeometry geo)
        {
            foreach (var attr in geo.DetailAttribs.GetAllAttributes())
            {
                var destAttr = result.DetailAttribs.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = result.DetailAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    destAttr.Values.Add(attr.Values.Count > 0 ? attr.Values[0] : attr.DefaultValue);
                }
            }
        }
    }
}
