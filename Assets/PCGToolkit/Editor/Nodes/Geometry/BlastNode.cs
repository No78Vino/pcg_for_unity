using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Geometry
{
    /// <summary>
    /// 删除指定元素（对标 Houdini Blast SOP）
    /// </summary>
    public class BlastNode : PCGNodeBase
    {
        public override string Name => "Blast";
        public override string DisplayName => "Blast";
        public override string Description => "按分组或编号删除点/面";
        public override PCGNodeCategory Category => PCGNodeCategory.Geometry;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "要删除的分组名或编号列表", ""),
            new PCGParamSchema("groupType", PCGPortDirection.Input, PCGPortType.String,
                "Group Type", "分组类型（point/primitive）", "primitive"),
            new PCGParamSchema("deleteNonSelected", PCGPortDirection.Input, PCGPortType.Bool,
                "Delete Non-Selected", "反转选择", false),
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
            string groupType = GetParamString(parameters, "groupType", "primitive");
            bool deleteNonSelected = GetParamBool(parameters, "deleteNonSelected", false);

            if (string.IsNullOrEmpty(group))
            {
                return SingleOutput("geometry", geo);
            }

            // 获取要操作的元素集合
            HashSet<int> targetSet = new HashSet<int>();
            if (groupType == "primitive")
            {
                if (geo.PrimGroups.TryGetValue(group, out var primGroup))
                {
                    targetSet = new HashSet<int>(primGroup);
                }
            }
            else
            {
                if (geo.PointGroups.TryGetValue(group, out var pointGroup))
                {
                    targetSet = new HashSet<int>(pointGroup);
                }
            }

            // 确定实际要删除的元素
            HashSet<int> toDelete = deleteNonSelected ? 
                GetComplement(targetSet, groupType == "primitive" ? geo.Primitives.Count : geo.Points.Count) :
                targetSet;

            if (toDelete.Count == 0)
            {
                return SingleOutput("geometry", geo);
            }

            if (groupType == "primitive")
            {
                // 删除面
                var newPrims = new List<int[]>();
                for (int i = 0; i < geo.Primitives.Count; i++)
                {
                    if (!toDelete.Contains(i))
                        newPrims.Add(geo.Primitives[i]);
                }
                geo.Primitives = newPrims;

                // 更新面分组
                var newPrimGroups = new Dictionary<string, HashSet<int>>();
                foreach (var kvp in geo.PrimGroups)
                {
                    var newGroup = new HashSet<int>();
                    int newIdx = 0;
                    for (int i = 0; i < geo.Primitives.Count + toDelete.Count; i++)
                    {
                        if (!toDelete.Contains(i))
                        {
                            if (kvp.Value.Contains(i))
                                newGroup.Add(newIdx);
                            newIdx++;
                        }
                    }
                    if (newGroup.Count > 0)
                        newPrimGroups[kvp.Key] = newGroup;
                }
                geo.PrimGroups = newPrimGroups;
            }
            else
            {
                // 删除点（同时删除包含这些点的面）
                var newPoints = new List<Vector3>();
                var indexMap = new Dictionary<int, int>();
                int newIdx = 0;
                for (int i = 0; i < geo.Points.Count; i++)
                {
                    if (!toDelete.Contains(i))
                    {
                        indexMap[i] = newIdx++;
                        newPoints.Add(geo.Points[i]);
                    }
                }
                geo.Points = newPoints;

                // 过滤面并更新索引
                var newPrims = new List<int[]>();
                foreach (var prim in geo.Primitives)
                {
                    bool keep = true;
                    foreach (int idx in prim)
                    {
                        if (!indexMap.ContainsKey(idx))
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
            }

            return SingleOutput("geometry", geo);
        }

        private HashSet<int> GetComplement(HashSet<int> set, int totalCount)
        {
            var result = new HashSet<int>();
            for (int i = 0; i < totalCount; i++)
            {
                if (!set.Contains(i))
                    result.Add(i);
            }
            return result;
        }
    }
}