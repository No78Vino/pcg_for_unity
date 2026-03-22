using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCGGeometry 与 Unity Mesh 之间的转换工具。
    /// 仅在最终输出阶段使用。
    /// 支持多 Submesh 输出（按 @material 属性或 PrimGroups 分组）。
    /// </summary>
    public static class PCGGeometryToMesh
    {
        /// <summary>
        /// 多 Submesh 转换结果
        /// </summary>
        public class MultiSubmeshResult
        {
            public Mesh Mesh;
            public List<string> MaterialPaths; // 每个 submesh 对应的材质路径
        }

        /// <summary>
        /// 将 PCGGeometry 转换为 Unity Mesh（支持多 Submesh）
        /// </summary>
        public static MultiSubmeshResult ConvertWithSubmeshes(PCGGeometry geometry)
        {
            var result = new MultiSubmeshResult
            {
                Mesh = new Mesh { name = "PCGMesh" },
                MaterialPaths = new List<string>()
            };

            if (geometry == null || geometry.Points.Count == 0)
                return result;

            result.Mesh.vertices = geometry.Points.ToArray();

            // 检查是否有 @material 属性
            var materialAttr = geometry.PrimAttribs.GetAttribute("material");
            var materialIdAttr = geometry.PrimAttribs.GetAttribute("materialId");

            // 按材质分组
            var submeshGroups = new List<List<int>>();
            var materialPathSet = new HashSet<string>();

            if (materialAttr != null || materialIdAttr != null)
            {
                // 按 @material 属性值分组
                var materialToSubmesh = new Dictionary<object, int>();
                for (int i = 0; i < geometry.Primitives.Count; i++)
                {
                    object matValue = null;
                    if (materialAttr != null && i < materialAttr.Values.Count)
                        matValue = materialAttr.Values[i];
                    else if (materialIdAttr != null && i < materialIdAttr.Values.Count)
                        matValue = materialIdAttr.Values[i];

                    if (matValue == null) matValue = 0;

                    if (!materialToSubmesh.TryGetValue(matValue, out int submeshIdx))
                    {
                        submeshIdx = submeshGroups.Count;
                        materialToSubmesh[matValue] = submeshIdx;
                        submeshGroups.Add(new List<int>());

                        // 记录材质路径
                        string matPath = matValue is string s ? s : matValue.ToString();
                        result.MaterialPaths.Add(matPath);
                    }
                    submeshGroups[submeshIdx].Add(i);
                }
            }
            else if (geometry.PrimGroups.Count > 0)
            {
                // 按 PrimGroups 分组
                var assignedPrims = new HashSet<int>();
                foreach (var kvp in geometry.PrimGroups)
                {
                    var groupPrims = new List<int>();
                    foreach (int pi in kvp.Value)
                    {
                        if (!assignedPrims.Contains(pi))
                        {
                            groupPrims.Add(pi);
                            assignedPrims.Add(pi);
                        }
                    }
                    if (groupPrims.Count > 0)
                    {
                        submeshGroups.Add(groupPrims);
                        result.MaterialPaths.Add(kvp.Key); // 使用 group 名作为材质标识
                    }
                }

                // 未分配的面归入最后一个 submesh
                var unassigned = new List<int>();
                for (int i = 0; i < geometry.Primitives.Count; i++)
                {
                    if (!assignedPrims.Contains(i))
                        unassigned.Add(i);
                }
                if (unassigned.Count > 0)
                {
                    submeshGroups.Add(unassigned);
                    result.MaterialPaths.Add("default");
                }
            }
            else
            {
                // 单 submesh：所有面
                var allPrims = new List<int>();
                for (int i = 0; i < geometry.Primitives.Count; i++)
                    allPrims.Add(i);
                submeshGroups.Add(allPrims);
                result.MaterialPaths.Add("default");
            }

            // 创建 submeshes
            result.Mesh.subMeshCount = submeshGroups.Count;
            var allTriangles = new List<int>();

            for (int subIdx = 0; subIdx < submeshGroups.Count; subIdx++)
            {
                int startIdx = allTriangles.Count;
                foreach (int primIdx in submeshGroups[subIdx])
                {
                    var prim = geometry.Primitives[primIdx];
                    if (prim.Length == 3)
                    {
                        allTriangles.Add(prim[0]);
                        allTriangles.Add(prim[1]);
                        allTriangles.Add(prim[2]);
                    }
                    else if (prim.Length == 4)
                    {
                        allTriangles.Add(prim[0]);
                        allTriangles.Add(prim[1]);
                        allTriangles.Add(prim[2]);
                        allTriangles.Add(prim[0]);
                        allTriangles.Add(prim[2]);
                        allTriangles.Add(prim[3]);
                    }
                    else if (prim.Length > 4)
                    {
                        TriangulateNgon(geometry.Points, prim, allTriangles);
                    }
                }
                result.Mesh.SetTriangles(allTriangles.Skip(startIdx).ToArray(), subIdx);
            }

            // 属性映射
            ApplyAttributes(geometry, result.Mesh);

            return result;
        }

        /// <summary>
        /// 将 PCGGeometry 转换为 Unity Mesh（单 Submesh，向后兼容）
        /// </summary>
        public static Mesh Convert(PCGGeometry geometry)
        {
            Debug.Log($"[PCGGeometryToMesh] Converting PCGGeometry to Mesh (Points: {geometry?.Points.Count ?? 0}, Prims: {geometry?.Primitives.Count ?? 0})");

            var mesh = new Mesh();
            mesh.name = "PCGMesh";

            if (geometry == null || geometry.Points.Count == 0)
                return mesh;

            mesh.vertices = geometry.Points.ToArray();

            // 将多边形面转换为三角形索引
            var triangles = new List<int>();
            foreach (var prim in geometry.Primitives)
            {
                if (prim.Length == 3)
                {
                    triangles.Add(prim[0]);
                    triangles.Add(prim[1]);
                    triangles.Add(prim[2]);
                }
                else if (prim.Length == 4)
                {
                    // 四边形拆分为两个三角形
                    triangles.Add(prim[0]);
                    triangles.Add(prim[1]);
                    triangles.Add(prim[2]);
                    triangles.Add(prim[0]);
                    triangles.Add(prim[2]);
                    triangles.Add(prim[3]);
                }
                else if (prim.Length > 4)
                {
                    // N-gon: 耳切法三角化
                    TriangulateNgon(geometry.Points, prim, triangles);
                }
            }
            mesh.triangles = triangles.ToArray();

            ApplyAttributes(geometry, mesh);

            return mesh;
        }

        /// <summary>
        /// 应用属性到 Mesh（法线、UV、颜色等）
        /// </summary>
        private static void ApplyAttributes(PCGGeometry geometry, Mesh mesh)
        {
            bool hasCustomNormals = false;

            // Normal ("N")
            var normalAttr = geometry.PointAttribs.GetAttribute("N");
            if (normalAttr != null && normalAttr.Values.Count == geometry.Points.Count)
            {
                var normals = new Vector3[geometry.Points.Count];
                for (int i = 0; i < geometry.Points.Count; i++)
                {
                    normals[i] = normalAttr.Values[i] is Vector3 n ? n : Vector3.up;
                }
                mesh.normals = normals;
                hasCustomNormals = true;
            }

            // UV ("uv")
            var uvAttr = geometry.PointAttribs.GetAttribute("uv");
            if (uvAttr != null && uvAttr.Values.Count == geometry.Points.Count)
            {
                var uvs = new Vector2[geometry.Points.Count];
                for (int i = 0; i < geometry.Points.Count; i++)
                {
                    var val = uvAttr.Values[i];
                    if (val is Vector2 uv2) uvs[i] = uv2;
                    else if (val is Vector3 uv3) uvs[i] = new Vector2(uv3.x, uv3.y);
                    else uvs[i] = Vector2.zero;
                }
                mesh.uv = uvs;
            }

            // Color ("Cd")
            var colorAttr = geometry.PointAttribs.GetAttribute("Cd");
            if (colorAttr != null && colorAttr.Values.Count == geometry.Points.Count)
            {
                var colors = new Color[geometry.Points.Count];
                for (int i = 0; i < geometry.Points.Count; i++)
                {
                    var val = colorAttr.Values[i];
                    if (val is Color c) colors[i] = c;
                    else if (val is Vector3 v) colors[i] = new Color(v.x, v.y, v.z, 1f);
                    else colors[i] = Color.white;
                }
                mesh.colors = colors;
            }

            // Alpha ("Alpha") — 如果有单独的 Alpha 属性，合并到 Color
            var alphaAttr = geometry.PointAttribs.GetAttribute("Alpha");
            if (alphaAttr != null && alphaAttr.Values.Count == geometry.Points.Count && mesh.colors != null)
            {
                var colors = mesh.colors;
                for (int i = 0; i < geometry.Points.Count; i++)
                {
                    if (alphaAttr.Values[i] is float a)
                        colors[i].a = a;
                }
                mesh.colors = colors;
            }

            // 如果没有自定义法线，自动计算
            if (!hasCustomNormals)
                mesh.RecalculateNormals();

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
        }

        /// <summary>
        /// 将 Unity Mesh 转换为 PCGGeometry（B1 完整实现：submesh→PrimGroup）
        /// </summary>
        public static PCGGeometry FromMesh(Mesh mesh)
        {
            var geo = new PCGGeometry();
            if (mesh == null) return geo;

            geo.Points = new List<Vector3>(mesh.vertices);

            // 法线
            if (mesh.normals != null && mesh.normals.Length == mesh.vertexCount)
            {
                var normalAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3);
                foreach (var n in mesh.normals)
                    normalAttr.Values.Add(n);
            }

            // UV
            if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount)
            {
                var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2);
                foreach (var uv in mesh.uv)
                    uvAttr.Values.Add(uv);
            }

            // 顶点颜色
            if (mesh.colors != null && mesh.colors.Length == mesh.vertexCount)
            {
                var colorAttr = geo.PointAttribs.CreateAttribute("Cd", AttribType.Color);
                foreach (var c in mesh.colors)
                    colorAttr.Values.Add(c);
            }

            // Submesh → Primitives + PrimGroups
            int globalPrimOffset = 0;
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                int[] tris = mesh.GetTriangles(sub);
                string groupName = mesh.subMeshCount > 1 ? $"submesh_{sub}" : "default";
                var groupIndices = new HashSet<int>();

                for (int i = 0; i < tris.Length; i += 3)
                {
                    geo.Primitives.Add(new int[] { tris[i], tris[i + 1], tris[i + 2] });
                    groupIndices.Add(globalPrimOffset++);
                }

                if (mesh.subMeshCount > 1)
                    geo.PrimGroups[groupName] = groupIndices;
            }

            return geo;
        }

        /// <summary>
        /// 耳切法三角化 N-gon
        /// </summary>
        private static void TriangulateNgon(List<Vector3> allPoints, int[] prim, List<int> triangles)
        {
            if (prim.Length < 3) return;

            // 计算多边形法线（用于判断凸凹）
            Vector3 normal = Vector3.zero;
            for (int i = 0; i < prim.Length; i++)
            {
                Vector3 curr = allPoints[prim[i]];
                Vector3 next = allPoints[prim[(i + 1) % prim.Length]];
                normal.x += (curr.y - next.y) * (curr.z + next.z);
                normal.y += (curr.z - next.z) * (curr.x + next.x);
                normal.z += (curr.x - next.x) * (curr.y + next.y);
            }
            if (normal.sqrMagnitude > 0.0001f) normal.Normalize();
            else normal = Vector3.up;

            var indices = new List<int>(prim);

            int safety = indices.Count * indices.Count; // 防止无限循环
            while (indices.Count > 3 && safety-- > 0)
            {
                bool earFound = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = (i - 1 + indices.Count) % indices.Count;
                    int next = (i + 1) % indices.Count;

                    Vector3 a = allPoints[indices[prev]];
                    Vector3 b = allPoints[indices[i]];
                    Vector3 c = allPoints[indices[next]];

                    // 检查是否为凸角
                    Vector3 cross = Vector3.Cross(b - a, c - b);
                    if (Vector3.Dot(cross, normal) < 0) continue;

                    // 检查是否有其他点在三角形内
                    bool isEar = true;
                    for (int j = 0; j < indices.Count; j++)
                    {
                        if (j == prev || j == i || j == next) continue;
                        if (PointInTriangle(allPoints[indices[j]], a, b, c))
                        {
                            isEar = false;
                            break;
                        }
                    }

                    if (isEar)
                    {
                        triangles.Add(indices[prev]);
                        triangles.Add(indices[i]);
                        triangles.Add(indices[next]);
                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    // Fallback: 扇形三角化
                    for (int i = 1; i < indices.Count - 1; i++)
                    {
                        triangles.Add(indices[0]);
                        triangles.Add(indices[i]);
                        triangles.Add(indices[i + 1]);
                    }
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
            }
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = c - a, v1 = b - a, v2 = p - a;
            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);
            float denom = dot00 * dot11 - dot01 * dot01;
            // 退化三角形保护：面积为零时直接返回 false
            if (Mathf.Abs(denom) < 1e-8f) return false;
            float inv = 1f / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * inv;
            float v = (dot00 * dot12 - dot01 * dot02) * inv;
            return u >= 0 && v >= 0 && u + v <= 1;
        }
    }
}
