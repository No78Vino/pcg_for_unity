using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 属性提升/降级（对标 Houdini AttribPromote SOP）
    /// </summary>
    public class AttributePromoteNode : PCGNodeBase
    {
        public override string Name => "AttributePromote";
        public override string DisplayName => "Attribute Promote";
        public override string Description => "在属性层级之间提升或降级属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "要提升/降级的属性名称", ""),
            new PCGParamSchema("fromClass", PCGPortDirection.Input, PCGPortType.String,
                "From Class", "源属性层级", "point")
            {
                EnumOptions = new[] { "point", "vertex", "primitive", "detail" }
            },
            new PCGParamSchema("toClass", PCGPortDirection.Input, PCGPortType.String,
                "To Class", "目标属性层级", "detail")
            {
                EnumOptions = new[] { "point", "vertex", "primitive", "detail" }
            },
            new PCGParamSchema("method", PCGPortDirection.Input, PCGPortType.String,
                "Method", "聚合方法（用于降维）", "min")
            {
                EnumOptions = new[] { "min", "max", "average", "sum", "first", "last" }
            },
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
            string name = GetParamString(parameters, "name", "");
            string fromClass = GetParamString(parameters, "fromClass", "point");
            string toClass = GetParamString(parameters, "toClass", "detail");
            string method = GetParamString(parameters, "method", "min");

            if (string.IsNullOrEmpty(name))
            {
                ctx.LogWarning("AttributePromote: 属性名称为空");
                return SingleOutput("geometry", geo);
            }

            var fromStore = GetStore(geo, fromClass);
            var toStore = GetStore(geo, toClass);

            var attr = fromStore.GetAttribute(name);
            if (attr == null)
            {
                ctx.LogWarning($"AttributePromote: 属性 '{name}' 不存在于 {fromClass} 层级");
                return SingleOutput("geometry", geo);
            }

            int fromCount = GetElementCount(geo, fromClass);
            int toCount = GetElementCount(geo, toClass);

            if (fromCount == 0 || toCount == 0)
            {
                ctx.LogWarning("AttributePromote: 几何体元素数量为 0");
                return SingleOutput("geometry", geo);
            }

            // 简化实现：只支持 Detail <- 其他 的提升
            if (toClass == "detail")
            {
                // 聚合所有值为单一值
                object aggregated = AggregateValues(attr.Values, method);
                toStore.SetAttribute(name, aggregated);
            }
            else if (fromClass == "detail" && toClass == "point")
            {
                // Detail -> Point: 将单一值复制到所有点
                object value = attr.Values.Count > 0 ? attr.Values[0] : attr.DefaultValue;
                var newAttr = toStore.CreateAttribute(name, attr.Type);
                for (int i = 0; i < toCount; i++)
                    newAttr.Values.Add(value);
            }
            else
            {
                // 其他情况：简单复制（实际实现需要更复杂的映射）
                var newAttr = toStore.CreateAttribute(name, attr.Type);
                for (int i = 0; i < toCount; i++)
                {
                    int srcIdx = Mathf.Min(i, attr.Values.Count - 1);
                    newAttr.Values.Add(attr.Values[srcIdx]);
                }
            }

            return SingleOutput("geometry", geo);
        }

        private AttributeStore GetStore(PCGGeometry geo, string attrClass)
        {
            return attrClass.ToLower() switch
            {
                "vertex" => geo.VertexAttribs,
                "primitive" => geo.PrimAttribs,
                "detail" => geo.DetailAttribs,
                _ => geo.PointAttribs,
            };
        }

        private int GetElementCount(PCGGeometry geo, string attrClass)
        {
            return attrClass.ToLower() switch
            {
                "vertex" => geo.Points.Count, // 简化：顶点数 = 点数
                "primitive" => geo.Primitives.Count,
                "detail" => 1,
                _ => geo.Points.Count,
            };
        }

        private object AggregateValues(List<object> values, string method)
        {
            if (values == null || values.Count == 0) return null;

            // 尝试数值聚合
            if (values[0] is float || values[0] is int)
            {
                var floats = new List<float>();
                foreach (var v in values)
                {
                    if (v is float f) floats.Add(f);
                    else if (v is int i) floats.Add(i);
                }

                if (floats.Count == 0) return values[0];

                return method.ToLower() switch
                {
                    "min" => Mathf.Min(floats.ToArray()),
                    "max" => Mathf.Max(floats.ToArray()),
                    "average" => floats.Count > 0 ? floats[0] / floats.Count * floats.Count : 0f,
                    "sum" => floats.Count > 0 ? floats[0] * floats.Count : 0f,
                    "first" => floats[0],
                    "last" => floats[floats.Count - 1],
                    _ => floats[0],
                };
            }

            return method == "last" ? values[values.Count - 1] : values[0];
        }
    }
}