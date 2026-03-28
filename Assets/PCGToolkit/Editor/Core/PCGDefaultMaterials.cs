using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG 官方默认材质管理器。提供灰调 PBR 展示材质，用于节点预览和 Live 模式。
    /// </summary>
    public static class PCGDefaultMaterials
    {
        private const string SHADER_PATH = "Hidden/PCG/SimplePBR";
        // URP Lit shader（项目使用 URP）
        private const string FALLBACK_SHADER = "Universal Render Pipeline/Lit";
        private const string FINAL_FALLBACK = "Diffuse";

        private static Material _defaultPBRMaterial;

        /// <summary>
        /// 获取 PCG 官方灰调 PBR 材质（单例缓存）。
        /// 加载链: Hidden/PCG/SimplePBR → URP/Lit → Diffuse
        /// </summary>
        public static Material GetDefaultMaterial()
        {
            if (_defaultPBRMaterial != null)
                return _defaultPBRMaterial;

            Shader shader = Shader.Find(SHADER_PATH);
            if (shader == null || shader.name.Contains("Error"))
            {
                shader = Shader.Find(FALLBACK_SHADER);
                if (shader == null || shader.name.Contains("Error"))
                {
                    shader = Shader.Find(FINAL_FALLBACK);
                }
            }

            if (shader != null)
            {
                _defaultPBRMaterial = new Material(shader);
                _defaultPBRMaterial.hideFlags = HideFlags.HideAndDontSave;

                // URP Lit 和自定义 shader 使用 _BaseColor
                if (_defaultPBRMaterial.HasProperty("_BaseColor"))
                    _defaultPBRMaterial.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f));
                // 内置 Standard / Diffuse 使用 _Color
                if (_defaultPBRMaterial.HasProperty("_Color"))
                    _defaultPBRMaterial.SetColor("_Color", new Color(0.85f, 0.85f, 0.85f));
            }
            else
            {
                // Last resort: 使用 Unity 内置 Default-Diffuse
                _defaultPBRMaterial = new Material(UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"));
                _defaultPBRMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _defaultPBRMaterial;
        }

        /// <summary>
        /// 清除缓存的材质实例，下次 GetDefaultMaterial 会重新创建。
        /// </summary>
        public static void Reset()
        {
            if (_defaultPBRMaterial != null)
            {
                Object.DestroyImmediate(_defaultPBRMaterial);
                _defaultPBRMaterial = null;
            }
        }
    }
}
