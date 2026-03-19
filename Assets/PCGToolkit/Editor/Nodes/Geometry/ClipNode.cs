using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 用平面裁剪几何体（对标 Houdini Clip SOP）
    /// </summary>
    public class ClipNode : PCGNodeBase
    {
        public override string Name => "Clip";
        public override string DisplayName => "Clip";
        public override string Description => "用一个平面裁剪几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "裁剪平面原点", Vector3.zero),
            new PCGParamSchema("normal", PCGPortDirection.Input, PCGPortType.Vector3,
                "Normal", "裁剪平面法线", Vector3.up),
            new PCGParamSchema("keepAbove", PCGPortDirection.Input, PCGPortType.Bool,
                "Keep Above", "保留法线方向侧", true),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 normal = GetParamVector3(parameters, "normal", Vector3.up).normalized;
            bool keepAbove = GetParamBool(parameters, "keepAbove", true);

            if (geo.Points.Count == 0)
            {
                return SingleOutput("geometry", geo);
            }

            // 计算每个顶点到平面的有符号距离
            // dist > 0: 在法线方向一侧
            // dist < 0: 在法线反方向一侧
            // dist = 0: 在平面上
            float[] distances = new float[geo.Points.Count];
            for (int i = 0; i < geo.Points.Count; i++)
            {
                distances[i] = Vector3.Dot(geo.Points[i] - origin, normal);
            }

            // 过滤面：保留所有顶点都在正确一侧的面
            var newPrims = new List<int[]>();
            var usedPoints = new HashSet<int>();

            foreach (var prim in geo.Primitives)
            {
                bool keepPrim = true;
                foreach (int idx in prim)
                {
                    bool isAbove = distances[idx] >= 0;
                    if (isAbove != keepAbove)
                    {
                        keepPrim = false;
                        break;
                    }
                }

                if (keepPrim)
                {
                    newPrims.Add((int[])prim.Clone());
                    foreach (int idx in prim)
                        usedPoints.Add(idx);
                }
            }

            // 构建顶点映射
            var indexMap = new Dictionary<int, int>();
            var newPoints = new List<Vector3>();
            foreach (int idx in usedPoints)
            {
                indexMap[idx] = newPoints.Count;
                newPoints.Add(geo.Points[idx]);
            }

            // 更新面索引
            for (int i = 0; i < newPrims.Count; i++)
            {
                for (int j = 0; j < newPrims[i].Length; j++)
                {
                    newPrims[i][j] = indexMap[newPrims[i][j]];
                }
            }

            geo.Points = newPoints;
            geo.Primitives = newPrims;

            return SingleOutput("geometry", geo);
        }
    }
}