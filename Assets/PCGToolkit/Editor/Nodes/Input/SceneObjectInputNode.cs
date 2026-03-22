using System;
using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes
{
    /// <summary>
    /// 从场景 GameObject 的 MeshFilter 读取网格，转换为 PCGGeometry 继续处理。
    /// 场景引用通过 Inspector ObjectField（SceneObject 端口）选择。
    /// </summary>
    public class SceneObjectInputNode : PCGNodeBase
    {
        public override string Name        => "SceneObjectInput";
        public override string DisplayName => "Scene Object Input";
        public override string Description => "读取场景 GameObject 的 MeshFilter，转为 PCGGeometry";
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
            new PCGParamSchema("applyTransform", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Apply Transform", description: "是否将 world 变换烘焙到顶点", defaultValue: false),
            new PCGParamSchema("readMaterials", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Read Materials", description: "是否将材质路径写入 @material 属性", defaultValue: true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                displayName: "Geometry"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var go = GetParamGameObject(ctx, parameters, "target");
            if (go == null)
            {
                ctx.LogWarning($"{DisplayName}: 未指定目标 GameObject");
                return SingleOutput("geometry", new PCGGeometry());
            }

            var mf = go.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
            {
                ctx.LogWarning($"{DisplayName}: GameObject '{go.name}' 没有 MeshFilter 或 sharedMesh 为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            bool applyTransform = GetParamBool(parameters, "applyTransform", false);
            bool readMaterials   = GetParamBool(parameters, "readMaterials", true);

            var geo = PCGGeometryToMesh.FromMesh(mf.sharedMesh);

            // 烘焙 world 变换
            if (applyTransform)
            {
                var matrix = go.transform.localToWorldMatrix;
                for (int i = 0; i < geo.Points.Count; i++)
                    geo.Points[i] = matrix.MultiplyPoint3x4(geo.Points[i]);

                // 法线也需要变换
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

            // 写入材质路径属性
            if (readMaterials)
            {
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterials != null)
                {
                    var matAttr = geo.PrimAttribs.CreateAttribute("material", AttribType.String);
                    // 每个 submesh 对应一个材质；每个面记录对应材质路径
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

            return SingleOutput("geometry", geo);
        }
    }
}
