using System.Collections.Generic;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 几何体公共工具方法，供多个节点共享使用。
    /// </summary>
    public static class PCGGeometryUtils
    {
        /// <summary>
        /// 按点连通性将几何体拆分为多个独立的连通分量。
        /// 使用 Union-Find 算法，被 ForEachNode 和 ConnectivityNode 共用。
        /// </summary>
        public static List<PCGGeometry> SplitByConnectivity(PCGGeometry geo)
        {
            int pointCount = geo.Points.Count;
            int[] componentId = new int[pointCount];
            for (int i = 0; i < pointCount; i++) componentId[i] = i;

            int Find(int x)
            {
                while (componentId[x] != x)
                {
                    componentId[x] = componentId[componentId[x]];
                    x = componentId[x];
                }
                return x;
            }
            void Union(int a, int b)
            {
                a = Find(a); b = Find(b);
                if (a != b) componentId[a] = b;
            }

            foreach (var prim in geo.Primitives)
            {
                if (prim.Length == 0) continue;
                for (int i = 1; i < prim.Length; i++)
                    Union(prim[0], prim[i]);
            }

            // 按连通分量分组面
            var groups = new Dictionary<int, List<int>>();
            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length == 0) continue;
                int root = Find(prim[0]);
                if (!groups.ContainsKey(root))
                    groups[root] = new List<int>();
                groups[root].Add(pi);
            }

            var results = new List<PCGGeometry>();
            foreach (var kvp in groups)
            {
                var primSet = new HashSet<int>(kvp.Value);
                results.Add(ExtractPrimGroup(geo, primSet));
            }
            return results;
        }

        /// <summary>
        /// 从几何体中提取指定面集合形成子几何体（重映射点索引，复制点属性）。
        /// </summary>
        public static PCGGeometry ExtractPrimGroup(PCGGeometry source, HashSet<int> primIndices)
        {
            var result = new PCGGeometry();

            var usedPoints = new HashSet<int>();
            foreach (int pi in primIndices)
            {
                if (pi < source.Primitives.Count)
                    foreach (int vi in source.Primitives[pi])
                        usedPoints.Add(vi);
            }

            // 建立旧索引 -> 新索引映射（保持有序）
            var indexMap = new Dictionary<int, int>();
            var sortedPoints = new List<int>(usedPoints);
            sortedPoints.Sort();
            foreach (int oldIdx in sortedPoints)
            {
                indexMap[oldIdx] = result.Points.Count;
                result.Points.Add(source.Points[oldIdx]);
            }

            foreach (int pi in primIndices)
            {
                if (pi >= source.Primitives.Count) continue;
                var prim = source.Primitives[pi];
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = indexMap[prim[i]];
                result.Primitives.Add(newPrim);
            }

            // 复制点属性
            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in sortedPoints)
                {
                    newAttr.Values.Add(oldIdx < attr.Values.Count ? attr.Values[oldIdx] : attr.DefaultValue);
                }
            }

            return result;
        }
    }
}
