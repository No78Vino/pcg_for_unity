using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// Facet 操作（对标 Houdini Facet SOP）
    /// 三种模式：
    ///   unique   — 每个面使用独立顶点（断开共享顶点）
    ///   consolidate — 合并重叠顶点（Fuse 的快捷方式）
    ///   computeNormals — 重算法线（flat / smooth）
    /// </summary>
    public class FacetNode : PCGNodeBase
    {
        public override string Name => "Facet";
        public override string DisplayName => "Facet";
        public override string Description => "Unique Points / Consolidate / Compute Normals 三模式";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "操作模式（unique/consolidate/computeNormals）", "unique")
            {
                EnumOptions = new[] { "unique", "consolidate", "computeNormals" }
            },
            new PCGParamSchema("normalMode", PCGPortDirection.Input, PCGPortType.String,
                "Normal Mode", "法线模式（flat/smooth），仅 computeNormals 模式使用", "flat")
            {
                EnumOptions = new[] { "flat", "smooth" }
            },
            new PCGParamSchema("tolerance", PCGPortDirection.Input, PCGPortType.Float,
                "Tolerance", "consolidate 模式的合并距离阈值", 0.0001f),
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
            string mode = GetParamString(parameters, "mode", "unique").ToLower();

            switch (mode)
            {
                case "unique":
                    return SingleOutput("geometry", MakeUnique(geo));
                case "consolidate":
                    float tol = GetParamFloat(parameters, "tolerance", 0.0001f);
                    return SingleOutput("geometry", Consolidate(geo, tol));
                case "computenormals":
                    string normalMode = GetParamString(parameters, "normalMode", "flat").ToLower();
                    return SingleOutput("geometry", ComputeNormals(geo, normalMode));
                default:
                    ctx.LogWarning($"Facet: 未知模式 '{mode}'，使用 unique");
                    return SingleOutput("geometry", MakeUnique(geo));
            }
        }

        private PCGGeometry MakeUnique(PCGGeometry geo)
        {
            var result = new PCGGeometry();

            // B8-1 fix: 复制PrimAttribs（面数量和顺序不变）
            result.PrimAttribs = geo.PrimAttribs.Clone();

            // B8-1 fix: 复制PrimGroups（面索引不变）
            foreach (var kvp in geo.PrimGroups)
                result.PrimGroups[kvp.Key] = new HashSet<int>(kvp.Value);

            // B8-1 fix: 复制DetailAttribs
            result.DetailAttribs = geo.DetailAttribs.Clone();

            int vertOffset = 0;
            foreach (var prim in geo.Primitives)
            {
                int[] newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                {
                    newPrim[i] = result.Points.Count;
                    result.Points.Add(geo.Points[prim[i]]);

                    // B8-1 fix: 为每个新顶点复制对应原始点的PointAttribs
                    foreach (var attr in geo.PointAttribs.GetAllAttributes())
                    {
                        var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                        if (prim[i] < attr.Values.Count)
                            destAttr.Values.Add(attr.Values[prim[i]]);
                        else
                            destAttr.Values.Add(attr.DefaultValue);
                    }
                }
                result.Primitives.Add(newPrim);
                vertOffset += prim.Length;
            }

            return result;
        }

        private PCGGeometry Consolidate(PCGGeometry geo, float tolerance)
        {
            var result = new PCGGeometry();
            float tolSqr = tolerance * tolerance;

            // B8-2 fix: 复制DetailAttribs
            result.DetailAttribs = geo.DetailAttribs.Clone();

            // 对每个旧点找到或创建新的合并后点
            int[] remap = new int[geo.Points.Count];
            for (int i = 0; i < geo.Points.Count; i++)
            {
                int found = -1;
                for (int j = 0; j < result.Points.Count; j++)
                {
                    if ((geo.Points[i] - result.Points[j]).sqrMagnitude < tolSqr)
                    {
                        found = j;
                        break;
                    }
                }
                if (found >= 0)
                {
                    remap[i] = found;
                }
                else
                {
                    remap[i] = result.Points.Count;
                    result.Points.Add(geo.Points[i]);

                    // B8-2 fix: 为新合并点复制原始点的PointAttribs
                    foreach (var attr in geo.PointAttribs.GetAllAttributes())
                    {
                        var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                        if (i < attr.Values.Count)
                            destAttr.Values.Add(attr.Values[i]);
                        else
                            destAttr.Values.Add(attr.DefaultValue);
                    }
                }
            }

            // 重映射面索引，跳过退化面
            // 记录保留的面索引
            var keptPrimIndices = new List<int>();
            foreach (var prim in geo.Primitives)
            {
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = remap[prim[i]];

                // 去重：确保面内无重复索引
                var unique = new HashSet<int>(newPrim);
                if (unique.Count >= 3)
                {
                    result.Primitives.Add(newPrim);
                    keptPrimIndices.Add(geo.Primitives.IndexOf(prim));
                }
            }

            // B8-2 fix: 复制PrimAttribs（只复制保留面的属性）
            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
            {
                var newAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int idx in keptPrimIndices)
                {
                    if (idx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[idx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            // B8-2 fix: 复制PrimGroups（只复制保留面的分组，使用remap更新）
            var primRemap = new Dictionary<int, int>();
            for (int i = 0; i < keptPrimIndices.Count; i++)
            {
                primRemap[keptPrimIndices[i]] = i;
            }
            foreach (var kvp in geo.PrimGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int idx in kvp.Value)
                {
                    if (primRemap.TryGetValue(idx, out int newIdx))
                        newGroup.Add(newIdx);
                }
                if (newGroup.Count > 0)
                    result.PrimGroups[kvp.Key] = newGroup;
            }

            return result;
        }

        private PCGGeometry ComputeNormals(PCGGeometry geo, string normalMode)
        {
            var result = geo.Clone();
            var normals = result.PointAttribs.GetAttribute("N");
            if (normals == null)
                normals = result.PointAttribs.CreateAttribute("N", AttribType.Vector3, Vector3.up);
            normals.Values.Clear();

            if (normalMode == "flat")
            {
                // Flat shading: 先 unique，再对每个面的所有顶点赋面法线
                var unique = MakeUnique(geo);
                var nAttr = unique.PointAttribs.CreateAttribute("N", AttribType.Vector3, Vector3.up);
                nAttr.Values.Clear();
                for (int i = 0; i < unique.Points.Count; i++)
                    nAttr.Values.Add(Vector3.up);

                foreach (var prim in unique.Primitives)
                {
                    if (prim.Length < 3) continue;
                    Vector3 v0 = unique.Points[prim[0]];
                    Vector3 v1 = unique.Points[prim[1]];
                    Vector3 v2 = unique.Points[prim[2]];
                    Vector3 fn = Vector3.Cross(v1 - v0, v2 - v0).normalized;

                    foreach (int idx in prim)
                        nAttr.Values[idx] = fn;
                }

                return unique;
            }
            else
            {
                // Smooth shading: 面积加权顶点法线
                for (int i = 0; i < result.Points.Count; i++)
                    normals.Values.Add(Vector3.zero);

                foreach (var prim in result.Primitives)
                {
                    if (prim.Length < 3) continue;
                    Vector3 v0 = result.Points[prim[0]];
                    Vector3 v1 = result.Points[prim[1]];
                    Vector3 v2 = result.Points[prim[2]];
                    Vector3 fn = Vector3.Cross(v1 - v0, v2 - v0);

                    foreach (int idx in prim)
                        normals.Values[idx] = (Vector3)normals.Values[idx] + fn;
                }

                for (int i = 0; i < result.Points.Count; i++)
                {
                    Vector3 n = (Vector3)normals.Values[i];
                    normals.Values[i] = n.sqrMagnitude > 0.000001f ? n.normalized : Vector3.up;
                }

                return result;
            }
        }
    }
}
