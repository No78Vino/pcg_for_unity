using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 创建新属性（对标 Houdini AttribCreate SOP）
    /// </summary>
    public class AttributeCreateNode : PCGNodeBase
    {
        public override string Name => "AttributeCreate";
        public override string DisplayName => "Attribute Create";
        public override string Description => "在几何体上创建新属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "属性名称", "Cd"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级（point/vertex/primitive/detail）", "point"),
            new PCGParamSchema("type", PCGPortDirection.Input, PCGPortType.String,
                "Type", "数据类型（float/int/vector3/vector4/color/string）", "float"),
            new PCGParamSchema("defaultFloat", PCGPortDirection.Input, PCGPortType.Float,
                "Default (Float)", "默认值（Float 类型）", 0f),
            new PCGParamSchema("defaultVector3", PCGPortDirection.Input, PCGPortType.Vector3,
                "Default (Vector3)", "默认值（Vector3 类型）", Vector3.zero),
            new PCGParamSchema("defaultString", PCGPortDirection.Input, PCGPortType.String,
                "Default (String)", "默认值（String 类型）", ""),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "输出几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();
            string attrName = GetParamString(parameters, "name", "Cd");
            string attrClass = GetParamString(parameters, "class", "point");
            string attrType = GetParamString(parameters, "type", "float");
            float defaultFloat = GetParamFloat(parameters, "defaultFloat", 0f);
            Vector3 defaultVector3 = GetParamVector3(parameters, "defaultVector3", Vector3.zero);
            string defaultString = GetParamString(parameters, "defaultString", "");

            // 确定属性类型
            AttribType type = attrType.ToLower() switch
            {
                "float" => AttribType.Float,
                "int" => AttribType.Int,
                "vector3" => AttribType.Vector3,
                "vector4" => AttribType.Vector4,
                "color" => AttribType.Color,
                "string" => AttribType.String,
                _ => AttribType.Float
            };

            // 确定默认值
            object defaultValue = type switch
            {
                AttribType.Float => defaultFloat,
                AttribType.Int => (int)defaultFloat,
                AttribType.Vector3 => defaultVector3,
                AttribType.Vector4 => new Vector4(defaultVector3.x, defaultVector3.y, defaultVector3.z, 1f),
                AttribType.Color => Color.white,
                AttribType.String => defaultString,
                _ => defaultFloat
            };

            // 获取目标属性存储
            AttributeStore store = attrClass.ToLower() switch
            {
                "point" => geo.PointAttribs,
                "vertex" => geo.VertexAttribs,
                "primitive" => geo.PrimAttribs,
                "detail" => geo.DetailAttribs,
                _ => geo.PointAttribs
            };

            // 创建属性
            var attr = store.CreateAttribute(attrName, type, defaultValue);

            // 确定元素数量并填充默认值
            int elementCount = attrClass.ToLower() switch
            {
                "point" => geo.Points.Count,
                "vertex" => geo.Points.Count * 3, // 简化假设
                "primitive" => geo.Primitives.Count,
                "detail" => 1,
                _ => geo.Points.Count
            };

            for (int i = 0; i < elementCount; i++)
            {
                attr.Values.Add(defaultValue);
            }

            return SingleOutput("geometry", geo);
        }
    }
}