using System;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 端口方向
    /// </summary>
    public enum PCGPortDirection
    {
        Input,
        Output
    }

    /// <summary>
    /// 端口数据类型（用于类型匹配和端口着色）
    /// </summary>
    public enum PCGPortType
    {
        Geometry,
        Float,
        Int,
        Vector3,
        String,
        Bool,
        Color,
        Any
    }

    /// <summary>
    /// 节点参数/端口的 Schema 定义。
    /// 同时用于 GraphView 端口生成和 AI Agent Skill JSON Schema 导出。
    /// </summary>
    [Serializable]
    public class PCGParamSchema
    {
        /// <summary>参数名称</summary>
        public string Name;

        /// <summary>参数显示名称（用于 UI）</summary>
        public string DisplayName;

        /// <summary>参数描述</summary>
        public string Description;

        /// <summary>端口方向（输入/输出）</summary>
        public PCGPortDirection Direction;

        /// <summary>端口数据类型</summary>
        public PCGPortType PortType;

        /// <summary>默认值</summary>
        public object DefaultValue;

        /// <summary>是否必须连接（无默认值时为 true）</summary>
        public bool Required;

        /// <summary>是否允许多条连线（如 Merge 的多输入）</summary>
        public bool AllowMultiple;

        /// <summary>float/int 类型的最小值（用于 UI 滑条）</summary>
        public float Min = float.MinValue;

        /// <summary>float/int 类型的最大值（用于 UI 滑条）</summary>
        public float Max = float.MaxValue;
        
        // 迭代四：Enum/Dropdown 支持
        /// <summary>枚举选项（非 null 时渲染为下拉框）</summary>
        public string[] EnumOptions;

        public PCGParamSchema(string name, PCGPortDirection direction, PCGPortType portType,
            string displayName = null, string description = null, object defaultValue = null,
            bool required = false, bool allowMultiple = false)
        {
            Name = name;
            Direction = direction;
            PortType = portType;
            DisplayName = displayName ?? name;
            Description = description ?? "";
            DefaultValue = defaultValue;
            Required = required;
            AllowMultiple = allowMultiple;
        }
    }
}
