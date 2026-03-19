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

            // TODO: 从 PointAttribs 中提取 Normal / UV / Color 等属性映射到 Mesh
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

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

            return geo;
        }
    }
}
