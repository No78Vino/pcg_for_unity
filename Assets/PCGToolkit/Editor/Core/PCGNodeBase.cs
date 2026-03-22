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

        // ---- 参数验证框架 (D1) ----

        /// <summary>
        /// 根据 Inputs 中定义的 Schema（Required、EnumOptions、Min/Max）自动验证参数。
        /// 验证失败时通过 ctx.LogWarning 输出警告，不会中断执行。
        /// 返回 false 表示有必填 Geometry 输入缺失（可用于提前返回）。
        /// </summary>
        protected bool ValidateInputs(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            bool valid = true;
            foreach (var schema in Inputs)
            {
                if (schema.Direction != PCGPortDirection.Input) continue;

                // 必填 Geometry 端口
                if (schema.Required && schema.PortType == PCGPortType.Geometry)
                {
                    if (!inputGeometries.TryGetValue(schema.Name, out var geo) || geo == null)
                    {
                        ctx.LogWarning($"{DisplayName}: 必填输入 '{schema.Name}' 未连接");
                        valid = false;
                    }
                }

                if (!parameters.TryGetValue(schema.Name, out var val)) continue;

                // EnumOptions 验证
                if (schema.EnumOptions != null && schema.EnumOptions.Length > 0 && val is string strVal)
                {
                    bool found = false;
                    foreach (var opt in schema.EnumOptions)
                        if (opt == strVal) { found = true; break; }
                    if (!found)
                        ctx.LogWarning($"{DisplayName}: 参数 '{schema.Name}' 值 '{strVal}' 不在枚举选项中");
                }

                // Float/Int 范围验证
                if (schema.PortType == PCGPortType.Float || schema.PortType == PCGPortType.Int)
                {
                    float fv = 0f;
                    if (val is float f) fv = f;
                    else if (val is int i) fv = i;
                    else if (val is double d) fv = (float)d;
                    else continue;

                    if (schema.Min != float.MinValue && fv < schema.Min)
                        ctx.LogWarning($"{DisplayName}: 参数 '{schema.Name}' 值 {fv} 小于最小值 {schema.Min}");
                    if (schema.Max != float.MaxValue && fv > schema.Max)
                        ctx.LogWarning($"{DisplayName}: 参数 '{schema.Name}' 值 {fv} 大于最大值 {schema.Max}");
                }
            }
            return valid;
        }

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
        /// 从参数字典中获取 GameObject（通过 PCGSceneObjectRef 或直接 GameObject）
        /// 需要配合 PCGContext.SceneReferences 使用
        /// </summary>
        protected GameObject GetParamGameObject(PCGContext ctx, Dictionary<string, object> parameters, string name)
        {
            // 优先从 context.SceneReferences 获取（A7 执行器桥已注入）
            if (ctx.SceneReferences.TryGetValue(name, out var unityObj) && unityObj is GameObject ctxGo)
                return ctxGo;
            // 直接参数值
            if (parameters != null && parameters.TryGetValue(name, out var val))
            {
                if (val is GameObject go) return go;
                if (val is PCGSceneObjectRef sceneRef) return sceneRef.Resolve();
            }
            return null;
        }
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
