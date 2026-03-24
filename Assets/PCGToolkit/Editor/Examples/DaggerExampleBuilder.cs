using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Examples
{
    public static class DaggerExampleBuilder
    {
        private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        [MenuItem("PCG Toolkit/Examples/Create Dagger Graph")]
        public static void CreateDaggerGraph()
        {
            var data = ScriptableObject.CreateInstance<PCGGraphData>();
            data.GraphName = "ProceduralDagger";

            var nodes = new Dictionary<string, PCGNodeData>();

            // ---- Blade (刀身) ----
            nodes["blade_box"] = data.AddNode("Box", new Vector2(0, 0));
            SetParam(nodes["blade_box"], "sizeX", "0.04", "float");
            SetParam(nodes["blade_box"], "sizeY", "0.25", "float");
            SetParam(nodes["blade_box"], "sizeZ", "0.005", "float");

            nodes["blade_subdiv"] = data.AddNode("Subdivide", new Vector2(250, 0));
            SetParam(nodes["blade_subdiv"], "iterations", "3", "int");
            SetParam(nodes["blade_subdiv"], "algorithm", "linear", "string");

            nodes["blade_group"] = data.AddNode("GroupCreate", new Vector2(500, 0));
            SetParam(nodes["blade_group"], "groupName", "blade", "string");
            SetParam(nodes["blade_group"], "groupType", "primitive", "string");
            SetParam(nodes["blade_group"], "filter", "", "string");

            nodes["blade_taper"] = data.AddNode("Taper", new Vector2(750, 0));
            SetParam(nodes["blade_taper"], "scaleStart", "1", "float");
            SetParam(nodes["blade_taper"], "scaleEnd", "0.05", "float");
            SetParam(nodes["blade_taper"], "axis", "y", "string");

            nodes["blade_bend"] = data.AddNode("Bend", new Vector2(1000, 0));
            SetParam(nodes["blade_bend"], "angle", "5", "float");
            SetParam(nodes["blade_bend"], "upAxis", "y", "string");
            SetParam(nodes["blade_bend"], "captureLength", "0.25", "float");

            // ---- Guard (护手) ----
            nodes["guard_box"] = data.AddNode("Box", new Vector2(0, 250));
            SetParam(nodes["guard_box"], "sizeX", "0.08", "float");
            SetParam(nodes["guard_box"], "sizeY", "0.015", "float");
            SetParam(nodes["guard_box"], "sizeZ", "0.02", "float");

            nodes["guard_transform"] = data.AddNode("Transform", new Vector2(250, 250));
            SetParam(nodes["guard_transform"], "translate", "0,-0.125,0", "Vector3");

            // ---- Handle (手柄) ----
            nodes["handle_box"] = data.AddNode("Box", new Vector2(0, 500));
            SetParam(nodes["handle_box"], "sizeX", "0.03", "float");
            SetParam(nodes["handle_box"], "sizeY", "0.12", "float");
            SetParam(nodes["handle_box"], "sizeZ", "0.03", "float");

            nodes["handle_subdiv"] = data.AddNode("Subdivide", new Vector2(250, 500));
            SetParam(nodes["handle_subdiv"], "iterations", "2", "int");
            SetParam(nodes["handle_subdiv"], "algorithm", "catmull-clark", "string");

            nodes["handle_pattern"] = data.AddNode("Mountain", new Vector2(500, 500));
            SetParam(nodes["handle_pattern"], "height", "0.002", "float");
            SetParam(nodes["handle_pattern"], "frequency", "8", "float");
            SetParam(nodes["handle_pattern"], "octaves", "3", "int");
            SetParam(nodes["handle_pattern"], "lacunarity", "2", "float");
            SetParam(nodes["handle_pattern"], "persistence", "0.5", "float");
            SetParam(nodes["handle_pattern"], "seed", "42", "int");
            SetParam(nodes["handle_pattern"], "noiseType", "perlin", "string");

            nodes["handle_transform"] = data.AddNode("Transform", new Vector2(750, 500));
            SetParam(nodes["handle_transform"], "translate", "0,-0.19,0", "Vector3");

            nodes["handle_group"] = data.AddNode("GroupCreate", new Vector2(1000, 500));
            SetParam(nodes["handle_group"], "groupName", "handle", "string");
            SetParam(nodes["handle_group"], "groupType", "primitive", "string");
            SetParam(nodes["handle_group"], "filter", "", "string");

            // ---- Assembly (组装) ----
            nodes["merge_all"] = data.AddNode("Merge", new Vector2(1250, 250));

            nodes["fuse"] = data.AddNode("Fuse", new Vector2(1500, 250));
            SetParam(nodes["fuse"], "distance", "0.001", "float");

            nodes["normals"] = data.AddNode("Normal", new Vector2(1750, 250));
            SetParam(nodes["normals"], "type", "point", "string");
            SetParam(nodes["normals"], "cuspAngle", "60", "float");
            SetParam(nodes["normals"], "weightByArea", "true", "bool");

            nodes["uv_project"] = data.AddNode("UVProject", new Vector2(2000, 250));
            SetParam(nodes["uv_project"], "projectionType", "cubic", "string");

            nodes["mat_blade"] = data.AddNode("MaterialAssign", new Vector2(2250, 250));
            SetParam(nodes["mat_blade"], "group", "blade", "string");
            SetParam(nodes["mat_blade"], "materialPath", "", "string");
            SetParam(nodes["mat_blade"], "materialId", "0", "int");

            nodes["mat_handle"] = data.AddNode("MaterialAssign", new Vector2(2500, 250));
            SetParam(nodes["mat_handle"], "group", "handle", "string");
            SetParam(nodes["mat_handle"], "materialPath", "", "string");
            SetParam(nodes["mat_handle"], "materialId", "1", "int");

            nodes["save_prefab"] = data.AddNode("SavePrefab", new Vector2(2750, 250));
            SetParam(nodes["save_prefab"], "assetPath", "Assets/PCGOutput/Dagger.prefab", "string");
            SetParam(nodes["save_prefab"], "prefabName", "Dagger", "string");
            SetParam(nodes["save_prefab"], "addCollider", "true", "bool");
            SetParam(nodes["save_prefab"], "convexCollider", "true", "bool");

            // ---- Edges (18 connections) ----
            AddEdge(data, nodes, "blade_box", "blade_subdiv");
            AddEdge(data, nodes, "blade_subdiv", "blade_group");
            AddEdge(data, nodes, "blade_group", "blade_taper");
            AddEdge(data, nodes, "blade_taper", "blade_bend");
            AddEdge(data, nodes, "guard_box", "guard_transform");
            AddEdge(data, nodes, "handle_box", "handle_subdiv");
            AddEdge(data, nodes, "handle_subdiv", "handle_pattern");
            AddEdge(data, nodes, "handle_pattern", "handle_transform");
            AddEdge(data, nodes, "handle_transform", "handle_group");
            AddEdge(data, nodes, "blade_bend", "merge_all");
            AddEdge(data, nodes, "guard_transform", "merge_all");
            AddEdge(data, nodes, "handle_group", "merge_all");
            AddEdge(data, nodes, "merge_all", "fuse");
            AddEdge(data, nodes, "fuse", "normals");
            AddEdge(data, nodes, "normals", "uv_project");
            AddEdge(data, nodes, "uv_project", "mat_blade");
            AddEdge(data, nodes, "mat_blade", "mat_handle");
            AddEdge(data, nodes, "mat_handle", "save_prefab");

            // ---- Exposed Parameters (10) ----
            AddExposed(data, nodes, "blade_box", "sizeX");
            AddExposed(data, nodes, "blade_box", "sizeY");
            AddExposed(data, nodes, "blade_box", "sizeZ");
            AddExposed(data, nodes, "blade_taper", "scaleEnd");
            AddExposed(data, nodes, "handle_box", "sizeX");
            AddExposed(data, nodes, "handle_box", "sizeY");
            AddExposed(data, nodes, "handle_box", "sizeZ");
            AddExposed(data, nodes, "blade_bend", "angle");
            AddExposed(data, nodes, "handle_pattern", "seed");
            AddExposed(data, nodes, "handle_pattern", "height");

            // ---- Groups (4) ----
            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = "Blade (刀身)",
                NodeIds = new List<string>
                {
                    nodes["blade_box"].NodeId, nodes["blade_subdiv"].NodeId,
                    nodes["blade_group"].NodeId, nodes["blade_taper"].NodeId, nodes["blade_bend"].NodeId
                },
                Position = new Vector2(-20, -40),
                Size = new Vector2(1100, 200)
            });

            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = "Guard (护手)",
                NodeIds = new List<string>
                {
                    nodes["guard_box"].NodeId, nodes["guard_transform"].NodeId
                },
                Position = new Vector2(-20, 210),
                Size = new Vector2(350, 200)
            });

            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = "Handle (手柄)",
                NodeIds = new List<string>
                {
                    nodes["handle_box"].NodeId, nodes["handle_subdiv"].NodeId,
                    nodes["handle_pattern"].NodeId, nodes["handle_transform"].NodeId, nodes["handle_group"].NodeId
                },
                Position = new Vector2(-20, 460),
                Size = new Vector2(1100, 200)
            });

            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = "Assembly (组装)",
                NodeIds = new List<string>
                {
                    nodes["merge_all"].NodeId, nodes["fuse"].NodeId, nodes["normals"].NodeId,
                    nodes["uv_project"].NodeId, nodes["mat_blade"].NodeId, nodes["mat_handle"].NodeId,
                    nodes["save_prefab"].NodeId
                },
                Position = new Vector2(1230, 210),
                Size = new Vector2(1600, 200)
            });

            // ---- Sticky Note ----
            data.StickyNotes.Add(new PCGStickyNoteData
            {
                NoteId = System.Guid.NewGuid().ToString(),
                Title = "Procedural Dagger",
                Content = "程序化匕首生成器\n\n暴露参数：\n- 刀身尺寸 (3D)\n- 开刃长度 (scaleEnd)\n- 手柄尺寸 (3D)\n- 弯曲弧度\n- 花纹种子\n- 花纹深度\n\n使用方法：\n1. 保存此图为 .asset\n2. 场景中添加 PCGGraphRunner\n3. 拖入 Graph Asset\n4. Sync Exposed Params\n5. 调整参数 → Run Graph",
                Position = new Vector2(-300, -100),
                Size = new Vector2(260, 400)
            });

            // ---- Save Asset ----
            string dir = "Assets/PCGToolkit/Examples";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                AssetDatabase.CreateFolder("Assets/PCGToolkit", "Examples");
            }
            string path = $"{dir}/ProceduralDagger.asset";
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
            Debug.Log($"[DaggerExampleBuilder] Created Dagger graph at {path} ({data.Nodes.Count} nodes, {data.Edges.Count} edges)");
        }

        private static void SetParam(PCGNodeData node, string key, string value, string type)
        {
            node.Parameters.Add(new PCGSerializedParameter
            {
                Key = key,
                ValueJson = value,
                ValueType = type
            });
        }

        private static void AddEdge(PCGGraphData data, Dictionary<string, PCGNodeData> nodes,
            string fromKey, string toKey)
        {
            data.AddEdge(nodes[fromKey].NodeId, "geometry", nodes[toKey].NodeId, "input");
        }

        private static void AddExposed(PCGGraphData data, Dictionary<string, PCGNodeData> nodes,
            string nodeKey, string paramName)
        {
            data.ExposedParameters.Add(new PCGExposedParamInfo
            {
                NodeId = nodes[nodeKey].NodeId,
                ParamName = paramName
            });
        }
    }
}
