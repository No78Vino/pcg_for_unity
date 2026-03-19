using System.Collections.Generic;
using System.Text.RegularExpressions;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 创建分组（对标 Houdini GroupCreate SOP）
    /// </summary>
    public class GroupCreateNode : PCGNodeBase
    {
        public override string Name => "GroupCreate";
        public override string DisplayName => "Group Create";
        public override string Description => "创建或修改点/面分组";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                "Group Name", "分组名称", "group1"),
            new PCGParamSchema("groupType", PCGPortDirection.Input, PCGPortType.String,
                "Group Type", "分组类型（point/primitive）", "point"),
            new PCGParamSchema("filter", PCGPortDirection.Input, PCGPortType.String,
                "Filter", "过滤表达式（如 @P.y > 0）", ""),
            new PCGParamSchema("baseGroup", PCGPortDirection.Input, PCGPortType.String,
                "Base Group", "基于哪个已有分组进行过滤", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体（带新分组）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string groupName = GetParamString(parameters, "groupName", "group1");
            string groupType = GetParamString(parameters, "groupType", "point");
            string filter = GetParamString(parameters, "filter", "");
            string baseGroup = GetParamString(parameters, "baseGroup", "");

            HashSet<int> groupMembers = new HashSet<int>();

            // 如果有基础分组，从中开始
            if (!string.IsNullOrEmpty(baseGroup))
            {
                if (groupType == "point" && geo.PointGroups.TryGetValue(baseGroup, out var basePointGroup))
                {
                    groupMembers = new HashSet<int>(basePointGroup);
                }
                else if (groupType == "primitive" && geo.PrimGroups.TryGetValue(baseGroup, out var basePrimGroup))
                {
                    groupMembers = new HashSet<int>(basePrimGroup);
                }
            }

            // 应用过滤表达式
            if (!string.IsNullOrEmpty(filter))
            {
                var filtered = EvaluateFilter(geo, filter, groupType);
                if (groupMembers.Count > 0)
                {
                    // 与基础分组取交集
                    groupMembers.IntersectWith(filtered);
                }
                else
                {
                    groupMembers = filtered;
                }
            }
            else if (groupMembers.Count == 0)
            {
                // 无过滤条件且无基础分组：全选
                int count = groupType == "point" ? geo.Points.Count : geo.Primitives.Count;
                for (int i = 0; i < count; i++)
                    groupMembers.Add(i);
            }

            // 创建分组
            if (groupType == "point")
            {
                geo.PointGroups[groupName] = groupMembers;
            }
            else
            {
                geo.PrimGroups[groupName] = groupMembers;
            }

            return SingleOutput("geometry", geo);
        }

        private HashSet<int> EvaluateFilter(PCGGeometry geo, string filter, string groupType)
        {
            var result = new HashSet<int>();

            // 简单解析 @P.y > value 格式
            var match = Regex.Match(filter, @"@P\.(x|y|z)\s*(>|<|>=|<=|==)\s*([\d.]+)");
            if (match.Success)
            {
                string axis = match.Groups[1].Value;
                string op = match.Groups[2].Value;
                float value = float.Parse(match.Groups[3].Value);

                int count = groupType == "point" ? geo.Points.Count : geo.Primitives.Count;
                for (int i = 0; i < count; i++)
                {
                    Vector3 pos;
                    if (groupType == "point")
                    {
                        pos = geo.Points[i];
                    }
                    else
                    {
                        // 面中心
                        pos = GetPrimitiveCenter(geo, i);
                    }

                    float coord = axis == "x" ? pos.x :
                                  axis == "y" ? pos.y : pos.z;

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

        private Vector3 GetPrimitiveCenter(PCGGeometry geo, int primIndex)
        {
            var prim = geo.Primitives[primIndex];
            Vector3 center = Vector3.zero;
            foreach (int idx in prim)
                center += geo.Points[idx];
            return center / prim.Length;
        }
    }
}