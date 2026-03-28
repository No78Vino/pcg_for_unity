using UnityEngine;
using UnityEngine.Rendering;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG 默认材质管理器。提供灰调展示材质，用于节点预览和 Live 模式。
    /// </summary>
    public static class PCGDefaultMaterials
    {
        // URP Lit shader（项目使用 URP）
        private const string SHADER_PATH = "Universal Render Pipeline/Lit";

        private static Material _defaultMaterial;
        private static Light _defaultLight;

        /// <summary>
        /// 获取灰色材质（单例缓存）。自动确保场景有正确的光源。
        /// </summary>
        public static Material GetDefaultMaterial()
        {
            EnsureDefaultLight();

            if (_defaultMaterial != null)
                return _defaultMaterial;

            Shader shader = Shader.Find(SHADER_PATH);
            if (shader != null)
            {
                _defaultMaterial = new Material(shader);
                _defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
                _defaultMaterial.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f));
                _defaultMaterial.SetFloat("_ReceiveShadows", 1.0f);
            }
            else
            {
                _defaultMaterial = new Material(UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Diffuse.mat"));
                _defaultMaterial.hideFlags = HideFlags.HideAndDontSave;
            }

            return _defaultMaterial;
        }

        /// <summary>
        /// 确保场景有正确的主光源（开启阴影）。
        /// </summary>
        private static void EnsureDefaultLight()
        {
            // 查找现有的主光源
            Light mainLight = FindMainLight();

            if (mainLight == null)
            {
                // 创建默认光源
                GameObject lightObj = new GameObject("PCG_DefaultLight");
                lightObj.hideFlags = HideFlags.HideAndDontSave;
                mainLight = lightObj.AddComponent<Light>();
                mainLight.type = LightType.Directional;
                mainLight.intensity = 1.0f;
                mainLight.color = Color.white;
                mainLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
                _defaultLight = mainLight;
            }

            // 确保阴影开启
            if (mainLight.shadows == LightShadows.None)
            {
                mainLight.shadows = LightShadows.Soft;
                mainLight.shadowStrength = 1.0f;
            }
        }

        /// <summary>
        /// 查找场景中的主光源。
        /// </summary>
        private static Light FindMainLight()
        {
            // 优先查找 sunLight
            if (_defaultLight != null && _defaultLight.gameObject != null)
                return _defaultLight;

            Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional && light.isActiveAndEnabled)
                    return light;
            }

            return null;
        }

        /// <summary>
        /// 清除缓存的材质和光源实例。
        /// </summary>
        public static void Reset()
        {
            if (_defaultMaterial != null)
            {
                Object.DestroyImmediate(_defaultMaterial);
                _defaultMaterial = null;
            }

            if (_defaultLight != null && _defaultLight.gameObject != null)
            {
                Object.DestroyImmediate(_defaultLight.gameObject);
                _defaultLight = null;
            }
        }
    }
}
