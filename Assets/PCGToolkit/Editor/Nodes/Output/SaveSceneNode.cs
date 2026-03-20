using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 组装并保存 Unity Scene
    /// </summary>
    public class SaveSceneNode : PCGNodeBase
    {
        public override string Name => "SaveScene";
        public override string DisplayName => "Save Scene";
        public override string Description => "组装几何体并保存为 Unity Scene";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("scenePath", PCGPortDirection.Input, PCGPortType.String,
                "Scene Path", "场景保存路径（Assets/ 开头，.unity 结尾）", "Assets/PCGOutput/PCGScene.unity"),
            new PCGParamSchema("sceneName", PCGPortDirection.Input, PCGPortType.String,
                "Scene Name", "场景名称（留空则使用路径中的文件名）", ""),
            new PCGParamSchema("createNewScene", PCGPortDirection.Input, PCGPortType.Bool,
                "Create New Scene", "是否创建新场景（false 则追加到当前场景）", true),
            new PCGParamSchema("objectName", PCGPortDirection.Input, PCGPortType.String,
                "Object Name", "生成的 GameObject 名称", "PCGOutput"),
            new PCGParamSchema("addCollider", PCGPortDirection.Input, PCGPortType.Bool,
                "Add Collider", "是否添加碰撞体", false),
            new PCGParamSchema("position", PCGPortDirection.Input, PCGPortType.Vector3,
                "Position", "物体位置", Vector3.zero),
            new PCGParamSchema("rotation", PCGPortDirection.Input, PCGPortType.Vector3,
                "Rotation", "物体旋转（欧拉角）", Vector3.zero),
            new PCGParamSchema("scale", PCGPortDirection.Input, PCGPortType.Vector3,
                "Scale", "物体缩放", Vector3.one),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体"),
            new PCGParamSchema("scenePath", PCGPortDirection.Output, PCGPortType.String,
                "Scene Path", "保存的场景路径"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");

            if (geo == null || geo.Points.Count == 0)
            {
                ctx.LogWarning("SaveScene: 输入几何体为空，跳过保存");
                return new Dictionary<string, PCGGeometry>
                {
                    { "geometry", geo },
                    { "scenePath", null }
                };
            }

            string scenePath = GetParamString(parameters, "scenePath", "Assets/PCGOutput/PCGScene.unity");
            string sceneName = GetParamString(parameters, "sceneName", "");
            bool createNewScene = GetParamBool(parameters, "createNewScene", true);
            string objectName = GetParamString(parameters, "objectName", "PCGOutput");
            bool addCollider = GetParamBool(parameters, "addCollider", false);
            Vector3 position = GetParamVector3(parameters, "position", Vector3.zero);
            Vector3 rotation = GetParamVector3(parameters, "rotation", Vector3.zero);
            Vector3 scale = GetParamVector3(parameters, "scale", Vector3.one);

            // 确保路径以 .unity 结尾
            if (!scenePath.EndsWith(".unity"))
                scenePath += ".unity";

            // 确保目录存在
            string directory = Path.GetDirectoryName(scenePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 确定场景名称
            if (string.IsNullOrEmpty(sceneName))
                sceneName = Path.GetFileNameWithoutExtension(scenePath);

            // 保存当前场景
            var currentScene = SceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;

            Scene targetScene;

            if (createNewScene)
            {
                // 创建新场景
                targetScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                targetScene.name = sceneName;
            }
            else
            {
                targetScene = currentScene;
            }

            // 转换为 Mesh
            var mesh = PCGGeometryToMesh.Convert(geo);
            mesh.name = objectName + "_Mesh";

            // 创建 GameObject
            var go = new GameObject(objectName);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(rotation);
            go.transform.localScale = scale;

            // 添加 MeshFilter
            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            // 添加 MeshRenderer
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

            // 添加碰撞体
            if (addCollider)
            {
                var collider = go.AddComponent<MeshCollider>();
                collider.sharedMesh = mesh;
            }

            // 保存 Mesh 资产
            string meshAssetPath = Path.Combine(
                Path.GetDirectoryName(scenePath),
                Path.GetFileNameWithoutExtension(scenePath) + "_Mesh.asset");
            AssetDatabase.CreateAsset(mesh, meshAssetPath);

            // 保存场景
            EditorSceneManager.SaveScene(targetScene, scenePath);

            ctx.Log($"SaveScene: 已保存到 {scenePath}");

            return new Dictionary<string, PCGGeometry>
            {
                { "geometry", geo },
                { "scenePath", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", scenePath) } }
            };
        }
    }
}