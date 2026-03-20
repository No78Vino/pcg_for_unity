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
                // TODO: 处理 N 边形（需要三角化，使用 LibTessDotNet）
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
    }
}
