using System;

namespace PCGToolkit.Core
{
    public enum PCGPortDirection
    {
        Input,
        Output
    }

    public enum PCGPortType
    {
        Geometry,
        Float,
        Int,
        Vector3,
        String,
        Bool,
        Color,
        Any,
        SceneObject   // 场景 GameObject 引用（Inspector 中渲染为 ObjectField）
    }

    [Serializable]
    public class PCGParamSchema
    {
        public string Name;
        public string DisplayName;
        public string Description;
        public PCGPortDirection Direction;
        public PCGPortType PortType;
        public object DefaultValue;
        public bool Required;
        public bool AllowMultiple;
        public float Min = float.MinValue;
        public float Max = float.MaxValue;
        public string[] EnumOptions;

        // 迭代六：SceneObject 约束
        /// <summary>ObjectField 的对象类型约束（如 typeof(GameObject)）</summary>
        public Type ObjectType;
        /// <summary>是否允许选择场景对象（true）还是仅 Project 资产（false）</summary>
        public bool AllowSceneObjects = true;

        // 迭代六：HDA 参数暴露
        /// <summary>标记为 true 的参数会出现在 PCGGraphRunner Inspector 中</summary>
        public bool Exposed;

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
