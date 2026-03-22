using System.Collections.Generic;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Nodes
{
    /// <summary>
    /// 读取场景 GameObject 的子对象位置作为点云，输出 PCGGeometry（仅含点，无面）。
    /// 可用于 CopyToPoints、Scatter 等需要种子点的节点。
    /// </summary>
    public class ScenePointsInputNode : PCGNodeBase
    {
        public override string Name        => "ScenePointsInput";
        public override string DisplayName => "Scene Points Input";
        public override string Description => "将场景 GameObject 的子对象位置转为点云 PCGGeometry";
        public override PCGNodeCategory Category => PCGNodeCategory.Create;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("target", PCGPortDirection.Input, PCGPortType.SceneObject,
                displayName: "Target", description: "父 GameObject（子对象用作点云）", required: false)
            {
                ObjectType = typeof(GameObject),
                AllowSceneObjects = true,
            },
            new PCGParamSchema("includeRoot", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Include Root", description: "是否包含根对象自身", defaultValue: false),
            new PCGParamSchema("readNames", PCGPortDirection.Input, PCGPortType.Bool,
                displayName: "Read Names", description: "将子对象名写入 @name 属性", defaultValue: true),
            new PCGParamSchema("space", PCGPortDirection.Input, PCGPortType.String,
                displayName: "Space", description: "World / Local",
                defaultValue: "World")
            { EnumOptions = new[] { "World", "Local" } },
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                displayName: "Points"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var go = GetParamGameObject(ctx, parameters, "target");
            if (go == null)
            {
                ctx.LogWarning($"{DisplayName}: 未指定目标 GameObject");
                return SingleOutput("geometry", new PCGGeometry());
            }

            bool includeRoot = GetParamBool(parameters, "includeRoot", false);
            bool readNames   = GetParamBool(parameters, "readNames", true);
            string space     = GetParamString(parameters, "space", "World");
            bool worldSpace  = space != "Local";

            var geo = new PCGGeometry();
            PCGAttribute nameAttr  = null;
            PCGAttribute rotAttr   = null;
            PCGAttribute scaleAttr = null;

            if (readNames)
                nameAttr = geo.PointAttribs.CreateAttribute("name", AttribType.String);
            rotAttr   = geo.PointAttribs.CreateAttribute("orient", AttribType.Vector3);
            scaleAttr = geo.PointAttribs.CreateAttribute("pscale", AttribType.Float);

            void AddTransform(Transform t)
            {
                Vector3 pos = worldSpace ? t.position : t.localPosition;
                geo.Points.Add(pos);
                nameAttr?.Values.Add(t.gameObject.name);
                rotAttr.Values.Add(worldSpace ? t.rotation.eulerAngles : t.localRotation.eulerAngles);
                scaleAttr.Values.Add(worldSpace ? t.lossyScale.magnitude / Mathf.Sqrt(3f) : t.localScale.magnitude / Mathf.Sqrt(3f));
            }

            if (includeRoot)
                AddTransform(go.transform);

            foreach (Transform child in go.transform)
                AddTransform(child);

            if (geo.Points.Count == 0)
                ctx.LogWarning($"{DisplayName}: GameObject '{go.name}' 没有子对象");

            return SingleOutput("geometry", geo);
        }
    }
}
