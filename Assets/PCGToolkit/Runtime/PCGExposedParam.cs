using UnityEngine;

namespace PCGToolkit.Graph
{
    [System.Serializable]
    public class PCGExposedParam
    {
        public string NodeId;
        public string ParamName;
        public string DisplayName;
        public string ValueType; // float / int / bool / string / Vector3 / Color

        public float   FloatValue;
        public int     IntValue;
        public bool    BoolValue;
        public string  StringValue;
        public Vector3 Vector3Value;
        public Color   ColorValue = Color.white;

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
            var ic = System.Globalization.CultureInfo.InvariantCulture;
            switch (type)
            {
                case "float":
                    float.TryParse(s, System.Globalization.NumberStyles.Float, ic, out FloatValue);
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
                        float.TryParse(parts[0], System.Globalization.NumberStyles.Float, ic, out Vector3Value.x);
                        float.TryParse(parts[1], System.Globalization.NumberStyles.Float, ic, out Vector3Value.y);
                        float.TryParse(parts[2], System.Globalization.NumberStyles.Float, ic, out Vector3Value.z);
                    }
                    break;
                default:
                    StringValue = s; break;
            }
        }
    }
}
