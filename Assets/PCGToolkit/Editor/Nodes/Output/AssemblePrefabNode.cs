using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 层级 Prefab 组装节点。
    /// 接收多个 Geometry 输入（input0~input7），每个 Geometry 成为子物体。
    /// 子物体名称优先读取 Geometry 的 @name Detail 属性，否则用 "Part_{i}"。
    /// 子物体的局部 Transform 可通过 @position / @rotation / @scale Detail 属性控制。
    /// </summary>
    public class AssemblePrefabNode : PCGNodeBase
    {
        public override string Name        => "AssemblePrefab";
        public override string DisplayName => "Assemble Prefab";
        public override string Description => "将多个 Geometry 组装为带层级结构的 Prefab（父物体 + 多子物体）";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        private const int MaxInputs = 8;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input0", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 0", "子部件 0", null, required: true),
            new PCGParamSchema("input1", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 1", "子部件 1", null, required: false),
            new PCGParamSchema("input2", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 2", "子部件 2", null, required: false),
            new PCGParamSchema("input3", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 3", "子部件 3", null, required: false),
            new PCGParamSchema("input4", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 4", "子部件 4", null, required: false),
            new PCGParamSchema("input5", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 5", "子部件 5", null, required: false),
            new PCGParamSchema("input6", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 6", "子部件 6", null, required: false),
            new PCGParamSchema("input7", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input 7", "子部件 7", null, required: false),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Save Path", "Prefab 保存路径（Assets/ 开头，.prefab 结尾）",
                "Assets/PCGOutput/assembly.prefab"),
            new PCGParamSchema("rootName", PCGPortDirection.Input, PCGPortType.String,
                "Root Name", "根物体名称", "PCG_Assembly"),
            new PCGParamSchema("addColliders", PCGPortDirection.Input, PCGPortType.Bool,
                "Add Colliders", "是否为每个子物体添加 MeshCollider", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("prefabPath", PCGPortDirection.Output, PCGPortType.String,
                "Prefab Path", "保存的 Prefab 路径"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string savePath   = GetParamString(parameters, "assetPath", "Assets/PCGOutput/assembly.prefab");
            string rootName   = GetParamString(parameters, "rootName", "PCG_Assembly");
            bool addColliders = GetParamBool(parameters, "addColliders", false);

            if (!savePath.EndsWith(".prefab")) savePath += ".prefab";

            string dir = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 收集有效输入
            var parts = new List<(string portName, PCGGeometry geo)>();
            for (int i = 0; i < MaxInputs; i++)
            {
                string port = $"input{i}";
                if (inputGeometries.TryGetValue(port, out var geo) && geo != null && geo.Points.Count > 0)
                    parts.Add((port, geo));
            }

            if (parts.Count == 0)
            {
                ctx.LogWarning("AssemblePrefab: 没有有效的输入几何体，跳过");
                return new Dictionary<string, PCGGeometry>
                {
                    { "prefabPath", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", "") } }
                };
            }

            // 创建根物体
            var root = new GameObject(rootName);
            int builtCount = 0;

            for (int i = 0; i < parts.Count; i++)
            {
                var (portName, geo) = parts[i];

                // 读取子物体名称（@name Detail 属性）
                string childName = $"Part_{i}";
                var nameAttr = geo.DetailAttribs?.GetAttribute("name");
                if (nameAttr?.Values?.Count > 0 && nameAttr.Values[0] is string s && !string.IsNullOrEmpty(s))
                    childName = s;

                // 转换为 Mesh（多材质支持）
                var meshResult = PCGGeometryToMesh.ConvertWithSubmeshes(geo);
                var mesh = meshResult.Mesh;
                mesh.name = childName;

                // 创建子物体
                var child = new GameObject(childName);
                child.transform.SetParent(root.transform, false);

                // 读取 @position / @rotation / @scale Detail 属性
                ApplyDetailTransform(child.transform, geo);

                child.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = child.AddComponent<MeshRenderer>();

                // 材质
                var defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");
                int matCount = meshResult.MaterialPaths?.Count ?? 0;
                var mats = new Material[Mathf.Max(1, matCount)];
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = null;
                    if (m < matCount && !string.IsNullOrEmpty(meshResult.MaterialPaths[m]))
                        mat = AssetDatabase.LoadAssetAtPath<Material>(meshResult.MaterialPaths[m]);
                    mats[m] = mat != null ? mat : defaultMat;
                }
                renderer.sharedMaterials = mats;

                if (addColliders)
                {
                    var col = child.AddComponent<MeshCollider>();
                    col.sharedMesh = mesh;
                }

                builtCount++;
            }

            // 保存 Prefab
            bool savedOk = false;
            try
            {
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, savePath);
                savedOk = prefab != null;
            }
            catch (System.Exception e)
            {
                ctx.LogWarning($"AssemblePrefab: 保存失败 - {e.Message}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            if (savedOk)
            {
                AssetDatabase.Refresh();
                ctx.Log($"AssemblePrefab: 已保存 {builtCount} 个子部件到 {savePath}");
            }

            return new Dictionary<string, PCGGeometry>
            {
                { "prefabPath", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", savedOk ? savePath : "") } }
            };
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────

        private static void ApplyDetailTransform(Transform t, PCGGeometry geo)
        {
            if (geo.DetailAttribs == null) return;

            var posAttr = geo.DetailAttribs.GetAttribute("position");
            if (posAttr?.Values?.Count > 0 && posAttr.Values[0] is Vector3 pos)
                t.localPosition = pos;

            var rotAttr = geo.DetailAttribs.GetAttribute("rotation");
            if (rotAttr?.Values?.Count > 0)
            {
                if (rotAttr.Values[0] is Vector3 euler)
                    t.localRotation = Quaternion.Euler(euler);
                else if (rotAttr.Values[0] is Vector4 q)
                    t.localRotation = new Quaternion(q.x, q.y, q.z, q.w);
            }

            var scaleAttr = geo.DetailAttribs.GetAttribute("scale");
            if (scaleAttr?.Values?.Count > 0)
            {
                if (scaleAttr.Values[0] is Vector3 sc)
                    t.localScale = sc;
                else if (scaleAttr.Values[0] is float sf)
                    t.localScale = Vector3.one * sf;
            }
        }
    }
}
