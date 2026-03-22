using System.Globalization;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 轻量场景对象引用，可序列化为字符串存入 YAML 资产。
    /// 执行时通过 instanceID 重新获取 GameObject（仅 Editor 运行期有效）。
    /// </summary>
    [System.Serializable]
    public class PCGSceneObjectRef
    {
        public int InstanceID;
        public string HierarchyPath; // 备用路径（instanceID 失效时回退）

        public PCGSceneObjectRef() { }
        public PCGSceneObjectRef(GameObject go)
        {
            if (go == null) return;
            InstanceID = go.GetInstanceID();
            HierarchyPath = GetHierarchyPath(go);
        }

        public GameObject Resolve()
        {
            // 先尝试 instanceID（Editor 内最可靠）
            var obj = UnityEditor.EditorUtility.InstanceIDToObject(InstanceID) as GameObject;
            if (obj != null) return obj;
            // 回退：按路径查找
            if (!string.IsNullOrEmpty(HierarchyPath))
                return GameObject.Find(HierarchyPath);
            return null;
        }

        private static string GetHierarchyPath(GameObject go)
        {
            var path = go.name;
            var parent = go.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        // 序列化为 "SceneRef:instanceID:path"
        public override string ToString() =>
            $"SceneRef:{InstanceID}:{HierarchyPath ?? ""}";

        public static PCGSceneObjectRef FromString(string s)
        {
            if (string.IsNullOrEmpty(s) || !s.StartsWith("SceneRef:"))
                return null;
            var parts = s.Substring("SceneRef:".Length).Split(new char[]{':'}, 2);
            if (parts.Length < 1) return null;
            var r = new PCGSceneObjectRef();
            int.TryParse(parts[0], out r.InstanceID);
            r.HierarchyPath = parts.Length > 1 ? parts[1] : "";
            return r;
        }
    }

    public static class PCGParamHelper
    {
        public static object DeserializeParamValue(PCGSerializedParameter param)
        {
            try
            {
                switch (param.ValueType)
                {
                    case "float":
                        return float.Parse(param.ValueJson, CultureInfo.InvariantCulture);
                    case "int":
                        return int.Parse(param.ValueJson);
                    case "bool":
                        return bool.Parse(param.ValueJson);
                    case "string":
                        return param.ValueJson;
                    case "Vector3":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 3)
                        {
                            return new Vector3(
                                float.Parse(parts[0], CultureInfo.InvariantCulture),
                                float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture));
                        }
                        return Vector3.zero;
                    }
                    case "Color":
                    {
                        var parts = param.ValueJson.Split(',');
                        if (parts.Length == 4)
                        {
                            return new Color(
                                float.Parse(parts[0], CultureInfo.InvariantCulture),
                                float.Parse(parts[1], CultureInfo.InvariantCulture),
                                float.Parse(parts[2], CultureInfo.InvariantCulture),
                                float.Parse(parts[3], CultureInfo.InvariantCulture));
                        }
                        return Color.white;
                    }
                    case "SceneObject":
                        return PCGSceneObjectRef.FromString(param.ValueJson);
                    case "null":
                    case null:
                    case "":
                        return param.ValueJson;
                    default:
                        // 如果 ValueJson 是 SceneRef 格式也尝试解析
                        if (param.ValueJson != null && param.ValueJson.StartsWith("SceneRef:"))
                            return PCGSceneObjectRef.FromString(param.ValueJson);
                        return param.ValueJson;
                }
            }
            catch
            {
                return param.ValueJson;
            }
        }

        public static string SerializeParamValue(object value)
        {
            if (value == null) return "";

            switch (value)
            {
                case float f:
                    return f.ToString(CultureInfo.InvariantCulture);
                case int i:
                    return i.ToString();
                case bool b:
                    return b.ToString().ToLower();
                case string s:
                    return s;
                case Vector3 v:
                    return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
                case Color c:
                    return $"{c.r.ToString(CultureInfo.InvariantCulture)},{c.g.ToString(CultureInfo.InvariantCulture)},{c.b.ToString(CultureInfo.InvariantCulture)},{c.a.ToString(CultureInfo.InvariantCulture)}";
                case PCGSceneObjectRef sceneRef:
                    return sceneRef.ToString();
                default:
                    return value.ToString();
            }
        }
    }
}