using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 保存为 Unity Prefab
    /// </summary>
    public class SavePrefabNode : PCGNodeBase
    {
        public override string Name => "SavePrefab";
        public override string DisplayName => "Save Prefab";
        public override string Description => "将几何体保存为 Unity Prefab 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Save Path", "保存路径（Assets/ 开头，.prefab 结尾）", "Assets/PCGOutput/output.prefab"),
            new PCGParamSchema("prefabName", PCGPortDirection.Input, PCGPortType.String,
                "Prefab Name", "Prefab 名称（留空则使用路径中的文件名）", ""),
            new PCGParamSchema("addCollider", PCGPortDirection.Input, PCGPortType.Bool,
                "Add Collider", "是否添加 MeshCollider", false),
            new PCGParamSchema("convexCollider", PCGPortDirection.Input, PCGPortType.Bool,
                "Convex Collider", "碰撞体是否为凸包", false),
            new PCGParamSchema("enabled", PCGPortDirection.Input, PCGPortType.Bool,
                "Enabled", "是否执行导出", true),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体"),
            // prefabPath 通过 ctx.GlobalVariables 传递，格式: {nodeId}.prefabPath
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");
            if (!GetParamBool(parameters, "enabled", true))
                return SingleOutput("geometry", geo ?? new PCGGeometry());

            if (geo == null || geo.Points.Count == 0)
            {
                ctx.LogWarning("SavePrefab: 输入几何体为空，跳过保存");
                return SingleOutput("geometry", geo ?? new PCGGeometry());
            }

            string savePath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/output.prefab");
            string prefabName = GetParamString(parameters, "prefabName", "");
            bool addCollider = GetParamBool(parameters, "addCollider", false);
            bool convexCollider = GetParamBool(parameters, "convexCollider", false);

            // 确保路径以 .prefab 结尾
            if (!savePath.EndsWith(".prefab"))
                savePath += ".prefab";

            // 确定名称
            if (string.IsNullOrEmpty(prefabName))
                prefabName = Path.GetFileNameWithoutExtension(savePath);

            // 确保目录存在
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 转换为 Mesh（支持多 Submesh）
            var meshResult = PCGGeometryToMesh.ConvertWithSubmeshes(geo);
            var mesh = meshResult.Mesh;
            mesh.name = prefabName + "_Mesh";

            // 创建临时 GameObject
            var go = new GameObject(prefabName);

            // 添加 MeshFilter
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            // 添加 MeshRenderer 并分配材质
            var renderer = go.AddComponent<MeshRenderer>();
            var materials = new List<Material>();

            foreach (var matPath in meshResult.MaterialPaths)
            {
                Material mat = null;

                // 尝试加载材质
                if (!string.IsNullOrEmpty(matPath) && matPath != "default")
                {
                    // 尝试多种路径格式
                    string[] tryPaths = new string[]
                    {
                        matPath,
                        matPath.EndsWith(".mat") ? matPath : matPath + ".mat",
                        $"Assets/{matPath}",
                        $"Assets/{matPath}.mat",
                        $"Assets/Materials/{matPath}.mat"
                    };

                    foreach (var tryPath in tryPaths)
                    {
                        mat = AssetDatabase.LoadAssetAtPath<Material>(tryPath);
                        if (mat != null) break;
                    }
                }

                // Fallback 到默认材质
                if (mat == null)
                    mat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

                materials.Add(mat);
            }

            renderer.sharedMaterials = materials.ToArray();

            // 添加碰撞体
            if (addCollider)
            {
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
                collider.convex = convexCollider;
            }

            // 保存 Mesh 资产
            string meshAssetPath = Path.Combine(
                Path.GetDirectoryName(savePath),
                Path.GetFileNameWithoutExtension(savePath) + "_Mesh.asset");
            AssetDatabase.CreateAsset(mesh, meshAssetPath);

            // 保存为 Prefab
            PrefabUtility.SaveAsPrefabAsset(go, savePath);
            Object.DestroyImmediate(go);

            ctx.Log($"SavePrefab: 已保存到 {savePath} ({mesh.subMeshCount} submeshes, {materials.Count} materials)");

            // 将 prefabPath 通过 GlobalVariables 传递
            ctx.GlobalVariables[$"{ctx.CurrentNodeId}.prefabPath"] = savePath;

            return SingleOutput("geometry", geo);
        }
    }
}