using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 保存为 Prefab 资产
    /// </summary>
    public class SavePrefabNode : PCGNodeBase
    {
        public override string Name => "SavePrefab";
        public override string DisplayName => "Save Prefab";
        public override string Description => "将几何体转换为 Mesh 并保存为 Prefab 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "保存路径（Assets/ 开头，.prefab 后缀）", "Assets/PCGOutput/output.prefab"),
            new PCGParamSchema("material", PCGPortDirection.Input, PCGPortType.String,
                "Material", "材质路径（留空使用默认材质）", ""),
            new PCGParamSchema("generateCollider", PCGPortDirection.Input, PCGPortType.Bool,
                "Generate Collider", "是否添加 MeshCollider", false),
            new PCGParamSchema("isStatic", PCGPortDirection.Input, PCGPortType.Bool,
                "Is Static", "标记为 Static（用于烘焙光照等）", true),
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
                ctx.LogWarning("SavePrefab: 输入几何体为空，跳过保存");
                return SingleOutput("geometry", geo);
            }

            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/output.prefab");
            string materialPath = GetParamString(parameters, "material", "");
            bool generateCollider = GetParamBool(parameters, "generateCollider", false);
            bool isStatic = GetParamBool(parameters, "isStatic", true);

            // 确保目录存在
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 转换为 Mesh
            var mesh = PCGGeometryToMesh.Convert(geo);
            mesh.name = Path.GetFileNameWithoutExtension(assetPath) + "_Mesh";

            // 保存 Mesh 资产
            string meshPath = Path.ChangeExtension(assetPath, ".asset");
            AssetDatabase.CreateAsset(mesh, meshPath);

            // 创建 GameObject
            var go = new GameObject(Path.GetFileNameWithoutExtension(assetPath));
            go.isStatic = isStatic;

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();

            // 应用材质
            if (!string.IsNullOrEmpty(materialPath))
            {
                var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (mat != null)
                    renderer.sharedMaterial = mat;
                else
                    ctx.LogWarning($"SavePrefab: 无法加载材质 {materialPath}");
            }
            else
            {
                renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
            }

            // 添加碰撞体
            if (generateCollider)
            {
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            // 保存为 Prefab
            PrefabUtility.SaveAsPrefabAsset(go, assetPath);
            Object.DestroyImmediate(go);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ctx.Log($"SavePrefab: 已保存到 {assetPath}");
            return SingleOutput("geometry", geo);
        }
    }
}
