using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PCGToolkit.Graph
{
    [Serializable]
    internal class PCGPresetData
    {
        public string NodeType;
        public string PresetName;
        public List<PCGPresetParam> Params = new List<PCGPresetParam>();
    }

    [Serializable]
    internal class PCGPresetParam
    {
        public string Key;
        public string TypeName;
        public string ValueJson;
    }

    /// <summary>
    /// 节点参数预设管理器（A3）。
    /// 将节点的参数配置保存为 JSON 文件，支持加载和列举。
    /// 预设存储在 Assets/PCGToolkit/Presets/ 目录下。
    /// </summary>
    public static class PCGPresetManager
    {
        private const string PRESET_DIR = "Assets/PCGToolkit/Presets";

        public static void SavePreset(string nodeType, string presetName, Dictionary<string, object> parameters)
        {
            EnsurePresetDir();

            var data = new PCGPresetData
            {
                NodeType = nodeType,
                PresetName = presetName,
            };

            foreach (var kvp in parameters)
            {
                data.Params.Add(new PCGPresetParam
                {
                    Key = kvp.Key,
                    TypeName = kvp.Value?.GetType().FullName ?? "null",
                    ValueJson = SerializeValue(kvp.Value),
                });
            }

            string json = JsonUtility.ToJson(data, true);
            string safeName = presetName.Replace(" ", "_").Replace("/", "_");
            string path = $"{PRESET_DIR}/{nodeType}_{safeName}.json";
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"PCGPresetManager: Preset saved to {path}");
        }

        public static Dictionary<string, object> LoadPreset(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"PCGPresetManager: Preset file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<PCGPresetData>(json);
            if (data == null) return null;

            var result = new Dictionary<string, object>();
            foreach (var param in data.Params)
                result[param.Key] = DeserializeValue(param.TypeName, param.ValueJson);
            return result;
        }

        public static string[] GetPresetsForNode(string nodeType)
        {
            EnsurePresetDir();
            return Directory.GetFiles(PRESET_DIR, $"{nodeType}_*.json");
        }

        public static string[] GetAllPresets()
        {
            EnsurePresetDir();
            return Directory.GetFiles(PRESET_DIR, "*.json");
        }

        public static string GetPresetName(string filePath)
        {
            string filename = Path.GetFileNameWithoutExtension(filePath);
            int idx = filename.IndexOf('_');
            return idx >= 0 ? filename.Substring(idx + 1).Replace("_", " ") : filename;
        }

        private static void EnsurePresetDir()
        {
            if (!Directory.Exists(PRESET_DIR))
            {
                Directory.CreateDirectory(PRESET_DIR);
                AssetDatabase.Refresh();
            }
        }

        private static string SerializeValue(object val)
        {
            if (val == null) return "null";
            if (val is float f) return f.ToString("R");
            if (val is int i) return i.ToString();
            if (val is bool b) return b.ToString();
            if (val is string s) return s;
            if (val is Vector3 v3) return $"{v3.x},{v3.y},{v3.z}";
            if (val is Color c) return $"{c.r},{c.g},{c.b},{c.a}";
            return val.ToString();
        }

        private static object DeserializeValue(string typeName, string valueJson)
        {
            if (typeName == "null" || valueJson == "null") return null;
            if (typeName == "System.Single" && float.TryParse(valueJson,
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float fv)) return fv;
            if (typeName == "System.Int32" && int.TryParse(valueJson, out int iv)) return iv;
            if (typeName == "System.Boolean" && bool.TryParse(valueJson, out bool bv)) return bv;
            if (typeName == "System.String") return valueJson;
            if (typeName == "UnityEngine.Vector3")
            {
                var parts = valueJson.Split(',');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                    return new Vector3(x, y, z);
            }
            if (typeName == "UnityEngine.Color")
            {
                var parts = valueJson.Split(',');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b) &&
                    float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float a))
                    return new Color(r, g, b, a);
            }
            return valueJson;
        }
    }
}


        /// <summary>
        /// 保存节点参数为预设文件。
        /// </summary>
        public static void SavePreset(string nodeType, string presetName, Dictionary<string, object> parameters)
        {
            EnsurePresetDir();

            var data = new PresetData
            {
                NodeType = nodeType,
                PresetName = presetName,
            };

            foreach (var kvp in parameters)
            {
                data.Params.Add(new PresetParam
                {
                    Key = kvp.Key,
                    TypeName = kvp.Value?.GetType().FullName ?? "null",
                    ValueJson = SerializeValue(kvp.Value),
                });
            }

            string json = JsonUtility.ToJson(data, true);
            string safeName = presetName.Replace(" ", "_").Replace("/", "_");
            string path = $"{PRESET_DIR}/{nodeType}_{safeName}.json";
            File.WriteAllText(path, json);
            AssetDatabase.Refresh();
            Debug.Log($"PCGPresetManager: Preset saved to {path}");
        }

        /// <summary>
        /// 加载预设文件，返回参数字典。
        /// </summary>
        public static Dictionary<string, object> LoadPreset(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning($"PCGPresetManager: Preset file not found: {filePath}");
                return null;
            }

            string json = File.ReadAllText(filePath);
            var data = JsonUtility.FromJson<PresetData>(json);
            if (data == null) return null;

            var result = new Dictionary<string, object>();
            foreach (var param in data.Params)
            {
                result[param.Key] = DeserializeValue(param.TypeName, param.ValueJson);
            }
            return result;
        }

        /// <summary>
        /// 列举指定节点类型的所有预设文件路径。
        /// </summary>
        public static string[] GetPresetsForNode(string nodeType)
        {
            EnsurePresetDir();
            var files = Directory.GetFiles(PRESET_DIR, $"{nodeType}_*.json");
            return files;
        }

        /// <summary>
        /// 列举所有预设文件路径。
        /// </summary>
        public static string[] GetAllPresets()
        {
            EnsurePresetDir();
            return Directory.GetFiles(PRESET_DIR, "*.json");
        }

        /// <summary>
        /// 从文件路径提取预设名称。
        /// </summary>
        public static string GetPresetName(string filePath)
        {
            string filename = Path.GetFileNameWithoutExtension(filePath);
            int underscoreIdx = filename.IndexOf('_');
            return underscoreIdx >= 0 ? filename.Substring(underscoreIdx + 1).Replace("_", " ") : filename;
        }

        private static void EnsurePresetDir()
        {
            if (!Directory.Exists(PRESET_DIR))
            {
                Directory.CreateDirectory(PRESET_DIR);
                AssetDatabase.Refresh();
            }
        }

        private static string SerializeValue(object val)
        {
            if (val == null) return "null";
            if (val is float f) return f.ToString("R");
            if (val is int i) return i.ToString();
            if (val is bool b) return b.ToString();
            if (val is string s) return s;
            if (val is Vector3 v3) return $"{v3.x},{v3.y},{v3.z}";
            if (val is Color c) return $"{c.r},{c.g},{c.b},{c.a}";
            return val.ToString();
        }

        private static object DeserializeValue(string typeName, string valueJson)
        {
            if (typeName == "null" || valueJson == "null") return null;
            if (typeName == "System.Single" && float.TryParse(valueJson, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float fv)) return fv;
            if (typeName == "System.Int32" && int.TryParse(valueJson, out int iv)) return iv;
            if (typeName == "System.Boolean" && bool.TryParse(valueJson, out bool bv)) return bv;
            if (typeName == "System.String") return valueJson;
            if (typeName == "UnityEngine.Vector3")
            {
                var parts = valueJson.Split(',');
                if (parts.Length == 3 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float y) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float z))
                    return new Vector3(x, y, z);
            }
            if (typeName == "UnityEngine.Color")
            {
                var parts = valueJson.Split(',');
                if (parts.Length == 4 &&
                    float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float r) &&
                    float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float g) &&
                    float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float b) &&
                    float.TryParse(parts[3], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float a))
                    return new Color(r, g, b, a);
            }
            return valueJson;
        }
    }
}
