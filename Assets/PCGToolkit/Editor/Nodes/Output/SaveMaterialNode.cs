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
            new PCGParamSchema("shaderType", PCGPortDirection.Input, PCGPortType.String,
                "Shader Type", "着色器类型", "Standard")
            {
                EnumOptions = new[] { "Standard", "URP_Lit", "HDRP_Lit", "Custom" }
            },
            new PCGParamSchema("customShader", PCGPortDirection.Input, PCGPortType.String,
                "Custom Shader", "自定义着色器名称（shaderType=Custom 时生效）", ""),
            new PCGParamSchema("albedoColor", PCGPortDirection.Input, PCGPortType.Color,
                "Albedo Color", "基础颜色", new Color(0.8f, 0.8f, 0.8f, 1f)),
            new PCGParamSchema("albedoTexture", PCGPortDirection.Input, PCGPortType.String,
                "Albedo Texture", "基础颜色纹理路径（Assets/...）", ""),
            new PCGParamSchema("normalMapPath", PCGPortDirection.Input, PCGPortType.String,
                "Normal Map", "法线贴图路径（Assets/...）", ""),
            new PCGParamSchema("metallicMapPath", PCGPortDirection.Input, PCGPortType.String,
                "Metallic Map", "金属度/粗糙度贴图路径（Assets/...）", ""),
            new PCGParamSchema("occlusionMapPath", PCGPortDirection.Input, PCGPortType.String,
                "Occlusion Map", "环境遮蔽贴图路径（Assets/...）", ""),
            new PCGParamSchema("metallic", PCGPortDirection.Input, PCGPortType.Float,
                "Metallic", "金属度", 0f) { Min = 0f, Max = 1f },
            new PCGParamSchema("smoothness", PCGPortDirection.Input, PCGPortType.Float,
                "Smoothness", "平滑度", 0.5f) { Min = 0f, Max = 1f },
            new PCGParamSchema("emissionColor", PCGPortDirection.Input, PCGPortType.Color,
                "Emission Color", "自发光颜色（黑色=无自发光）", Color.black),
            new PCGParamSchema("tiling", PCGPortDirection.Input, PCGPortType.Vector3,
                "Tiling", "贴图 Tiling（XY 有效，Z 忽略）", new Vector3(1f, 1f, 0f)),
            new PCGParamSchema("texOffset", PCGPortDirection.Input, PCGPortType.Vector3,
                "Offset", "贴图 Offset（XY 有效，Z 忽略）", Vector3.zero),
            new PCGParamSchema("renderMode", PCGPortDirection.Input, PCGPortType.String,
                "Render Mode", "渲染模式（opaque/cutout/transparent/fade）", "opaque")
            {
                EnumOptions = new[] { "opaque", "cutout", "transparent", "fade" }
            },
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
            string savePath        = GetParamString(parameters, "assetPath", "Assets/PCGOutput/material.mat");
            string shaderType      = GetParamString(parameters, "shaderType", "Standard");
            string customShader    = GetParamString(parameters, "customShader", "");
            Color  albedoColor     = GetParamColor(parameters, "albedoColor", new Color(0.8f, 0.8f, 0.8f, 1f));
            string albedoTex       = GetParamString(parameters, "albedoTexture", "");
            string normalMapPath   = GetParamString(parameters, "normalMapPath", "");
            string metallicMapPath = GetParamString(parameters, "metallicMapPath", "");
            string occlusionPath   = GetParamString(parameters, "occlusionMapPath", "");
            float  metallic        = GetParamFloat(parameters, "metallic", 0f);
            float  smoothness      = GetParamFloat(parameters, "smoothness", 0.5f);
            Color  emissionColor   = GetParamColor(parameters, "emissionColor", Color.black);
            Vector3 tiling3        = GetParamVector3(parameters, "tiling", new Vector3(1f, 1f, 0f));
            Vector3 offset3        = GetParamVector3(parameters, "texOffset", Vector3.zero);
            string renderMode      = GetParamString(parameters, "renderMode", "opaque").ToLower();

            var tiling = new Vector2(tiling3.x, tiling3.y);
            var texOffset = new Vector2(offset3.x, offset3.y);

            if (!savePath.EndsWith(".mat")) savePath += ".mat";

            string directory = Path.GetDirectoryName(savePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            // 解析 shader 名称
            string shaderName = ResolveShaderName(shaderType, customShader);
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                shader = Shader.Find("Standard");
                ctx.LogWarning($"SaveMaterial: 着色器 '{shaderName}' 未找到，回退到 Standard");
                shaderName = "Standard";
            }

            var material = new Material(shader);
            material.name = Path.GetFileNameWithoutExtension(savePath);

            // Albedo
            material.color = albedoColor;
            ApplyTexture(material, albedoTex, MainTexProp(shaderName));

            // Tiling / Offset
            material.mainTextureScale  = tiling;
            material.mainTextureOffset = texOffset;

            // Normal Map
            if (!string.IsNullOrEmpty(normalMapPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalMapPath);
                if (tex != null)
                {
                    material.EnableKeyword("_NORMALMAP");
                    material.SetTexture(NormalMapProp(shaderName), tex);
                }
            }

            // Metallic Map
            if (!string.IsNullOrEmpty(metallicMapPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(metallicMapPath);
                if (tex != null)
                {
                    material.EnableKeyword("_METALLICGLOSSMAP");
                    material.SetTexture(MetallicMapProp(shaderName), tex);
                }
            }

            // Occlusion Map
            if (!string.IsNullOrEmpty(occlusionPath))
            {
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(occlusionPath);
                if (tex != null)
                    material.SetTexture(OcclusionProp(shaderName), tex);
            }

            // Metallic / Smoothness scalars
            SetFloatSafe(material, MetallicProp(shaderName), metallic);
            SetFloatSafe(material, SmoothnessProp(shaderName), smoothness);

            // Emission
            if (emissionColor != Color.black)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(EmissionProp(shaderName), emissionColor);
            }

            // Render Mode（Standard only — URP/HDRP 用 Surface Type 设置，不同 shader）
            if (shaderName.Contains("Standard"))
                ApplyStandardRenderMode(material, renderMode);

            AssetDatabase.CreateAsset(material, savePath);
            AssetDatabase.SaveAssets();
            ctx.Log($"SaveMaterial: 已保存到 {savePath} (shader={shaderName})");

            return new Dictionary<string, PCGGeometry>
            {
                { "material", new PCGGeometry { DetailAttribs = new AttributeStore().SetAttribute("value", savePath) } }
            };
        }

        // ── 辅助：Shader 名称解析 ────────────────────────────────────────────────
        private static string ResolveShaderName(string shaderType, string custom)
        {
            switch (shaderType)
            {
                case "URP_Lit":  return "Universal Render Pipeline/Lit";
                case "HDRP_Lit": return "HDRP/Lit";
                case "Custom":   return string.IsNullOrEmpty(custom) ? "Standard" : custom;
                default:         return "Standard";
            }
        }

        // ── 辅助：各 shader 的属性名 ─────────────────────────────────────────────
        private static string MainTexProp(string s)    => s.Contains("Universal") || s.Contains("HDRP") ? "_BaseMap"    : "_MainTex";
        private static string NormalMapProp(string s)  => "_BumpMap";
        private static string MetallicMapProp(string s)=> s.Contains("Universal") || s.Contains("HDRP") ? "_MetallicGlossMap" : "_MetallicGlossMap";
        private static string OcclusionProp(string s)  => "_OcclusionMap";
        private static string MetallicProp(string s)   => "_Metallic";
        private static string SmoothnessProp(string s) => s.Contains("Universal") || s.Contains("HDRP") ? "_Smoothness" : "_Glossiness";
        private static string EmissionProp(string s)   => s.Contains("Universal") || s.Contains("HDRP") ? "_EmissionColor" : "_EmissionColor";

        private static void ApplyTexture(Material mat, string path, string prop)
        {
            if (string.IsNullOrEmpty(path)) return;
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex != null) mat.SetTexture(prop, tex);
        }

        private static void SetFloatSafe(Material mat, string prop, float val)
        {
            if (mat.HasProperty(prop)) mat.SetFloat(prop, val);
        }

        private static void ApplyStandardRenderMode(Material mat, string mode)
        {
            switch (mode)
            {
                case "cutout":
                    mat.SetInt("_Mode", 1);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.EnableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 2450;
                    break;
                case "transparent":
                    mat.SetInt("_Mode", 3);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    break;
                case "fade":
                    mat.SetInt("_Mode", 2);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = 3000;
                    break;
                default: // opaque
                    mat.SetInt("_Mode", 0);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = -1;
                    break;
            }
        }
    }
}