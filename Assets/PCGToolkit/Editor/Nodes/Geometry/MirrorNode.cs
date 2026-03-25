using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 沿平面镜像几何体（对标 Houdini Mirror SOP）
    /// </summary>
    public class MirrorNode : PCGNodeBase
    {
        public override string Name => "Mirror";
        public override string DisplayName => "Mirror";
        public override string Description => "沿平面镜像几何体，可选保留原始几何体并翻转面绕序";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("origin", PCGPortDirection.Input, PCGPortType.Vector3,
                "Origin", "镜像平面原点", Vector3.zero),
            new PCGParamSchema("normal", PCGPortDirection.Input, PCGPortType.Vector3,
                "Normal", "镜像平面法线（x/y/z 轴或自定义）", Vector3.right),
            new PCGParamSchema("keepOriginal", PCGPortDirection.Input, PCGPortType.Bool,
                "Keep Original", "是否保留原始几何体", true),
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
            Vector3 origin = GetParamVector3(parameters, "origin", Vector3.zero);
            Vector3 normal = GetParamVector3(parameters, "normal", Vector3.right).normalized;
            bool keepOriginal = GetParamBool(parameters, "keepOriginal", true);

            if (geo.Points.Count == 0)
                return SingleOutput("geometry", geo.Clone());

            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.right;

            var mirrored = new PCGGeometry();

            // 镜像顶点：P' = P - 2 * dot(P - origin, normal) * normal
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i];
                float d = Vector3.Dot(p - origin, normal);
                mirrored.Points.Add(p - 2f * d * normal);
            }

            // 翻转面绕序
            foreach (var prim in geo.Primitives)
            {
                var flipped = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    flipped[i] = prim[prim.Length - 1 - i];
                mirrored.Primitives.Add(flipped);
            }

            // 镜像点属性
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
            {
                var newAttr = mirrored.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                if (attr.Name == "N")
                {
                    // 法线也需要镜像
                    foreach (var val in attr.Values)
                    {
                        if (val is Vector3 n)
                        {
                            float d = Vector3.Dot(n, normal);
                            newAttr.Values.Add(n - 2f * d * normal);
                        }
                        else
                            newAttr.Values.Add(val);
                    }
                }
                else
                {
                    newAttr.Values.AddRange(attr.Values);
                }
            }

            // B11 fix: 复制镜像几何体的PrimAttribs
            mirrored.PrimAttribs = geo.PrimAttribs.Clone();

            // B11 fix: 复制镜像几何体的PrimGroups和PointGroups
            foreach (var kvp in geo.PrimGroups)
                mirrored.PrimGroups[kvp.Key] = new HashSet<int>(kvp.Value);
            foreach (var kvp in geo.PointGroups)
                mirrored.PointGroups[kvp.Key] = new HashSet<int>(kvp.Value);

            // B11 fix: 复制镜像几何体的DetailAttribs
            mirrored.DetailAttribs = geo.DetailAttribs.Clone();

            if (!keepOriginal)
                return SingleOutput("geometry", mirrored);

            // 合并原始 + 镜像
            var result = geo.Clone();
            int offset = result.Points.Count;
            int primOffset = result.Primitives.Count;
            result.Points.AddRange(mirrored.Points);

            foreach (var prim in mirrored.Primitives)
            {
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = prim[i] + offset;
                result.Primitives.Add(newPrim);
            }

            // 合并镜像侧的点属性
            foreach (var attr in mirrored.PointAttribs.GetAllAttributes())
            {
                var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    for (int j = 0; j < offset; j++)
                        destAttr.Values.Add(destAttr.DefaultValue);
                }
                destAttr.Values.AddRange(attr.Values);
            }

            // B11 fix: 合并镜像侧的点分组
            foreach (var kvp in mirrored.PointGroups)
            {
                if (!result.PointGroups.ContainsKey(kvp.Key))
                    result.PointGroups[kvp.Key] = new HashSet<int>();
                foreach (int idx in kvp.Value)
                    result.PointGroups[kvp.Key].Add(idx + offset);
            }

            // B11 fix: 合并镜像侧的面属性
            foreach (var attr in mirrored.PrimAttribs.GetAllAttributes())
            {
                var destAttr = result.PrimAttribs.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    for (int j = 0; j < primOffset; j++)
                        destAttr.Values.Add(destAttr.DefaultValue);
                }
                destAttr.Values.AddRange(attr.Values);
            }

            // B11 fix: 合并镜像侧的面分组
            foreach (var kvp in mirrored.PrimGroups)
            {
                if (!result.PrimGroups.ContainsKey(kvp.Key))
                    result.PrimGroups[kvp.Key] = new HashSet<int>();
                foreach (int idx in kvp.Value)
                    result.PrimGroups[kvp.Key].Add(idx + primOffset);
            }

            return SingleOutput("geometry", result);
        }
    }
}
