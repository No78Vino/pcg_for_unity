using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 将几何体复制到点上（对标 Houdini CopyToPoints SOP）
    /// </summary>
    public class CopyToPointsNode : PCGNodeBase
    {
        public override string Name => "CopyToPoints";
        public override string DisplayName => "Copy To Points";
        public override string Description => "将源几何体复制到目标点的每个位置上";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("source", PCGPortDirection.Input, PCGPortType.Geometry,
                "Source", "要复制的源几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target Points", "目标点集", null, required: true),
            new PCGParamSchema("usePointOrient", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Orient", "使用点的 orient 属性控制旋转", true),
            new PCGParamSchema("usePointScale", PCGPortDirection.Input, PCGPortType.Bool,
                "Use Point Scale", "使用点的 pscale 属性控制缩放", true),
            new PCGParamSchema("pack", PCGPortDirection.Input, PCGPortType.Bool,
                "Pack", "是否将副本打包为实例", false),
            new PCGParamSchema("transferAttributes", PCGPortDirection.Input, PCGPortType.String,
                "Transfer Attributes", "要传递的属性名（逗号分隔，如 variant,height）", ""),
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
            var source = GetInputGeometry(inputGeometries, "source").Clone();
            var target = GetInputGeometry(inputGeometries, "target").Clone();
            bool usePointOrient = GetParamBool(parameters, "usePointOrient", true);
            bool usePointScale = GetParamBool(parameters, "usePointScale", true);
            string transferAttrs = GetParamString(parameters, "transferAttributes", "");

            // 解析要传递的属性列表
            var transferAttrList = new List<string>();
            if (!string.IsNullOrEmpty(transferAttrs))
            {
                foreach (var attr in transferAttrs.Split(','))
                {
                    var trimmed = attr.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                        transferAttrList.Add(trimmed);
                }
            }

            var result = new PCGGeometry();

            if (source.Points.Count == 0)
            {
                ctx.LogWarning("CopyToPoints: 源几何体为空");
                return SingleOutput("geometry", result);
            }

            if (target.Points.Count == 0)
            {
                ctx.LogWarning("CopyToPoints: 目标点集为空");
                return SingleOutput("geometry", result);
            }

            // 获取属性
            PCGAttribute orientAttr = null;
            PCGAttribute scaleAttr = null;

            if (usePointOrient)
                orientAttr = target.PointAttribs.GetAttribute("orient");
            if (usePointScale)
                scaleAttr = target.PointAttribs.GetAttribute("pscale");

            // 获取要传递的属性
            var transferAttrData = new List<(string name, PCGAttribute attr)>();
            foreach (var attrName in transferAttrList)
            {
                var attr = target.PointAttribs.GetAttribute(attrName);
                if (attr != null)
                    transferAttrData.Add((attrName, attr));
            }

            // B10 fix: 复制源几何体的DetailAttribs
            result.DetailAttribs = source.DetailAttribs.Clone();

            // 对每个目标点复制源几何体
            for (int pointIdx = 0; pointIdx < target.Points.Count; pointIdx++)
            {
                Vector3 position = target.Points[pointIdx];
                Quaternion rotation = Quaternion.identity;
                float scale = 1f;

                // 从属性读取旋转
                if (orientAttr != null && pointIdx < orientAttr.Values.Count)
                {
                    var orientVal = orientAttr.Values[pointIdx];
                    if (orientVal is Vector3 euler)
                    {
                        rotation = Quaternion.Euler(euler);
                    }
                    else if (orientVal is Vector4 quat)
                    {
                        rotation = new Quaternion(quat.x, quat.y, quat.z, quat.w);
                    }
                    else if (orientVal is Quaternion q)
                    {
                        rotation = q;
                    }
                }

                // 从属性读取缩放
                if (scaleAttr != null && pointIdx < scaleAttr.Values.Count)
                {
                    scale = System.Convert.ToSingle(scaleAttr.Values[pointIdx]);
                }

                // 计算顶点偏移
                int vertexOffset = result.Points.Count;
                int pointOffsetForAttribs = result.Points.Count;

                // B10 fix: 复制变换后的顶点，同时复制源几何体的PointAttribs
                foreach (int srcIdx in Enumerable.Range(0, source.Points.Count))
                {
                    Vector3 srcPoint = source.Points[srcIdx];
                    Vector3 transformed = rotation * (srcPoint * scale) + position;
                    result.Points.Add(transformed);

                    // 复制源几何体的点属性
                    foreach (var attr in source.PointAttribs.GetAllAttributes())
                    {
                        var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                        if (destAttr == null)
                        {
                            destAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                            // 补齐前面已添加的点的默认值
                            for (int j = 0; j < pointOffsetForAttribs; j++)
                                destAttr.Values.Add(attr.DefaultValue);
                        }
                        if (srcIdx < attr.Values.Count)
                            destAttr.Values.Add(attr.Values[srcIdx]);
                        else
                            destAttr.Values.Add(attr.DefaultValue);
                    }
                }

                // B10 fix: 复制源几何体的面（调整索引）
                int primOffsetForAttribs = result.Primitives.Count;
                foreach (var srcPrim in source.Primitives)
                {
                    var newPrim = new int[srcPrim.Length];
                    for (int i = 0; i < srcPrim.Length; i++)
                    {
                        newPrim[i] = srcPrim[i] + vertexOffset;
                    }
                    result.Primitives.Add(newPrim);
                }

                // B10 fix: 复制源几何体的面属性
                foreach (var attr in source.PrimAttribs.GetAllAttributes())
                {
                    var destAttr = result.PrimAttribs.GetAttribute(attr.Name);
                    if (destAttr == null)
                    {
                        destAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                        // 补齐前面已添加的面的默认值
                        for (int j = 0; j < primOffsetForAttribs; j++)
                            destAttr.Values.Add(attr.DefaultValue);
                    }
                    destAttr.Values.AddRange(attr.Values);
                }

                // B10 fix: 复制源几何体的面分组（索引需要加上primOffset）
                foreach (var kvp in source.PrimGroups)
                {
                    if (!result.PrimGroups.ContainsKey(kvp.Key))
                        result.PrimGroups[kvp.Key] = new HashSet<int>();
                    foreach (int idx in kvp.Value)
                        result.PrimGroups[kvp.Key].Add(idx + primOffsetForAttribs);
                }

                // 为这个副本的所有点写入 @copynum 属性
                var copynumAttr = result.PointAttribs.GetAttribute("copynum");
                if (copynumAttr == null)
                {
                    copynumAttr = result.PointAttribs.CreateAttribute("copynum", AttribType.Float, 0f);
                    for (int j = 0; j < pointOffsetForAttribs; j++)
                        copynumAttr.Values.Add(0f);
                }
                for (int i = 0; i < source.Points.Count; i++)
                    copynumAttr.Values.Add((float)pointIdx);

                // 传递目标点的属性到副本的所有点（写入 PointAttribs）
                foreach (var (attrName, attr) in transferAttrData)
                {
                    if (pointIdx < attr.Values.Count)
                    {
                        var pointAttr = result.PointAttribs.GetAttribute(attrName);
                        if (pointAttr == null)
                        {
                            pointAttr = result.PointAttribs.CreateAttribute(attrName, attr.Type, attr.DefaultValue);
                            // 补齐前面已添加的点的默认值
                            for (int j = 0; j < pointOffsetForAttribs; j++)
                                pointAttr.Values.Add(attr.DefaultValue);
                        }
                        // 为这个副本的每个点写入对应目标点的属性值
                        for (int i = 0; i < source.Points.Count; i++)
                            pointAttr.Values.Add(attr.Values[pointIdx]);
                    }
                }

                // B10 fix: 复制源几何体的点分组（索引需要加上vertexOffset）
                foreach (var kvp in source.PointGroups)
                {
                    if (!result.PointGroups.ContainsKey(kvp.Key))
                        result.PointGroups[kvp.Key] = new HashSet<int>();
                    foreach (int idx in kvp.Value)
                        result.PointGroups[kvp.Key].Add(idx + vertexOffset);
                }
            }

            ctx.Log($"CopyToPoints: 复制了 {target.Points.Count} 个实例，共 {result.Points.Count} 个顶点");
            return SingleOutput("geometry", result);
        }
    }
}