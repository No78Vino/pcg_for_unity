using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 将 PCGGeometry 导出为 Unity Mesh 资产和 Prefab
    /// </summary>
    public class ExportMeshNode : PCGNodeBase
    {
        public override string Name => "ExportMesh";
        public override string DisplayName => "Export Mesh";
        public override string Description => "将几何体导出为 Unity Mesh 资产和 Prefab";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Save Path", "保存路径（Assets/ 开头，.prefab 结尾）", "Assets/PCGOutput/output.prefab"),
            new PCGParamSchema("createRenderer", PCGPortDirection.Input, PCGPortType.Bool,
                "Create Renderer", "是否创建 MeshRenderer 并保存为 Prefab", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");

            if (geo == null || geo.Points.Count == 0)
            {
                ctx.LogWarning("ExportMesh: 输入几何体为空，跳过导出");
                return SingleOutput("geometry", geo);
            }

            string savePath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/output.prefab");
            bool createRenderer = GetParamBool(parameters, "createRenderer", true);

            // 确保目录存在
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 转换为 Mesh
            var mesh = PCGGeometryToMesh.Convert(geo);
            mesh.name = Path.GetFileNameWithoutExtension(savePath) + "_Mesh";

            // 保存 Mesh 资产
            string meshAssetPath = Path.ChangeExtension(savePath, ".asset");
            AssetDatabase.CreateAsset(mesh, meshAssetPath);
            ctx.Log($"ExportMesh: Mesh 已保存到 {meshAssetPath}");

            if (createRenderer)
            {
                // 创建临时 GameObject
                var go = new GameObject(Path.GetFileNameWithoutExtension(savePath));
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                // 保存为 Prefab
                string prefabPath = Path.ChangeExtension(savePath, ".prefab");
                PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
                Object.DestroyImmediate(go);
                ctx.Log($"ExportMesh: Prefab 已保存到 {prefabPath}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return SingleOutput("geometry", geo);
        }
    }
}