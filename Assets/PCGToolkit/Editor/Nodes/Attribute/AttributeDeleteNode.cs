using System.Collections.Generic;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes.Attribute
{
    /// <summary>
    /// 删除指定属性（对标 Houdini AttribDelete SOP）
    /// </summary>
    public class AttributeDeleteNode : PCGNodeBase
    {
        public override string Name => "AttributeDelete";
        public override string DisplayName => "Attribute Delete";
        public override string Description => "删除几何体上的指定属性";
        public override PCGNodeCategory Category => PCGNodeCategory.Attribute;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("name", PCGPortDirection.Input, PCGPortType.String,
                "Name", "要删除的属性名称（多个用逗号分隔）", ""),
            new PCGParamSchema("class", PCGPortDirection.Input, PCGPortType.String,
                "Class", "属性层级", "point")
            {
                EnumOptions = new[] { "point", "vertex", "primitive", "detail" }
            },
            new PCGParamSchema("deleteAll", PCGPortDirection.Input, PCGPortType.Bool,
                "Delete All", "删除该层级的所有属性", false),
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
            string names = GetParamString(parameters, "name", "");
            string attrClass = GetParamString(parameters, "class", "point");
            bool deleteAll = GetParamBool(parameters, "deleteAll", false);

            var store = GetStore(geo, attrClass);

            if (deleteAll)
            {
                store.Clear();
            }
            else if (!string.IsNullOrEmpty(names))
            {
                foreach (var name in names.Split(','))
                {
                    var trimmed = name.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        store.RemoveAttribute(trimmed);
                    }
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
    }
}