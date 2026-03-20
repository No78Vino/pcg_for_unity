using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 导出 FBX 文件
    /// 注意：需要 com.unity.formats.fbx 包支持
    /// </summary>
    public class ExportFBXNode : PCGNodeBase
    {
        public override string Name => "ExportFBX";
        public override string DisplayName => "Export FBX";
        public override string Description => "将几何体导出为 FBX 文件";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("fbxPath", PCGPortDirection.Input, PCGPortType.String,
                "FBX Path", "导出路径（Assets/ 开头，.fbx 结尾）", "Assets/PCGOutput/output.fbx"),
            new PCGParamSchema("exportMaterials", PCGPortDirection.Input, PCGPortType.Bool,
                "Export Materials", "是否导出材质", true),
            new PCGParamSchema("copyTextures", PCGPortDirection.Input, PCGPortType.Bool,
                "Copy Textures", "是否复制纹理", false),
            new PCGParamSchema("exportAnimations", PCGPortDirection.Input, PCGPortType.Bool,
                "Export Animations", "是否导出动画", false),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "透传输入几何体"),
            new PCGParamSchema("fbxPath", PCGPortDirection.Output, PCGPortType.String,
                "FBX Path", "导出的 FBX 路径"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input");

            if (geo == null || geo.Points.Count == 0)
            {
                ctx.LogWarning("ExportFBX: 输入几何体为空，跳过导出");
                return new Dictionary<string, PCGGeometry>
                {
                    { "geometry", geo },
                    { "fbxPath", null }
                };
            }

            string fbxPath = GetParamString(parameters, "fbxPath", "Assets/PCGOutput/output.fbx");
            bool exportMaterials = GetParamBool(parameters, "exportMaterials", true);

            // 确保路径以 .fbx 结尾
            if (!fbxPath.EndsWith(".fbx"))
                fbxPath += ".fbx";

            // 确保目录存在
            string directory = Path.GetDirectoryName(fbxPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 转换为 Mesh
            var mesh = PCGGeometryToMesh.Convert(geo);
            mesh.name = Path.GetFileNameWithoutExtension(fbxPath);

            // 创建临时 GameObject
            var go = new GameObject(mesh.name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat");

            // 尝试使用 FBX 导出器
            // 注意：这需要 com.unity.formats.fbx 包
            // 如果包不存在，使用 OBJ 作为替代
            bool fbxExportSuccess = false;

            try
            {
                // 检查 FBX 导出器是否可用
                var fbxExporterType = System.Type.GetType("UnityEditor.Formats.Fbx.Exporter.ModelExporter, Unity.Formats.Fbx.Editor");
                if (fbxExporterType != null)
                {
                    // 使用反射调用 FBX 导出
                    var exportMethod = fbxExporterType.GetMethod("ExportObject",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                        null,
                        new System.Type[] { typeof(string), typeof(UnityEngine.Object) },
                        null);

                    if (exportMethod != null)
                    {
                        exportMethod.Invoke(null, new object[] { fbxPath, go });
                        fbxExportSuccess = true;
                        ctx.Log($"ExportFBX: 已导出到 {fbxPath}");
                    }
                }
            }
            catch (System.Exception e)
            {
                ctx.LogWarning($"ExportFBX: FBX 导出失败 - {e.Message}，尝试 OBJ 格式");
            }

            // 如果 FBX 导出失败，使用 OBJ 格式
            if (!fbxExportSuccess)
            {
                string objPath = Path.ChangeExtension(fbxPath, ".obj");
                ExportToOBJ(go, objPath);
                fbxPath = objPath;
                ctx.Log($"ExportFBX: 已导出为 OBJ 格式 {objPath}");
            }

            Object.DestroyImmediate(go);

            return new Dictionary<string, PCGGeometry>
            {
                { "geometry", geo },
                { "fbxPath", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", fbxPath) } }
            };
        }

        private void ExportToOBJ(GameObject go, string path)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;

            var mesh = meshFilter.sharedMesh;
            var sb = new System.Text.StringBuilder();

            sb.AppendLine($"# OBJ file exported from PCGToolkit");
            sb.AppendLine($"o {go.name}");

            // 顶点
            foreach (var v in mesh.vertices)
            {
                sb.AppendLine($"v {v.x} {v.y} {v.z}");
            }

            // UV
            if (mesh.uv.Length > 0)
            {
                foreach (var uv in mesh.uv)
                {
                    sb.AppendLine($"vt {uv.x} {uv.y}");
                }
            }

            // 法线
            foreach (var n in mesh.normals)
            {
                sb.AppendLine($"vn {n.x} {n.y} {n.z}");
            }

            // 面
            int[] triangles = mesh.triangles;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i] + 1;
                int i1 = triangles[i + 1] + 1;
                int i2 = triangles[i + 2] + 1;

                if (mesh.uv.Length > 0)
                {
                    sb.AppendLine($"f {i0}/{i0}/{i0} {i1}/{i1}/{i1} {i2}/{i2}/{i2}");
                }
                else
                {
                    sb.AppendLine($"f {i0}//{i0} {i1}//{i1} {i2}//{i2}");
                }
            }

            File.WriteAllText(path, sb.ToString());
            AssetDatabase.ImportAsset(path);
        }
    }
}