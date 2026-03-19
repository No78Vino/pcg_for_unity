using System.Collections.Generic;
using System.Text.RegularExpressions;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 删除几何体中的元素（对标 Houdini Delete SOP）
    /// </summary>
    public class DeleteNode : PCGNodeBase
    {
        public override string Name => "Delete";
        public override string DisplayName => "Delete";
        public override string Description => "删除几何体中的点、面或边";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要删除的分组名（留空则用 filter）", ""),
            new PCGParamSchema("filter", PCGPortDirection.Input, PCGPortType.String,
                "Filter", "过滤表达式（如 @P.y > 0）", ""),
            new PCGParamSchema("deleteNonSelected", PCGPortDirection.Input, PCGPortType.Bool,
                "Delete Non-Selected", "反转选择（删除未选中的元素）", false),
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
            string group = GetParamString(parameters, "group", "");
            string filter = GetParamString(parameters, "filter", "");
            bool deleteNonSelected = GetParamBool(parameters, "deleteNonSelected", false);

            if (string.IsNullOrEmpty(group) && string.IsNullOrEmpty(filter))
            {
                return SingleOutput("geometry", geo);
            }

            // 确定要删除的点索引集合
            HashSet<int> toDelete = new HashSet<int>();

            if (!string.IsNullOrEmpty(group))
            {
                // 从分组获取要删除的点
                if (geo.PointGroups.TryGetValue(group, out var groupPoints))
                {
                    toDelete = new HashSet<int>(groupPoints);
                }
            }
            else if (!string.IsNullOrEmpty(filter))
            {
                // 简单表达式解析：支持 @P.y > value 格式
                toDelete = EvaluateFilter(geo, filter);
            }

            if (deleteNonSelected)
            {
                // 反转：删除未选中的
                var newToDelete = new HashSet<int>();
                for (int i = 0; i < geo.Points.Count; i++)
                {
                    if (!toDelete.Contains(i))
                        newToDelete.Add(i);
                }
                toDelete = newToDelete;
            }

            // 构建索引映射：旧索引 -> 新索引
            var indexMap = new Dictionary<int, int>();
            int newIndex = 0;
            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (!toDelete.Contains(i))
                {
                    indexMap[i] = newIndex++;
                }
            }

            // 创建新的顶点列表
            var newPoints = new List<Vector3>();
            for (int i = 0; i < geo.Points.Count; i++)
            {
                if (!toDelete.Contains(i))
                    newPoints.Add(geo.Points[i]);
            }
            geo.Points = newPoints;

            // 过滤面：删除包含被删除点的面
            var newPrims = new List<int[]>();
            foreach (var prim in geo.Primitives)
            {
                bool keep = true;
                foreach (int idx in prim)
                {
                    if (toDelete.Contains(idx))
                    {
                        keep = false;
                        break;
                    }
                }
                if (keep)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        newPrim[i] = indexMap[prim[i]];
                    newPrims.Add(newPrim);
                }
            }
            geo.Primitives = newPrims;

            // 更新分组
            var newPointGroups = new Dictionary<string, HashSet<int>>();
            foreach (var kvp in geo.PointGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (int idx in kvp.Value)
                {
                    if (indexMap.TryGetValue(idx, out int mapped))
                        newGroup.Add(mapped);
                }
                if (newGroup.Count > 0)
                    newPointGroups[kvp.Key] = newGroup;
            }
            geo.PointGroups = newPointGroups;

            return SingleOutput("geometry", geo);
        }

        private HashSet<int> EvaluateFilter(PCGGeometry geo, string filter)
        {
            var result = new HashSet<int>();
            
            // 简单解析 @P.y > value 格式
            var match = Regex.Match(filter, @"@P\.(x|y|z)\s*(>|<|>=|<=|==)\s*([\d.]+)");
            if (match.Success)
            {
                string axis = match.Groups[1].Value;
                string op = match.Groups[2].Value;
                float value = float.Parse(match.Groups[3].Value);

                for (int i = 0; i < geo.Points.Count; i++)
                {
                    float coord = axis == "x" ? geo.Points[i].x :
                                  axis == "y" ? geo.Points[i].y : geo.Points[i].z;

                    bool matches = op == ">" ? coord > value :
                                   op == "<" ? coord < value :
                                   op == ">=" ? coord >= value :
                                   op == "<=" ? coord <= value :
                                   Mathf.Approximately(coord, value);

                    if (matches)
                        result.Add(i);
                }
            }

            return result;
        }
    }
}