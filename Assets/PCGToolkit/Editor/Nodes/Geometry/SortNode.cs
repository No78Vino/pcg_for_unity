using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 排序点/面顺序（对标 Houdini Sort SOP）
    /// </summary>
    public class SortNode : PCGNodeBase
    {
        public override string Name => "Sort";
        public override string DisplayName => "Sort";
        public override string Description => "按指定规则排序点或面的顺序";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("key", PCGPortDirection.Input, PCGPortType.String,
                "Key", "排序依据（x/y/z/random/attribute）", "y"),
            new PCGParamSchema("reverse", PCGPortDirection.Input, PCGPortType.Bool,
                "Reverse", "降序排列", false),
            new PCGParamSchema("pointSort", PCGPortDirection.Input, PCGPortType.Bool,
                "Sort Points", "是否排序点", true),
            new PCGParamSchema("primSort", PCGPortDirection.Input, PCGPortType.Bool,
                "Sort Primitives", "是否排序面", false),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子（当 key=random 时使用）", 0),
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
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string key = GetParamString(parameters, "key", "y");
            bool reverse = GetParamBool(parameters, "reverse", false);
            bool pointSort = GetParamBool(parameters, "pointSort", true);
            bool primSort = GetParamBool(parameters, "primSort", false);
            int seed = GetParamInt(parameters, "seed", 0);

            if (pointSort)
            {
                int[] indices = GetSortedIndices(geo.Points.Count, i => GetSortKey(geo.Points[i], key, i, seed), reverse);
                geo = RemapPoints(geo, indices);
            }

            if (primSort)
            {
                int[] indices = GetSortedIndices(geo.Primitives.Count, i => GetPrimSortKey(geo, i, key, seed), reverse);
                geo = RemapPrimitives(geo, indices);
            }

            return SingleOutput("geometry", geo);
        }

        private float GetSortKey(Vector3 point, string key, int index, int seed)
        {
            switch (key.ToLower())
            {
                case "x": return point.x;
                case "y": return point.y;
                case "z": return point.z;
                case "random": 
                    System.Random rng = new System.Random(seed + index);
                    return (float)rng.NextDouble();
                default: return point.y;
            }
        }

        private float GetPrimSortKey(PCGGeometry geo, int primIndex, string key, int seed)
        {
            var prim = geo.Primitives[primIndex];
            Vector3 center = Vector3.zero;
            foreach (int idx in prim)
                center += geo.Points[idx];
            center /= prim.Length;
            return GetSortKey(center, key, primIndex, seed);
        }

        private int[] GetSortedIndices(int count, System.Func<int, float> keySelector, bool reverse)
        {
            var indexed = Enumerable.Range(0, count)
                .Select(i => new { Index = i, Key = keySelector(i) })
                .ToList();

            if (reverse)
                indexed = indexed.OrderByDescending(x => x.Key).ToList();
            else
                indexed = indexed.OrderBy(x => x.Key).ToList();

            // 构建映射：新位置 -> 旧索引
            int[] result = new int[count];
            for (int i = 0; i < count; i++)
            {
                result[i] = indexed[i].Index;
            }
            return result;
        }

        private PCGGeometry RemapPoints(PCGGeometry geo, int[] newToOld)
        {
            var result = new PCGGeometry();
            
            // 构建旧索引到新索引的映射
            int[] oldToNew = new int[geo.Points.Count];
            for (int i = 0; i < newToOld.Length; i++)
            {
                oldToNew[newToOld[i]] = i;
            }

            // 重新排列顶点
            foreach (int oldIdx in newToOld)
            {
                result.Points.Add(geo.Points[oldIdx]);
            }

            // B9-1 fix: 按newToOld顺序重排PointAttribs
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
            {
                var newAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in newToOld)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            // 更新面索引
            foreach (var prim in geo.Primitives)
            {
                var newPrim = new int[prim.Length];
                for (int i = 0; i < prim.Length; i++)
                    newPrim[i] = oldToNew[prim[i]];
                result.Primitives.Add(newPrim);
            }

            // 复制边和分组
            foreach (var edge in geo.Edges)
            {
                result.Edges.Add(new int[] { oldToNew[edge[0]], oldToNew[edge[1]] });
            }

            foreach (var kvp in geo.PointGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int idx in kvp.Value)
                    newGroup.Add(oldToNew[idx]);
                result.PointGroups[kvp.Key] = newGroup;
            }

            // 复制面属性和分组
            result.PrimAttribs = geo.PrimAttribs.Clone();
            result.PrimGroups = geo.PrimGroups;
            result.DetailAttribs = geo.DetailAttribs.Clone();

            return result;
        }

        private PCGGeometry RemapPrimitives(PCGGeometry geo, int[] newToOld)
        {
            var result = new PCGGeometry();
            
            result.Points = new List<Vector3>(geo.Points);

            // B9-2 fix: 按newToOld顺序重排PrimAttribs
            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
            {
                var newAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                foreach (int oldIdx in newToOld)
                {
                    if (oldIdx < attr.Values.Count)
                        newAttr.Values.Add(attr.Values[oldIdx]);
                    else
                        newAttr.Values.Add(attr.DefaultValue);
                }
            }

            // 重新排列面
            foreach (int oldIdx in newToOld)
            {
                result.Primitives.Add((int[])geo.Primitives[oldIdx].Clone());
            }

            // 更新面分组
            var oldToNewPrim = new Dictionary<int, int>();
            for (int i = 0; i < newToOld.Length; i++)
            {
                oldToNewPrim[newToOld[i]] = i;
            }
            foreach (var kvp in geo.PrimGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int oldIdx in kvp.Value)
                {
                    if (oldToNewPrim.TryGetValue(oldIdx, out int newIdx))
                        newGroup.Add(newIdx);
                }
                if (newGroup.Count > 0)
                    result.PrimGroups[kvp.Key] = newGroup;
            }

            // 复制点属性和分组
            result.PointAttribs = geo.PointAttribs.Clone();
            result.PointGroups = geo.PointGroups;
            result.Edges = new List<int[]>(geo.Edges);
            result.DetailAttribs = geo.DetailAttribs.Clone();

            return result;
        }
    }
}