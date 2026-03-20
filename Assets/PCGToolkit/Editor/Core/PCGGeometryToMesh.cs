using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCGGeometry 与 Unity Mesh 之间的转换工具。
    /// 仅在最终输出阶段使用。
    /// </summary>
    public static class PCGGeometryToMesh
    {
        /// <summary>
        /// 将 PCGGeometry 转换为 Unity Mesh
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

            // 从 PointAttribs 提取属性映射到 Mesh
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

            return mesh;
        }

        /// <summary>
        /// 将 Unity Mesh 转换为 PCGGeometry
        /// </summary>
        public static PCGGeometry FromMesh(Mesh mesh)
        {
            // TODO: 实现完整的 Mesh → PCGGeometry 转换
            Debug.Log("[PCGGeometryToMesh] FromMesh: TODO - 将 Unity Mesh 转换为 PCGGeometry");

            var geo = new PCGGeometry();

            if (mesh == null)
                return geo;

            geo.Points = new List<Vector3>(mesh.vertices);

            // 将三角形索引转换为 Primitives
            var tris = mesh.triangles;
            for (int i = 0; i < tris.Length; i += 3)
            {
                geo.Primitives.Add(new int[] { tris[i], tris[i + 1], tris[i + 2] });
            }

            // TODO: 映射 normals、uv、colors 等到属性系统
            if (mesh.normals != null && mesh.normals.Length > 0)
            {
                var normalAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3);
                foreach (var n in mesh.normals)
                    normalAttr.Values.Add(n);
            }

            // 映射 UV
            if (mesh.uv != null && mesh.uv.Length > 0)
            {
                var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2);
                foreach (var uv in mesh.uv)
                    uvAttr.Values.Add(uv);
            }

            // 映射 Color
            if (mesh.colors != null && mesh.colors.Length > 0)
            {
                var colorAttr = geo.PointAttribs.CreateAttribute("Cd", AttribType.Color);
                foreach (var c in mesh.colors)
                    colorAttr.Values.Add(c);
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
            float inv = 1f / (dot00 * dot11 - dot01 * dot01);
            float u = (dot11 * dot02 - dot01 * dot12) * inv;
            float v = (dot00 * dot12 - dot01 * dot02) * inv;
            return u >= 0 && v >= 0 && u + v <= 1;
        }
    }
}
