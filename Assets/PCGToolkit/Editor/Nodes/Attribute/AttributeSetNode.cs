using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 设置/修改属性值（对标 Houdini AttribWrangle / AttribSet）
    /// </summary>
    public class AttributeSetNode : PCGNodeBase
    {
        public override string Name => "AttributeSet";
        public override string DisplayName => "Attribute Set";
        public override string Description => "设置或修改几何体上的属性值";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "属性名称", "Cd"),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级（point/vertex/primitive/detail）", "point"),
            new PCGParamSchema("expression", PCGPortDirection.Input, PCGPortType.String,
                "Expression", "值表达式（如 @P.y, rand(@ptnum) 等）", ""),
            new PCGParamSchema("group", PCGPortDirection.Input, PCGPortType.String,
                "Group", "仅对指定分组的元素进行设置", ""),
            new PCGParamSchema("valueFloat", PCGPortDirection.Input, PCGPortType.Float,
                "Value (Float)", "常量值（Float 类型，expression 为空时使用）", 0f),
            new PCGParamSchema("valueVector3", PCGPortDirection.Input, PCGPortType.Vector3,
                "Value (Vector3)", "常量值（Vector3 类型）", Vector3.zero),
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
            string expression = GetParamString(parameters, "expression", "");
            string group = GetParamString(parameters, "group", "");
            float valueFloat = GetParamFloat(parameters, "valueFloat", 0f);
            Vector3 valueVector3 = GetParamVector3(parameters, "valueVector3", Vector3.zero);

            // 获取目标属性存储
            AttributeStore store = attrClass.ToLower() switch
            {
                "point" => geo.PointAttribs,
                "vertex" => geo.VertexAttribs,
                "primitive" => geo.PrimAttribs,
                "detail" => geo.DetailAttribs,
                _ => geo.PointAttribs
            };

            var attr = store.GetAttribute(attrName);
            if (attr == null)
            {
                ctx.LogWarning($"AttributeSet: 属性 {attrName} 不存在");
                return SingleOutput("geometry", geo);
            }

            // 确定要修改的索引集合
            HashSet<int> indices = null;
            if (!string.IsNullOrEmpty(group))
            {
                if (attrClass == "point" && geo.PointGroups.TryGetValue(group, out var pointGroup))
                    indices = pointGroup;
                else if (attrClass == "primitive" && geo.PrimGroups.TryGetValue(group, out var primGroup))
                    indices = primGroup;
            }

            int elementCount = attrClass.ToLower() switch
            {
                "point" => geo.Points.Count,
                "primitive" => geo.Primitives.Count,
                "detail" => 1,
                _ => attr.Values.Count
            };

            // 设置属性值
            for (int i = 0; i < elementCount && i < attr.Values.Count; i++)
            {
                if (indices != null && !indices.Contains(i))
                    continue;

                if (!string.IsNullOrEmpty(expression))
                {
                    // 简单表达式求值
                    attr.Values[i] = EvaluateExpression(geo, expression, i, attrClass, attr.Type);
                }
                else
                {
                    // 使用常量值
                    if (attr.Type == AttribType.Float || attr.Type == AttribType.Int)
                        attr.Values[i] = valueFloat;
                    else if (attr.Type == AttribType.Vector3 || attr.Type == AttribType.Vector4 || attr.Type == AttribType.Color)
                        attr.Values[i] = valueVector3;
                }
            }

            return SingleOutput("geometry", geo);
        }

        private object EvaluateExpression(PCGGeometry geo, string expression, int index, string attrClass, AttribType type)
        {
            // 简单表达式：@P.y, @ptnum, rand(@ptnum)
            expression = expression.Trim();

            if (expression == "@ptnum")
                return (float)index;

            if (expression.StartsWith("@P."))
            {
                char axis = expression[3];
                if (attrClass == "point")
                {
                    Vector3 p = geo.Points[index];
                    return axis == 'x' ? p.x : axis == 'y' ? p.y : p.z;
                }
            }

            if (expression.StartsWith("rand("))
            {
                // 简单随机
                float seed = index * 0.618033988749895f;
                return (seed - Mathf.Floor(seed));
            }

            // 默认返回 0
            return type == AttribType.Vector3 ? Vector3.zero : 0f;
        }
    }
}