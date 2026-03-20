using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCG 节点基类，提供 IPCGNode 的默认实现和辅助方法。
    /// 所有具体节点继承此类。
    /// </summary>
    public abstract class PCGNodeBase : IPCGNode
    {
        public abstract string Name { get; }
        public abstract string DisplayName { get; }
        public abstract string Description { get; }
        public abstract PCGNodeCategory Category { get; }
        public abstract PCGParamSchema[] Inputs { get; }
        public abstract PCGParamSchema[] Outputs { get; }

        public abstract Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters);

        // ---- 辅助方法 ----

        /// <summary>
        /// 从输入字典中获取几何体，不存在时返回空几何体
        /// </summary>
        protected PCGGeometry GetInputGeometry(Dictionary<string, PCGGeometry> inputs, string portName)
        {
            if (inputs != null && inputs.TryGetValue(portName, out var geo))
                return geo;
            return new PCGGeometry();
        }

        /// <summary>
        /// 从参数字典中获取 float 值
        /// </summary>
        protected float GetParamFloat(Dictionary<string, object> parameters, string name, float defaultValue = 0f)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is float f) return f;
                if (val is double d) return (float)d;
                if (val is int i) return i;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从参数字典中获取 int 值
        /// </summary>
        protected int GetParamInt(Dictionary<string, object> parameters, string name, int defaultValue = 0)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is int i) return i;
                if (val is float f) return (int)f;
                if (val is double d) return (int)d;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从参数字典中获取 bool 值
        /// </summary>
        protected bool GetParamBool(Dictionary<string, object> parameters, string name, bool defaultValue = false)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is bool b) return b;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从参数字典中获取 string 值
        /// </summary>
        protected string GetParamString(Dictionary<string, object> parameters, string name, string defaultValue = "")
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is string s) return s;
            }
            return defaultValue;
        }

        /// <summary>
        /// 从参数字典中获取 Vector3 值
        /// </summary>
        protected Vector3 GetParamVector3(Dictionary<string, object> parameters, string name, Vector3? defaultValue = null)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is Vector3 v) return v;
            }
            return defaultValue ?? Vector3.zero;
        }

        /// <summary>
        /// 从参数字典中获取 Color 值
        /// </summary>
        protected Color GetParamColor(Dictionary<string, object> parameters, string name, Color defaultValue)
        {
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is Color c) return c;
                if (val is Vector4 v4) return new Color(v4.x, v4.y, v4.z, v4.w);
                if (val is string s && ColorUtility.TryParseHtmlString(s, out var parsed))
                    return parsed;
            }
            return defaultValue;
        }

        /// <summary>
        /// 构建单输出结果字典
        /// </summary>
        protected Dictionary<string, PCGGeometry> SingleOutput(string portName, PCGGeometry geometry)
        {
            return new Dictionary<string, PCGGeometry> { { portName, geometry } };
        }

        /// <summary>
        /// 获取动态输出端口（根据参数动态生成）
        /// 用于 SubGraphInputNode 等需要动态端口的节点
        /// </summary>
        public virtual PCGParamSchema[] GetDynamicOutputs(Dictionary<string, object> parameters)
        {
            return Outputs;
        }

        /// <summary>
        /// 获取动态输入端口（根据参数动态生成）
        /// </summary>
        public virtual PCGParamSchema[] GetDynamicInputs(Dictionary<string, object> parameters)
        {
            return Inputs;
        }
    }
}
