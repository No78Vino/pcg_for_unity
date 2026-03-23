using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes
{
    /// <summary>
    /// Reads PCGSelectionState and outputs a PCGGeometry with the selection
    /// encoded as PrimGroups/PointGroups. Reuses SceneObjectInputNode's mesh reading logic.
    /// </summary>
    public class SceneSelectionInputNode : PCGNodeBase
    {
        public override string Name => "SceneSelectionInput";
        public override string DisplayName => "Scene Selection Input";
        public override string Description => "读取场景交互选择结果，输出带选择 Group 的几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.SceneObject,
                displayName: "Target", description: "场景中的 GameObject (需要 MeshFilter)",
                required: false)
            {
                ObjectType = typeof(GameObject),
                AllowSceneObjects = true,
            },
            new PCGParamSchema("groupName", PCGPortDirection.Input, PCGPortType.String,
                displayName: "Group Name", description: "输出的 Group 名称", defaultValue: "selected"),
            new PCGParamSchema("applyTransform", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Apply Transform", description: "是否烘焙世界变换", defaultValue: true),
            new PCGParamSchema("readMaterials", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Read Materials", description: "是否读取材质", defaultValue: true),
            new PCGParamSchema("serializedSelection", PCGPortDirection.Input, PCGPortType.String,
                displayName: "Serialized Selection", description: "序列化的选择数据（自动管理，勿手动修改）", defaultValue: ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                displayName: "Geometry",
                description: "完整几何体，带 PrimGroups/PointGroups 标记选择结果"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string groupName = GetParamString(parameters, "groupName", "selected");
            bool applyTransform = GetParamBool(parameters, "applyTransform", true);
            bool readMaterials = GetParamBool(parameters, "readMaterials", true);
            string serializedSelection = GetParamString(parameters, "serializedSelection", "");

            // Restore selection from serialized data if current selection is empty
            if (PCGSelectionState.SelectionCount == 0 && !string.IsNullOrEmpty(serializedSelection))
                PCGSelectionState.RestoreFromJson(serializedSelection);

            // Try to get geometry from SelectionState first
            PCGGeometry geo = null;

            if (PCGSelectionState.SourceGeometry != null)
            {
                geo = PCGSelectionState.SourceGeometry.Clone();
            }
            else
            {
                // Fallback: read from scene GameObject
                var go = GetParamGameObject(ctx, parameters, "target");
                if (go == null)
                {
                    ctx.LogWarning($"{DisplayName}: No target GameObject and no active selection");
                    return SingleOutput("geometry", new PCGGeometry());
                }

                var mf = go.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null)
                {
                    ctx.LogWarning($"{DisplayName}: GameObject '{go.name}' has no MeshFilter or mesh is null");
                    return SingleOutput("geometry", new PCGGeometry());
                }

                geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);

                if (applyTransform)
                {
                    var matrix = go.transform.localToWorldMatrix;
                    for (int i = 0; i < geo.Points.Count; i++)
                        geo.Points[i] = matrix.MultiplyPoint3x4(geo.Points[i]);

                    var normalAttr = geo.PointAttribs.GetAttribute("N");
                    if (normalAttr != null)
                    {
                        var normalMat = matrix.inverse.transpose;
                        for (int i = 0; i < normalAttr.Values.Count; i++)
                        {
                            if (normalAttr.Values[i] is Vector3 n)
                                normalAttr.Values[i] = normalMat.MultiplyVector(n).normalized;
                        }
                    }
                }

                if (readMaterials)
                {
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null && mr.sharedMaterials != null)
                    {
                        var matAttr = geo.PrimAttribs.CreateAttribute("material", AttribType.String);
                        for (int sub = 0; sub < mf.sharedMesh.subMeshCount; sub++)
                        {
                            string matPath = sub < mr.sharedMaterials.Length && mr.sharedMaterials[sub] != null
                                ? UnityEditor.AssetDatabase.GetAssetPath(mr.sharedMaterials[sub])
                                : "";
                            int triCount = mf.sharedMesh.GetTriangles(sub).Length / 3;
                            for (int t = 0; t < triCount; t++)
                                matAttr.Values.Add(matPath);
                        }
                    }
                }
            }

            // Apply selection as groups
            switch (PCGSelectionState.CurrentMode)
            {
                case PCGSelectMode.Face:
                    if (PCGSelectionState.SelectedPrimIndices.Count > 0)
                    {
                        var validIndices = new HashSet<int>();
                        foreach (int idx in PCGSelectionState.SelectedPrimIndices)
                        {
                            if (idx >= 0 && idx < geo.Primitives.Count)
                                validIndices.Add(idx);
                        }
                        if (validIndices.Count > 0)
                            geo.PrimGroups[groupName] = validIndices;
                    }
                    break;

                case PCGSelectMode.Vertex:
                    if (PCGSelectionState.SelectedPointIndices.Count > 0)
                    {
                        var validIndices = new HashSet<int>();
                        foreach (int idx in PCGSelectionState.SelectedPointIndices)
                        {
                            if (idx >= 0 && idx < geo.Points.Count)
                                validIndices.Add(idx);
                        }
                        if (validIndices.Count > 0)
                            geo.PointGroups[groupName] = validIndices;
                    }
                    break;

                case PCGSelectMode.Edge:
                    if (PCGSelectionState.SelectedEdgeIndices.Count > 0 && geo.Edges.Count > 0)
                    {
                        var edgePointIndices = new HashSet<int>();
                        foreach (int edgeIdx in PCGSelectionState.SelectedEdgeIndices)
                        {
                            if (edgeIdx >= 0 && edgeIdx < geo.Edges.Count)
                            {
                                edgePointIndices.Add(geo.Edges[edgeIdx][0]);
                                edgePointIndices.Add(geo.Edges[edgeIdx][1]);
                            }
                        }
                        if (edgePointIndices.Count > 0)
                            geo.PointGroups[groupName] = edgePointIndices;
                    }
                    break;
            }

            int selCount = PCGSelectionState.SelectionCount;
            ctx.Log($"SceneSelectionInput: mode={PCGSelectionState.CurrentMode}, selected={selCount}, group='{groupName}'");

            // Persist selection data
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.serializedSelection"] = PCGSelectionState.SerializeToJson();

            return SingleOutput("geometry", geo);
        }
    }
}
