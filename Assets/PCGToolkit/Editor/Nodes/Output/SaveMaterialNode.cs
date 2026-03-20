using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Output
{
    /// <summary>
    /// 创建并保存 Unity Material
    /// </summary>
    public class SaveMaterialNode : PCGNodeBase
    {
        public override string Name => "SaveMaterial";
        public override string DisplayName => "Save Material";
        public override string Description => "创建并保存 Unity Material 资产";
        public override PCGNodeCategory Category => PCGNodeCategory.Output;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Save Path", "保存路径（Assets/ 开头，.mat 结尾）", "Assets/PCGOutput/material.mat"),
            new PCGParamSchema("shaderName", PCGPortDirection.Input, PCGPortType.String,
                "Shader", "着色器名称", "Standard"),
            new PCGParamSchema("albedoColor", PCGPortDirection.Input, PCGPortType.Color,
                "Albedo Color", "基础颜色", new Color(0.8f, 0.8f, 0.8f, 1f)),
            new PCGParamSchema("albedoTexture", PCGPortDirection.Input, PCGPortType.String,
                "Albedo Texture", "基础颜色纹理路径", ""),
            new PCGParamSchema("metallic", PCGPortDirection.Input, PCGPortType.Float,
                "Metallic", "金属度", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("smoothness", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothness", "平滑度", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("emissionColor", PCGPortDirection.Input, PCGPortType.Color,
                "Emission Color", "自发光颜色（黑色=无自发光）", Color.black),
            new PCGParamSchema("renderMode", PCGPortDirection.Input, PCGPortType.String,
                "Render Mode", "渲染模式（opaque/cutout/transparent/fade）", "opaque"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("material", PCGPortDirection.Output, PCGPortType.String,
                "Material", "创建的 Material 路径"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            string savePath = GetParamString(parameters, "assetPath", "Assets/PCGOutput/material.mat");
            string shaderName = GetParamString(parameters, "shaderName", "Standard");
            Color albedoColor = GetParamColor(parameters, "albedoColor", new Color(0.8f, 0.8f, 0.8f, 1f));
            string albedoTexture = GetParamString(parameters, "albedoTexture", "");
            float metallic = GetParamFloat(parameters, "metallic", 0f);
            float smoothness = GetParamFloat(parameters, "smoothness", 0.5f);
            Color emissionColor = GetParamColor(parameters, "emissionColor", Color.black);
            string renderMode = GetParamString(parameters, "renderMode", "opaque").ToLower();

            // 确保路径以 .mat 结尾
            if (!savePath.EndsWith(".mat"))
                savePath += ".mat";

            // 确保目录存在
            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // 查找着色器
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                ctx.LogWarning($"SaveMaterial: 着色器 '{shaderName}' 未找到，使用 Standard");
            }

            // 创建材质
            var material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(savePath);

            // 设置属性
            material.color = albedoColor;

            // 设置纹理
            if (!string.IsNullOrEmpty(albedoTexture) && File.Exists(albedoTexture))
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoTexture);
                if (texture != null)
                {
                    material.SetTexture("_MainTex", texture);
                }
                else
                {
                    // 尝试从文件加载
                    var bytes = File.ReadAllBytes(albedoTexture);
                    var tex = new Texture2D(2, 2);
                    tex.LoadImage(bytes);
                    material.SetTexture("_MainTex", tex);
                }
            }

            // Standard shader 属性
            if (shaderName.Contains("Standard"))
            {
                material.SetFloat("_Metallic", metallic);
                material.SetFloat("_Glossiness", smoothness);

                if (emissionColor != Color.black)
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emissionColor);
                }

                // 渲染模式
                switch (renderMode)
                {
                    case "cutout":
                        material.SetInt("_Mode", 1);
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.EnableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 2450;
                        break;
                    case "transparent":
                        material.SetInt("_Mode", 3);
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                        break;
                    case "fade":
                        material.SetInt("_Mode", 2);
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        material.SetInt("_ZWrite", 0);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.EnableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = 3000;
                        break;
                    default: // opaque
                        material.SetInt("_Mode", 0);
                        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        material.SetInt("_ZWrite", 1);
                        material.DisableKeyword("_ALPHATEST_ON");
                        material.DisableKeyword("_ALPHABLEND_ON");
                        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                        material.renderQueue = -1;
                        break;
                }
            }

            // 保存材质
            AssetDatabase.CreateAsset(material, savePath);
            AssetDatabase.SaveAssets();

            ctx.Log($"SaveMaterial: 已保存到 {savePath}");

            return new Dictionary<string, PCGGeometry>
            {
                { "material", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", savePath) } }
            };
        }
    }
}