using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 属性复制（对标 Houdini AttribCopy SOP）
    /// </summary>
    public class AttributeCopyNode : PCGNodeBase
    {
        public override string Name => "AttributeCopy";
        public override string DisplayName => "Attribute Copy";
        public override string Description => "将属性从一个几何体复制到另一个几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("dest", PCGPortDirection.Input, PCGPortType.Geometry,
                "Destination", "目标几何体（属性将被写入）", null, required: true),
            new PCGParamSchema("src", PCGPortDirection.Input, PCGPortType.Geometry,
                "Source", "源几何体（属性将被读取）", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "要复制的属性名称", ""),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级", "point")
            {
                EnumOptions = new[] { "point", "vertex", "primitive", "detail" }
            },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "目标几何体（包含复制的属性）"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var dest = GetInputGeometry(inputGeometries, "dest").Clone();
            var src = GetInputGeometry(inputGeometries, "src");

            string name = GetParamString(parameters, "name", "");
            string attrClass = GetParamString(parameters, "class", "point");

            if (string.IsNullOrEmpty(name))
            {
                ctx.LogWarning("AttributeCopy: 属性名称为空");
                return SingleOutput("geometry", dest);
            }

            var srcStore = GetStore(src, attrClass);
            var destStore = GetStore(dest, attrClass);

            var attr = srcStore.GetAttribute(name);
            if (attr == null)
            {
                ctx.LogWarning($"AttributeCopy: 源几何体上没有属性 '{name}'");
                return SingleOutput("geometry", dest);
            }

            int destCount = GetElementCount(dest, attrClass);
            int srcCount = GetElementCount(src, attrClass);

            // 创建新属性
            var newAttr = destStore.CreateAttribute(name, attr.Type);
            newAttr.DefaultValue = attr.DefaultValue;

            // 复制值（索引映射）
            for (int i = 0; i < destCount; i++)
            {
                int srcIdx = srcCount > 0 ? i % srcCount : 0;
                if (srcIdx < attr.Values.Count)
                    newAttr.Values.Add(attr.Values[srcIdx]);
                else
                    newAttr.Values.Add(attr.DefaultValue);
            }

            return SingleOutput("geometry", dest);
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
                "vertex" => geo.Points.Count,
                "primitive" => geo.Primitives.Count,
                "detail" => 1,
                _ => geo.Points.Count,
            };
        }
    }
}