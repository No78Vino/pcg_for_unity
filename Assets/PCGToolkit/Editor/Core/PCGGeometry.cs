using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 核心几何数据结构，对标 Houdini 的 Geometry + Attribute 体系。
    /// 所有 PCG 节点之间传递的核心数据类型。
    /// Unity Mesh 只在最终输出阶段才由 PCGGeometry 转换生成。
    /// </summary>
    public class PCGGeometry
    {
        // ---- 拓扑 ----
        /// <summary>顶点位置列表</summary>
        public List<Vector3> Points = new List<Vector3>();

        /// <summary>面（支持三角形/四边形/多边形），每个元素是该面的顶点索引数组</summary>
        public List<int[]> Primitives = new List<int[]>();

        /// <summary>边（按需构建），每个元素是 [startIndex, endIndex]</summary>
        public List<int[]> Edges = new List<int[]>();

        // ---- 属性系统（Point / Vertex / Primitive / Detail 四个层级） ----
        public AttributeStore PointAttribs = new AttributeStore();
        public AttributeStore VertexAttribs = new AttributeStore();
        public AttributeStore PrimAttribs = new AttributeStore();
        public AttributeStore DetailAttribs = new AttributeStore();

        // ---- 分组系统 ----
        public Dictionary<string, HashSet<int>> PointGroups = new Dictionary<string, HashSet<int>>();
        public Dictionary<string, HashSet<int>> PrimGroups = new Dictionary<string, HashSet<int>>();

        /// <summary>
        /// 创建当前几何体的深拷贝
        /// </summary>
        public PCGGeometry Clone()
        {
            // TODO: 实现深拷贝
            var clone = new PCGGeometry();
            clone.Points = new List<Vector3>(Points);
            foreach (var prim in Primitives)
                clone.Primitives.Add((int[])prim.Clone());
            foreach (var edge in Edges)
                clone.Edges.Add((int[])edge.Clone());
            clone.PointAttribs = PointAttribs.Clone();
            clone.VertexAttribs = VertexAttribs.Clone();
            clone.PrimAttribs = PrimAttribs.Clone();
            clone.DetailAttribs = DetailAttribs.Clone();
            foreach (var kvp in PointGroups)
                clone.PointGroups[kvp.Key] = new HashSet<int>(kvp.Value);
            foreach (var kvp in PrimGroups)
                clone.PrimGroups[kvp.Key] = new HashSet<int>(kvp.Value);
            return clone;
        }

        /// <summary>
        /// 按需构建边列表（从 Primitives 中提取所有边）
        /// </summary>
        public void BuildEdges()
        {
            Edges.Clear();
            var edgeSet = new HashSet<long>(); // 用 long 编码无序边对

            foreach (var prim in Primitives)
            {
                for (int i = 0; i < prim.Length; i++)
                {
                    int a = prim[i];
                    int b = prim[(i + 1) % prim.Length];

                    // 无序边：确保 min 在前
                    int minIdx = Mathf.Min(a, b);
                    int maxIdx = Mathf.Max(a, b);
                    long edgeKey = ((long)minIdx << 32) | (long)(uint)maxIdx;

                    if (edgeSet.Add(edgeKey))
                    {
                        Edges.Add(new int[] { minIdx, maxIdx });
                    }
                }
            }
        }

        /// <summary>
        /// 清空所有数据
        /// </summary>
        public void Clear()
        {
            Points.Clear();
            Primitives.Clear();
            Edges.Clear();
            PointAttribs.Clear();
            VertexAttribs.Clear();
            PrimAttribs.Clear();
            DetailAttribs.Clear();
            PointGroups.Clear();
            PrimGroups.Clear();
        }
    }
}
