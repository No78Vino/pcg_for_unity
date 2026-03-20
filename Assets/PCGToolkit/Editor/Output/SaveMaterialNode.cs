using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 创建并保存材质资产
    /// </summary>
    public class SaveMaterialNode : PCGNodeBase
    {
        public override string Name => "SaveMaterial";
        public override string DisplayName => "Save Material";
        public override string Description => "根据参数创建并保存 Unity Material 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体（透传）", null, required: false),
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "材质保存路径", "Assets/PCGOutput/material.mat"),
            new PCGParamSchema("shader", PCGPortDirection.Input, PCGPortType.String,
                "Shader", "Shader 名称", "Standard"),
            new PCGParamSchema("color", PCGPortDirection.Input, PCGPortType.Color,
                "Color", "基础颜色", new Color(0.8f, 0.8f, 0.8f, 1f)),
            new PCGParamSchema("metallic", PCGPortDirection.Input, PCGPortType.Float,
                "Metallic", "金属度", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("smoothness", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothness", "光滑度", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("albedoTexture", PCGPortDirection.Input, PCGPortType.String,
                "Albedo Texture", "漫反射贴图路径", ""),
            new PCGParamSchema("normalTexture", PCGPortDirection.Input, PCGPortType.String,
                "Normal Texture", "法线贴图路径", ""),
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

            string assetPath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/material.mat");
            string shaderName = GetParamString(parameters, "shader", "Standard");
            Color color = GetParamColor(parameters, "color", new Color(0.8f, 0.8f, 0.8f, 1f));
            float metallic = GetParamFloat(parameters, "metallic", 0f);
            float smoothness = GetParamFloat(parameters, "smoothness", 0.5f);
            string albedoTexture = GetParamString(parameters, "albedoTexture", "");
            string normalTexture = GetParamString(parameters, "normalTexture", "");

            // 确保目录存在
            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 查找 Shader
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                ctx.LogWarning($"SaveMaterial: 无法找到 Shader '{shaderName}'，使用 Standard");
                shader = Shader.Find("Standard");
            }

            // 创建材质
            var material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(assetPath);

            // 设置属性
            material.color = color;

            // Standard Shader 特定属性
            if (shaderName.Contains("Standard") || shaderName == "Standard")
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", smoothness);
            }

            // 加载贴图
            if (!string.IsNullOrEmpty(albedoTexture))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(albedoTexture);
                if (tex != null)
                    material.mainTexture = tex;
                else
                    ctx.LogWarning($"SaveMaterial: 无法加载贴图 {albedoTexture}");
            }

            if (!string.IsNullOrEmpty(normalTexture))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture>(normalTexture);
                if (tex != null)
                    material.SetTexture("_BumpMap", tex);
            }

            // 保存材质
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            ctx.Log($"SaveMaterial: 已保存到 {assetPath}");
            return SingleOutput("geometry", geo);
        }

        protected Color GetParamColor(Dictionary<string, object> parameters, string name, Color defaultValue)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is Color c) return c;
                if (val is string s && ColorUtility.TryParseHtmlString(s, out var parsed))
                    return parsed;
            }
            return defaultValue;
        }
    }
}
