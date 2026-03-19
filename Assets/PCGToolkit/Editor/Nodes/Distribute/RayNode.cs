using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Distribute
{
    /// <summary>
    /// 射线投射/投影（对标 Houdini Ray SOP）
    /// 将点沿射线方向投影到目标表面
    /// </summary>
    public class RayNode : PCGNodeBase
    {
        public override string Name => "Ray";
        public override string DisplayName => "Ray";
        public override string Description => "将几何体的点沿射线方向投影到目标表面";
        public override PCGNodeCategory Category => PCGNodeCategory.Distribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "要投影的几何体", null, required: true),
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.Geometry,
                "Target", "目标表面几何体", null, required: true),
            new PCGParamSchema("direction", PCGPortDirection.Input, PCGPortType.Vector3,
                "Direction", "射线方向（留空则使用点法线）", Vector3.down),
            new PCGParamSchema("maxDistance", PCGPortDirection.Input, PCGPortType.Float,
                "Max Distance", "最大投射距离", 100f),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅投影指定分组的点", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "投影后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            var target = GetInputGeometry(inputGeometries, "target");
            Vector3 direction = GetParamVector3(parameters, "direction", Vector3.down).normalized;
            float maxDistance = GetParamFloat(parameters, "maxDistance", 100f);
            string group = GetParamString(parameters, "group", "");

            if (geo.Points.Count == 0)
            {
                return SingleOutput("geometry", geo);
            }

            if (target.Points.Count == 0 || target.Primitives.Count == 0)
            {
                ctx.LogWarning("Ray: 目标几何体为空");
                return SingleOutput("geometry", geo);
            }

            // 构建目标的三角形列表用于射线检测
            List<RayTriangle> triangles = new List<RayTriangle>();
            foreach (var prim in target.Primitives)
            {
                if (prim.Length == 3)
                {
                    triangles.Add(new RayTriangle
                    {
                        v0 = target.Points[prim[0]],
                        v1 = target.Points[prim[1]],
                        v2 = target.Points[prim[2]]
                    });
                }
                else if (prim.Length == 4)
                {
                    // 四边形拆分为两个三角形
                    triangles.Add(new RayTriangle
                    {
                        v0 = target.Points[prim[0]],
                        v1 = target.Points[prim[1]],
                        v2 = target.Points[prim[2]]
                    });
                    triangles.Add(new RayTriangle
                    {
                        v0 = target.Points[prim[0]],
                        v1 = target.Points[prim[2]],
                        v2 = target.Points[prim[3]]
                    });
                }
            }

            // 确定要投影的点
            HashSet<int> pointsToProject = new HashSet<int>();
            if (!string.IsNullOrEmpty(group) && geo.PointGroups.TryGetValue(group, out var groupPoints))
            {
                pointsToProject = groupPoints;
            }
            else
            {
                for (int i = 0; i < geo.Points.Count; i++)
                    pointsToProject.Add(i);
            }

            // 对每个点执行射线检测
            int hitCount = 0;
            foreach (int pointIdx in pointsToProject)
            {
                Vector3 origin = geo.Points[pointIdx];
                float closestDist = maxDistance;
                Vector3? closestHit = null;

                foreach (var tri in triangles)
                {
                    if (RayTriangleIntersect(origin, direction, tri, maxDistance, out float dist, out Vector3 hitPoint))
                    {
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            closestHit = hitPoint;
                        }
                    }
                }

                if (closestHit.HasValue)
                {
                    geo.Points[pointIdx] = closestHit.Value;
                    hitCount++;
                }
            }

            ctx.Log($"Ray: 投影了 {hitCount}/{pointsToProject.Count} 个点");

            return SingleOutput("geometry", geo);
        }

        private struct RayTriangle
        {
            public Vector3 v0, v1, v2;
        }

        private bool RayTriangleIntersect(Vector3 origin, Vector3 dir, RayTriangle tri, float maxDist, out float dist, out Vector3 hitPoint)
        {
            dist = 0;
            hitPoint = Vector3.zero;

            Vector3 edge1 = tri.v1 - tri.v0;
            Vector3 edge2 = tri.v2 - tri.v0;
            Vector3 h = Vector3.Cross(dir, edge2);
            float a = Vector3.Dot(edge1, h);

            if (Mathf.Abs(a) < 1e-6f)
                return false;

            float f = 1f / a;
            Vector3 s = origin - tri.v0;
            float u = f * Vector3.Dot(s, h);

            if (u < 0 || u > 1)
                return false;

            Vector3 q = Vector3.Cross(s, edge1);
            float v = f * Vector3.Dot(dir, q);

            if (v < 0 || u + v > 1)
                return false;

            float t = f * Vector3.Dot(edge2, q);

            if (t > 0 && t < maxDist)
            {
                dist = t;
                hitPoint = origin + dir * t;
                return true;
            }

            return false;
        }
    }
}