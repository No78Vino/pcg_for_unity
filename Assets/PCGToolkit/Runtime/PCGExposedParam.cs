using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Runtime
{
    /// <summary>
    /// 运行时可暴露的参数槽（对应 PCGParamSchema.Exposed=true 的参数）。
    /// 存储于 PCGGraphRunner.ExposedParams，可在 Inspector 中覆盖 Graph 默认值。
    /// </summary>
    [System.Serializable]
    public class PCGExposedParam
    {
        public string NodeId;
        public string ParamName;
        public string DisplayName;
        public string ValueType; // float / int / bool / string / Vector3 / Color

        // 各类型值字段（Inspector 中只显示对应类型的字段）
        public float  FloatValue;
        public int    IntValue;
        public bool   BoolValue;
        public string StringValue;
        public Vector3 Vector3Value;
        public Color  ColorValue = Color.white;

        /// <summary>获取当前值（box）</summary>
        public object GetValue()
        {
            switch (ValueType)
            {
                case "float":   return FloatValue;
                case "int":     return IntValue;
                case "bool":    return BoolValue;
                case "string":  return StringValue;
                case "Vector3": return Vector3Value;
                case "Color":   return ColorValue;
                default:        return StringValue;
            }
        }

        public void SetDefaultFromString(string s, string type)
        {
            ValueType = type;
            switch (type)
            {
                case "float":
                    float.TryParse(s, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out FloatValue);
                    break;
                case "int":
                    int.TryParse(s, out IntValue); break;
                case "bool":
                    bool.TryParse(s, out BoolValue); break;
                case "string":
                    StringValue = s; break;
                case "Vector3":
                    var parts = s.Split(',');
                    if (parts.Length == 3)
                    {
                        float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out Vector3Value.x);
                        float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out Vector3Value.y);
                        float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out Vector3Value.z);
                    }
                    break;
                default:
                    StringValue = s; break;
            }
        }
    }
}
