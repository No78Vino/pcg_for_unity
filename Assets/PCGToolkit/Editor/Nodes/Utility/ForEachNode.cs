using System.Collections.Generic;
using System.Linq;
using PCGToolkit.Core;
using PCGToolkit.Graph;
using UnityEngine;

namespace PCGToolkit.Nodes.Utility
{
    /// <summary>
    /// 对每个 PrimGroup / PointGroup / Piece 执行子图（对标 Houdini ForEach SOP）
    /// 迭代模式:
    ///   byGroup   — 遍历所有 PrimGroup，每次提取该组的面和相关点
    ///   byPiece   — 按连通分量拆分，每个连通分量作为一次迭代
    ///   count     — 按固定次数迭代，每次传入完整几何体
    /// 每次迭代通过 ctx.GlobalVariables 注入 iteration / groupname / numiterations / value
    /// 所有迭代结果合并（Merge）后输出
    /// </summary>
    public class ForEachNode : PCGNodeBase
    {
        public override string Name => "ForEach";
        public override string DisplayName => "For Each";
        public override string Description => "对每个 Group/Piece/迭代执行子图";
        public override PCGNodeCategory Category => PCGNodeCategory.Utility;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("subGraphPath", PCGPortDirection.Input, PCGPortType.String,
                "SubGraph Path", "子图资源路径（Assets/...）", ""),
            new PCGParamSchema("mode", PCGPortDirection.Input, PCGPortType.String,
                "Mode", "迭代模式（byGroup/byPiece/count）", "byGroup")
            {
                EnumOptions = new[] { "byGroup", "byPiece", "count" }
            },
            new PCGParamSchema("iterations", PCGPortDirection.Input, PCGPortType.Int,
                "Iterations", "count 模式下的迭代次数", 1),
            new PCGParamSchema("feedback", PCGPortDirection.Input, PCGPortType.Bool,
                "Feedback", "count 模式下是否只输出最终迭代结果", false),
            new PCGParamSchema("valueAttrib", PCGPortDirection.Input, PCGPortType.String,
                "Value Attribute", "要读取值的属性名（注入到 value 变量）", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "所有迭代结果合并后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            string subGraphPath = GetParamString(parameters, "subGraphPath", "");
            string mode = GetParamString(parameters, "mode", "byGroup");
            int iterations = GetParamInt(parameters, "iterations", 1);
            bool feedback = GetParamBool(parameters, "feedback", false);
            string valueAttrib = GetParamString(parameters, "valueAttrib", "");

            if (string.IsNullOrEmpty(subGraphPath))
            {
                ctx.LogWarning("ForEach: subGraphPath is empty");
                return SingleOutput("geometry", geo.Clone());
            }

            var subGraphAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<PCGGraphData>(subGraphPath);
            if (subGraphAsset == null)
            {
                ctx.LogWarning($"ForEach: Failed to load subgraph at {subGraphPath}");
                return SingleOutput("geometry", geo.Clone());
            }

            var pieces = new List<PCGGeometry>();

            switch (mode.ToLower())
            {
                case "bygroup":
                    pieces = IterateByGroup(geo, subGraphAsset, valueAttrib, ctx);
                    break;
                case "bypiece":
                    pieces = IterateByPiece(geo, subGraphAsset, valueAttrib, ctx);
                    break;
                case "count":
                    pieces = IterateByCount(geo, subGraphAsset, iterations, feedback, ctx);
                    break;
                default:
                    ctx.LogWarning($"ForEach: Unknown mode '{mode}', using byGroup");
                    pieces = IterateByGroup(geo, subGraphAsset, valueAttrib, ctx);
                    break;
            }

            var merged = MergeResults(pieces);
            ctx.Log($"ForEach: {pieces.Count} iterations, result {merged.Points.Count} points, {merged.Primitives.Count} prims");
            return SingleOutput("geometry", merged);
        }

        private List<PCGGeometry> IterateByGroup(PCGGeometry geo, PCGGraphData subGraph, string valueAttrib, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();
            int totalIterations = geo.PrimGroups.Count;

            if (geo.PrimGroups.Count == 0)
            {
                ctx.LogWarning("ForEach byGroup: no PrimGroups found, running once with full geometry");
                var result = ExecuteSubGraph(subGraph, geo, ctx, 0, "", totalIterations, 0f);
                if (result != null) results.Add(result);
                return results;
            }

            int iteration = 0;
            foreach (var kvp in geo.PrimGroups)
            {
                var piece = PCGGeometryUtils.ExtractPrimGroup(geo, kvp.Value);
                float value = GetPieceValue(piece, valueAttrib);
                var result = ExecuteSubGraph(subGraph, piece, ctx, iteration, kvp.Key, totalIterations, value);
                if (result != null) results.Add(result);
                iteration++;
            }
            return results;
        }

        private List<PCGGeometry> IterateByPiece(PCGGeometry geo, PCGGraphData subGraph, string valueAttrib, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();
            var pieces = PCGGeometryUtils.SplitByConnectivity(geo);
            int totalIterations = pieces.Count;

            for (int i = 0; i < pieces.Count; i++)
            {
                float value = GetPieceValue(pieces[i], valueAttrib);
                var result = ExecuteSubGraph(subGraph, pieces[i], ctx, i, $"piece_{i}", totalIterations, value);
                if (result != null) results.Add(result);
            }
            return results;
        }

        private List<PCGGeometry> IterateByCount(PCGGeometry geo, PCGGraphData subGraph, int count, bool feedback, PCGContext ctx)
        {
            var results = new List<PCGGeometry>();
            var current = geo;

            for (int i = 0; i < count; i++)
            {
                var result = ExecuteSubGraph(subGraph, current, ctx, i, "", count, i);
                if (result != null)
                {
                    if (feedback)
                    {
                        // feedback 模式：只保留最终结果，用于累积变换
                        current = result;
                    }
                    else
                    {
                        // 非 feedback 模式：收集所有中间结果
                        results.Add(result);
                        current = result;
                    }
                }
            }

            // feedback 模式下只返回最终结果
            if (feedback)
            {
                return new List<PCGGeometry> { current };
            }
            return results;
        }

        private float GetPieceValue(PCGGeometry piece, string valueAttrib)
        {
            if (string.IsNullOrEmpty(valueAttrib))
                return 0f;

            // 尝试从 Detail 属性读取
            var detailAttr = piece.DetailAttribs.GetAttribute(valueAttrib);
            if (detailAttr != null && detailAttr.Values.Count > 0)
            {
                return System.Convert.ToSingle(detailAttr.Values[0]);
            }

            // 尝试从第一个点的属性读取
            var pointAttr = piece.PointAttribs.GetAttribute(valueAttrib);
            if (pointAttr != null && pointAttr.Values.Count > 0)
            {
                return System.Convert.ToSingle(pointAttr.Values[0]);
            }

            return 0f;
        }

        private PCGGeometry ExecuteSubGraph(PCGGraphData subGraph, PCGGeometry inputGeo, PCGContext ctx, int iteration, string groupName, int totalIterations, float value)
        {
            var subExecutor = new PCGGraphExecutor(subGraph);

            // 保存旧值，防止嵌套 ForEach 污染外层变量
            var savedVars = new Dictionary<string, object>(ctx.GlobalVariables);

            ctx.GlobalVariables["iteration"] = (float)iteration;
            ctx.GlobalVariables["groupname"] = groupName;
            ctx.GlobalVariables["numiterations"] = (float)totalIterations;
            ctx.GlobalVariables["value"] = value;
            ctx.SetExternalInput("geometry", inputGeo);

            try
            {
                subExecutor.Execute(ctx);
            }
            catch (System.Exception e)
            {
                ctx.LogError($"ForEach iteration {iteration}: {e.Message}");
                // 恢复旧值
                foreach (var kvp in savedVars) ctx.GlobalVariables[kvp.Key] = kvp.Value;
                return null;
            }

            // 恢复旧值
            foreach (var kvp in savedVars) ctx.GlobalVariables[kvp.Key] = kvp.Value;

            if (ctx.TryGetExternalOutput("geometry", out var output))
                return output;

            return null;
        }

        private PCGGeometry MergeResults(List<PCGGeometry> pieces)
        {
            var result = new PCGGeometry();
            int pointOffset = 0;
            int primOffset = 0;

            foreach (var piece in pieces)
            {
                if (piece == null || piece.Points.Count == 0) continue;

                int piecePointCount = piece.Points.Count;
                int piecePrimCount = piece.Primitives.Count;

                result.Points.AddRange(piece.Points);
                foreach (var prim in piece.Primitives)
                {
                    var newPrim = new int[prim.Length];
                    for (int i = 0; i < prim.Length; i++)
                        newPrim[i] = prim[i] + pointOffset;
                    result.Primitives.Add(newPrim);
                }

                // 合并点属性
                var processedPointAttrNames = new HashSet<string>();
                foreach (var attr in piece.PointAttribs.GetAllAttributes())
                {
                    processedPointAttrNames.Add(attr.Name);
                    var destAttr = result.PointAttribs.GetAttribute(attr.Name);
                    if (destAttr == null)
                    {
                        destAttr = result.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                        for (int j = 0; j < pointOffset; j++)
                            destAttr.Values.Add(destAttr.DefaultValue);
                    }
                    destAttr.Values.AddRange(attr.Values);
                }
                // 补齐dest中已有但piece中没有的点属性
                foreach (var destAttr in result.PointAttribs.GetAllAttributes())
                {
                    if (!processedPointAttrNames.Contains(destAttr.Name))
                    {
                        for (int j = 0; j < piecePointCount; j++)
                            destAttr.Values.Add(destAttr.DefaultValue);
                    }
                }

                // B13-1 fix: 合并面属性
                var processedPrimAttrNames = new HashSet<string>();
                foreach (var attr in piece.PrimAttribs.GetAllAttributes())
                {
                    processedPrimAttrNames.Add(attr.Name);
                    var destAttr = result.PrimAttribs.GetAttribute(attr.Name);
                    if (destAttr == null)
                    {
                        destAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                        for (int j = 0; j < primOffset; j++)
                            destAttr.Values.Add(destAttr.DefaultValue);
                    }
                    destAttr.Values.AddRange(attr.Values);
                }
                // 补齐dest中已有但piece中没有的面属性
                foreach (var destAttr in result.PrimAttribs.GetAllAttributes())
                {
                    if (!processedPrimAttrNames.Contains(destAttr.Name))
                    {
                        for (int j = 0; j < piecePrimCount; j++)
                            destAttr.Values.Add(destAttr.DefaultValue);
                    }
                }

                // 合并点分组
                foreach (var grp in piece.PointGroups)
                {
                    if (!result.PointGroups.ContainsKey(grp.Key))
                        result.PointGroups[grp.Key] = new HashSet<int>();
                    foreach (int idx in grp.Value)
                        result.PointGroups[grp.Key].Add(idx + pointOffset);
                }

                // 合并面分组
                foreach (var grp in piece.PrimGroups)
                {
                    if (!result.PrimGroups.ContainsKey(grp.Key))
                        result.PrimGroups[grp.Key] = new HashSet<int>();
                    foreach (int idx in grp.Value)
                        result.PrimGroups[grp.Key].Add(idx + primOffset);
                }

                pointOffset += piecePointCount;
                primOffset += piecePrimCount;
            }

            return result;
        }
    }
}
