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
            float[] distances = new float[geo.Points.Count];
            for (int i = 0; i < geo.Points.Count; i++)
            {
                distances[i] = Vector3.Dot(geo.Points[i] - origin, normal);
            }

            // 过滤面：记录保留的面索引（用于属性同步）
            var newPrims = new List<int[]>();
            var keptPrimIndices = new List<int>();
            var usedPoints = new HashSet<int>();

            for (int primIdx = 0; primIdx < geo.Primitives.Count; primIdx++)
            {
                var prim = geo.Primitives[primIdx];
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
                    keptPrimIndices.Add(primIdx);
                    foreach (int idx in prim)
                        usedPoints.Add(idx);
                }
            }

            // 构建顶点映射（使用有序遍历确保确定性）
            var indexMap = new Dictionary<int, int>();
            var newPoints = new List<Vector3>();
            var sortedUsedPoints = new List<int>(usedPoints);
            sortedUsedPoints.Sort();
            foreach (int idx in sortedUsedPoints)
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

            // B5 fix: 同步PointAttribs - 按indexMap重建
            var newPointAttribs = new AttributeStore();
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
            {
                var newAttr = newPointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in sortedUsedPoints)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }
            geo.PointAttribs = newPointAttribs;

            // B5 fix: 同步PrimAttribs - 使用keptPrimIndices
            var newPrimAttribs = new AttributeStore();
            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
            {
                var newAttr = newPrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in keptPrimIndices)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }
            geo.PrimAttribs = newPrimAttribs;

            // B5 fix: 同步PointGroups - 使用indexMap更新索引
            var newPointGroups = new Dictionary<string, HashSet<int>>();
            foreach (var kvp in geo.PointGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (var idx in kvp.Value)
                {
                    if (indexMap.TryGetValue(idx, out int newIdx))
                    {
                        newGroup.Add(newIdx);
                    }
                }
                if (newGroup.Count > 0)
                    newPointGroups[kvp.Key] = newGroup;
            }
            geo.PointGroups = newPointGroups;

            // B5 fix: 同步PrimGroups - 使用keptPrimIndices建立映射
            var newPrimGroups = new Dictionary<string, HashSet<int>>();
            var primRemap = new Dictionary<int, int>();
            for (int i = 0; i < keptPrimIndices.Count; i++)
            {
                primRemap[keptPrimIndices[i]] = i;
            }
            foreach (var kvp in geo.PrimGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (var idx in kvp.Value)
                {
                    if (primRemap.TryGetValue(idx, out int newIdx))
                    {
                        newGroup.Add(newIdx);
                    }
                }
                if (newGroup.Count > 0)
                    newPrimGroups[kvp.Key] = newGroup;
            }
            geo.PrimGroups = newPrimGroups;

            // 清空Edges（拓扑已变化）
            geo.Edges.Clear();

            return SingleOutput("geometry", geo);
        }
    }
}