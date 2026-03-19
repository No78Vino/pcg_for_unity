using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;
using UnityEditor;

namespace PCGToolkit.Nodes.Create
{
    /// <summary>
    /// 导入 Unity Mesh 资产为 PCGGeometry（对标 Houdini File SOP 的导入功能）
    /// </summary>
    public class ImportMeshNode : PCGNodeBase
    {
        public override string Name => "ImportMesh";
        public override string DisplayName => "Import Mesh";
        public override string Description => "从 Unity Mesh 资产导入几何体";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("assetPath", PCGPortDirection.Input, PCGPortType.String,
                "Asset Path", "Mesh 资产路径（Assets/ 开头）", ""),
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
            string assetPath = GetParamString(parameters, "assetPath", "");

            if (string.IsNullOrEmpty(assetPath))
            {
                ctx.LogWarning("ImportMesh: assetPath 为空");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 尝试加载 Mesh
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (mesh == null)
            {
                ctx.LogWarning($"ImportMesh: 无法加载 Mesh 资产: {assetPath}");
                return SingleOutput("geometry", new PCGGeometry());
            }

            // 转换为 PCGGeometry
            var geo = PCGGeometryToMesh.FromMesh(mesh);
            ctx.Log($"ImportMesh: 已导入 {geo.Points.Count} 个顶点, {geo.Primitives.Count} 个面");

            return SingleOutput("geometry", geo);
        }
    }
}