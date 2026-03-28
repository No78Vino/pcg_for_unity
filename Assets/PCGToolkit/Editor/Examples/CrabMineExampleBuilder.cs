using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Examples
{
    public static class CrabMineExampleBuilder
    {
        private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        [MenuItem("PCG Toolkit/Examples/Create CrabMine Graph")]
        public static void CreateCrabMineGraph()
        {
            var data = ScriptableObject.CreateInstance<PCGGraphData>();
            data.GraphName = "CrabMine_ProceduralRobot";

            var nodes = new Dictionary<string, PCGNodeData>();
            float row = 0;

            // =====================================================
            //  Body Group: Main Disc (主体盘)
            // =====================================================
            row = 0;
            nodes["body_disc"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["body_disc"], "radiusOuter", "0.5", "float");
            SetParam(nodes["body_disc"], "radiusInner", "0", "float");
            SetParam(nodes["body_disc"], "height", "0.12", "float");
            SetParam(nodes["body_disc"], "columns", "24", "int");
            SetParam(nodes["body_disc"], "endCaps", "true", "bool");

            nodes["body_disc_bevel"] = data.AddNode("PolyBevel", new Vector2(250, row));
            SetParam(nodes["body_disc_bevel"], "offset", "0.015", "float");
            SetParam(nodes["body_disc_bevel"], "divisions", "2", "int");
            SetParam(nodes["body_disc_bevel"], "group", "", "string");
            AddEdge(data, nodes, "body_disc", "body_disc_bevel");

            // =====================================================
            //  Body Group: Top Dome (顶部穹顶)
            // =====================================================
            row += 200;
            nodes["top_dome"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["top_dome"], "radiusOuter", "0.42", "float");
            SetParam(nodes["top_dome"], "radiusInner", "0", "float");
            SetParam(nodes["top_dome"], "height", "0.08", "float");
            SetParam(nodes["top_dome"], "columns", "24", "int");
            SetParam(nodes["top_dome"], "endCaps", "true", "bool");

            nodes["top_dome_xform"] = data.AddNode("Transform", new Vector2(250, row));
            SetParam(nodes["top_dome_xform"], "translate", "0,0.10,0", "Vector3");
            AddEdge(data, nodes, "top_dome", "top_dome_xform");

            nodes["top_dome_bevel"] = data.AddNode("PolyBevel", new Vector2(500, row));
            SetParam(nodes["top_dome_bevel"], "offset", "0.01", "float");
            SetParam(nodes["top_dome_bevel"], "divisions", "1", "int");
            AddEdge(data, nodes, "top_dome_xform", "top_dome_bevel");

            nodes["top_dome_inset"] = data.AddNode("Inset", new Vector2(750, row));
            SetParam(nodes["top_dome_inset"], "distance", "0.02", "float");
            SetParam(nodes["top_dome_inset"], "group", "", "string");
            SetParam(nodes["top_dome_inset"], "outputInner", "true", "bool");
            SetParam(nodes["top_dome_inset"], "outputSide", "true", "bool");
            AddEdge(data, nodes, "top_dome_bevel", "top_dome_inset");

            // =====================================================
            //  Body Group: Bottom Plate (底部底板)
            // =====================================================
            row += 200;
            nodes["bottom_plate"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["bottom_plate"], "radiusOuter", "0.48", "float");
            SetParam(nodes["bottom_plate"], "radiusInner", "0", "float");
            SetParam(nodes["bottom_plate"], "height", "0.03", "float");
            SetParam(nodes["bottom_plate"], "columns", "24", "int");
            SetParam(nodes["bottom_plate"], "endCaps", "true", "bool");

            nodes["bottom_plate_xform"] = data.AddNode("Transform", new Vector2(250, row));
            SetParam(nodes["bottom_plate_xform"], "translate", "0,-0.06,0", "Vector3");
            AddEdge(data, nodes, "bottom_plate", "bottom_plate_xform");

            // =====================================================
            //  Body Group: Armor Panels (装甲裙板 radial×8)
            // =====================================================
            row += 200;
            nodes["armor_panel"] = data.AddNode("Box", new Vector2(0, row));
            SetParam(nodes["armor_panel"], "sizeX", "0.06", "float");
            SetParam(nodes["armor_panel"], "sizeY", "0.10", "float");
            SetParam(nodes["armor_panel"], "sizeZ", "0.35", "float");
            SetParam(nodes["armor_panel"], "center", "0.46,0.08,0", "Vector3");

            nodes["armor_panel_bevel"] = data.AddNode("PolyBevel", new Vector2(250, row));
            SetParam(nodes["armor_panel_bevel"], "offset", "0.008", "float");
            SetParam(nodes["armor_panel_bevel"], "divisions", "2", "int");
            AddEdge(data, nodes, "armor_panel", "armor_panel_bevel");

            nodes["armor_panel_inset"] = data.AddNode("Inset", new Vector2(500, row));
            SetParam(nodes["armor_panel_inset"], "distance", "0.01", "float");
            SetParam(nodes["armor_panel_inset"], "outputInner", "true", "bool");
            SetParam(nodes["armor_panel_inset"], "outputSide", "true", "bool");
            AddEdge(data, nodes, "armor_panel_bevel", "armor_panel_inset");

            nodes["armor_panel_array"] = data.AddNode("Array", new Vector2(750, row));
            SetParam(nodes["armor_panel_array"], "mode", "radial", "string");
            SetParam(nodes["armor_panel_array"], "count", "8", "int");
            SetParam(nodes["armor_panel_array"], "axis", "0,1,0", "Vector3");
            SetParam(nodes["armor_panel_array"], "center", "0,0,0", "Vector3");
            SetParam(nodes["armor_panel_array"], "fullAngle", "360", "float");
            AddEdge(data, nodes, "armor_panel_inset", "armor_panel_array");

            // =====================================================
            //  Body Group: Top Handle (顶部提手)
            // =====================================================
            row += 200;
            nodes["handle"] = data.AddNode("Box", new Vector2(0, row));
            SetParam(nodes["handle"], "sizeX", "0.15", "float");
            SetParam(nodes["handle"], "sizeY", "0.03", "float");
            SetParam(nodes["handle"], "sizeZ", "0.04", "float");
            SetParam(nodes["handle"], "center", "0,0.26,0", "Vector3");

            nodes["handle_bevel"] = data.AddNode("PolyBevel", new Vector2(250, row));
            SetParam(nodes["handle_bevel"], "offset", "0.005", "float");
            SetParam(nodes["handle_bevel"], "divisions", "2", "int");
            AddEdge(data, nodes, "handle", "handle_bevel");

            // =====================================================
            //  Body Group: Canister (背部圆筒)
            // =====================================================
            row += 200;
            nodes["canister"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["canister"], "radiusOuter", "0.06", "float");
            SetParam(nodes["canister"], "radiusInner", "0", "float");
            SetParam(nodes["canister"], "height", "0.28", "float");
            SetParam(nodes["canister"], "columns", "16", "int");
            SetParam(nodes["canister"], "endCaps", "true", "bool");

            nodes["canister_xform"] = data.AddNode("Transform", new Vector2(250, row));
            SetParam(nodes["canister_xform"], "translate", "0,0.22,-0.35", "Vector3");
            SetParam(nodes["canister_xform"], "rotate", "0,0,90", "Vector3");
            AddEdge(data, nodes, "canister", "canister_xform");

            // =====================================================
            //  Body Group: Canister Caps (圆筒端盖 via Sphere+Clip+Mirror)
            // =====================================================
            row += 200;
            nodes["cap_sphere"] = data.AddNode("Sphere", new Vector2(0, row));
            SetParam(nodes["cap_sphere"], "radius", "0.06", "float");
            SetParam(nodes["cap_sphere"], "rows", "8", "int");
            SetParam(nodes["cap_sphere"], "columns", "16", "int");

            nodes["cap_clip"] = data.AddNode("Clip", new Vector2(250, row));
            SetParam(nodes["cap_clip"], "origin", "0,0,0", "Vector3");
            SetParam(nodes["cap_clip"], "normal", "1,0,0", "Vector3");
            SetParam(nodes["cap_clip"], "keepAbove", "true", "bool");
            AddEdge(data, nodes, "cap_sphere", "cap_clip");

            nodes["cap_xform_a"] = data.AddNode("Transform", new Vector2(500, row));
            SetParam(nodes["cap_xform_a"], "translate", "0.14,0.22,-0.35", "Vector3");
            AddEdge(data, nodes, "cap_clip", "cap_xform_a");

            nodes["cap_mirror"] = data.AddNode("Mirror", new Vector2(750, row));
            SetParam(nodes["cap_mirror"], "origin", "0,0.22,-0.35", "Vector3");
            SetParam(nodes["cap_mirror"], "normal", "1,0,0", "Vector3");
            SetParam(nodes["cap_mirror"], "keepOriginal", "true", "bool");
            AddEdge(data, nodes, "cap_xform_a", "cap_mirror");

            // =====================================================
            //  Body Group: Antenna (天线)
            // =====================================================
            row += 200;
            nodes["antenna_rod"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["antenna_rod"], "radiusOuter", "0.004", "float");
            SetParam(nodes["antenna_rod"], "radiusInner", "0", "float");
            SetParam(nodes["antenna_rod"], "height", "0.25", "float");
            SetParam(nodes["antenna_rod"], "columns", "6", "int");
            SetParam(nodes["antenna_rod"], "endCaps", "true", "bool");

            nodes["antenna_rod_xform"] = data.AddNode("Transform", new Vector2(250, row));
            SetParam(nodes["antenna_rod_xform"], "translate", "0.05,0.35,0.1", "Vector3");
            AddEdge(data, nodes, "antenna_rod", "antenna_rod_xform");

            nodes["antenna_base"] = data.AddNode("Tube", new Vector2(0, row + 100));
            SetParam(nodes["antenna_base"], "radiusOuter", "0.015", "float");
            SetParam(nodes["antenna_base"], "radiusInner", "0", "float");
            SetParam(nodes["antenna_base"], "height", "0.02", "float");
            SetParam(nodes["antenna_base"], "columns", "8", "int");
            SetParam(nodes["antenna_base"], "endCaps", "true", "bool");

            nodes["antenna_base_xform"] = data.AddNode("Transform", new Vector2(250, row + 100));
            SetParam(nodes["antenna_base_xform"], "translate", "0.05,0.22,0.1", "Vector3");
            AddEdge(data, nodes, "antenna_base", "antenna_base_xform");

            nodes["antenna_tip"] = data.AddNode("Sphere", new Vector2(0, row + 200));
            SetParam(nodes["antenna_tip"], "radius", "0.008", "float");
            SetParam(nodes["antenna_tip"], "rows", "6", "int");
            SetParam(nodes["antenna_tip"], "columns", "8", "int");
            SetParam(nodes["antenna_tip"], "center", "0.05,0.48,0.1", "Vector3");

            // =====================================================
            //  Body Group: Indicator Lights (指示灯)
            // =====================================================
            nodes["light_01"] = data.AddNode("Sphere", new Vector2(0, row + 350));
            SetParam(nodes["light_01"], "radius", "0.015", "float");
            SetParam(nodes["light_01"], "rows", "6", "int");
            SetParam(nodes["light_01"], "columns", "8", "int");
            SetParam(nodes["light_01"], "center", "0.15,0.15,0.38", "Vector3");

            nodes["light_02"] = data.AddNode("Sphere", new Vector2(250, row + 350));
            SetParam(nodes["light_02"], "radius", "0.012", "float");
            SetParam(nodes["light_02"], "rows", "6", "int");
            SetParam(nodes["light_02"], "columns", "8", "int");
            SetParam(nodes["light_02"], "center", "-0.2,0.10,0.40", "Vector3");

            // =====================================================
            //  Merge Body Parts
            // =====================================================
            nodes["merge_body"] = data.AddNode("Merge", new Vector2(1100, 600));
            AddMergeEdges(data, nodes, "merge_body", new[]
            {
                "body_disc_bevel", "top_dome_inset", "bottom_plate_xform",
                "armor_panel_array", "handle_bevel", "canister_xform",
                "cap_mirror", "antenna_rod_xform", "antenna_base_xform", "antenna_tip"
            });

            // Body subdivide for smoother appearance
            nodes["body_subdivide"] = data.AddNode("Subdivide", new Vector2(1350, 600));
            SetParam(nodes["body_subdivide"], "iterations", "1", "int");
            SetParam(nodes["body_subdivide"], "algorithm", "linear", "string");
            AddEdge(data, nodes, "merge_body", "body_subdivide");

            // =====================================================
            //  Leg Group: Upper Leg (大腿)
            // =====================================================
            float legBase = 2200;

            nodes["leg_upper"] = data.AddNode("Box", new Vector2(0, legBase));
            SetParam(nodes["leg_upper"], "sizeX", "0.22", "float");
            SetParam(nodes["leg_upper"], "sizeY", "0.06", "float");
            SetParam(nodes["leg_upper"], "sizeZ", "0.07", "float");

            nodes["leg_upper_bevel"] = data.AddNode("PolyBevel", new Vector2(250, legBase));
            SetParam(nodes["leg_upper_bevel"], "offset", "0.008", "float");
            SetParam(nodes["leg_upper_bevel"], "divisions", "2", "int");
            AddEdge(data, nodes, "leg_upper", "leg_upper_bevel");

            nodes["leg_upper_extrude"] = data.AddNode("Extrude", new Vector2(500, legBase));
            SetParam(nodes["leg_upper_extrude"], "distance", "0.005", "float");
            SetParam(nodes["leg_upper_extrude"], "inset", "0.01", "float");
            SetParam(nodes["leg_upper_extrude"], "individual", "true", "bool");
            AddEdge(data, nodes, "leg_upper_bevel", "leg_upper_extrude");

            // =====================================================
            //  Leg Group: Joints (关节球)
            // =====================================================
            nodes["joint_mount"] = data.AddNode("Sphere", new Vector2(0, legBase + 150));
            SetParam(nodes["joint_mount"], "radius", "0.04", "float");
            SetParam(nodes["joint_mount"], "rows", "8", "int");
            SetParam(nodes["joint_mount"], "columns", "12", "int");
            SetParam(nodes["joint_mount"], "center", "-0.13,0,0", "Vector3");

            nodes["joint_elbow"] = data.AddNode("Sphere", new Vector2(0, legBase + 300));
            SetParam(nodes["joint_elbow"], "radius", "0.035", "float");
            SetParam(nodes["joint_elbow"], "rows", "8", "int");
            SetParam(nodes["joint_elbow"], "columns", "12", "int");
            SetParam(nodes["joint_elbow"], "center", "0.13,0,0", "Vector3");

            // =====================================================
            //  Leg Group: Upper Leg Assembly
            // =====================================================
            nodes["merge_upper_leg"] = data.AddNode("Merge", new Vector2(750, legBase + 100));
            AddMergeEdges(data, nodes, "merge_upper_leg", new[]
            {
                "leg_upper_extrude", "joint_mount", "joint_elbow"
            });

            nodes["upper_leg_xform"] = data.AddNode("Transform", new Vector2(1000, legBase + 100));
            SetParam(nodes["upper_leg_xform"], "translate", "0.55,0,0", "Vector3");
            SetParam(nodes["upper_leg_xform"], "rotate", "0,0,-30", "Vector3");
            AddEdge(data, nodes, "merge_upper_leg", "upper_leg_xform");

            // =====================================================
            //  Leg Group: Lower Leg (小腿)
            // =====================================================
            nodes["leg_lower"] = data.AddNode("Box", new Vector2(0, legBase + 500));
            SetParam(nodes["leg_lower"], "sizeX", "0.20", "float");
            SetParam(nodes["leg_lower"], "sizeY", "0.05", "float");
            SetParam(nodes["leg_lower"], "sizeZ", "0.06", "float");

            nodes["leg_lower_bevel"] = data.AddNode("PolyBevel", new Vector2(250, legBase + 500));
            SetParam(nodes["leg_lower_bevel"], "offset", "0.006", "float");
            SetParam(nodes["leg_lower_bevel"], "divisions", "2", "int");
            AddEdge(data, nodes, "leg_lower", "leg_lower_bevel");

            nodes["leg_lower_extrude"] = data.AddNode("Extrude", new Vector2(500, legBase + 500));
            SetParam(nodes["leg_lower_extrude"], "distance", "0.004", "float");
            SetParam(nodes["leg_lower_extrude"], "inset", "0.008", "float");
            SetParam(nodes["leg_lower_extrude"], "individual", "true", "bool");
            AddEdge(data, nodes, "leg_lower_bevel", "leg_lower_extrude");

            nodes["joint_ankle"] = data.AddNode("Sphere", new Vector2(0, legBase + 650));
            SetParam(nodes["joint_ankle"], "radius", "0.03", "float");
            SetParam(nodes["joint_ankle"], "rows", "6", "int");
            SetParam(nodes["joint_ankle"], "columns", "10", "int");
            SetParam(nodes["joint_ankle"], "center", "0.12,0,0", "Vector3");

            // =====================================================
            //  Leg Group: Lower Leg Assembly
            // =====================================================
            nodes["merge_lower_leg"] = data.AddNode("Merge", new Vector2(750, legBase + 550));
            AddMergeEdges(data, nodes, "merge_lower_leg", new[]
            {
                "leg_lower_extrude", "joint_ankle"
            });

            nodes["lower_leg_xform"] = data.AddNode("Transform", new Vector2(1000, legBase + 550));
            SetParam(nodes["lower_leg_xform"], "translate", "0.75,-0.12,0", "Vector3");
            SetParam(nodes["lower_leg_xform"], "rotate", "0,0,-60", "Vector3");
            AddEdge(data, nodes, "merge_lower_leg", "lower_leg_xform");

            // =====================================================
            //  Leg Group: Foot (足爪 via Boolean cut)
            // =====================================================
            nodes["foot_base"] = data.AddNode("Box", new Vector2(0, legBase + 850));
            SetParam(nodes["foot_base"], "sizeX", "0.12", "float");
            SetParam(nodes["foot_base"], "sizeY", "0.04", "float");
            SetParam(nodes["foot_base"], "sizeZ", "0.08", "float");

            nodes["foot_cutter"] = data.AddNode("Box", new Vector2(0, legBase + 1000));
            SetParam(nodes["foot_cutter"], "sizeX", "0.10", "float");
            SetParam(nodes["foot_cutter"], "sizeY", "0.08", "float");
            SetParam(nodes["foot_cutter"], "sizeZ", "0.10", "float");
            SetParam(nodes["foot_cutter"], "center", "0.04,0.03,0", "Vector3");

            nodes["foot_cutter_xform"] = data.AddNode("Transform", new Vector2(250, legBase + 1000));
            SetParam(nodes["foot_cutter_xform"], "rotate", "0,0,-25", "Vector3");
            AddEdge(data, nodes, "foot_cutter", "foot_cutter_xform");

            nodes["foot_boolean"] = data.AddNode("Boolean", new Vector2(500, legBase + 900));
            SetParam(nodes["foot_boolean"], "operation", "subtract", "string");
            AddEdge(data, nodes, "foot_base", "foot_boolean", "inputA");
            AddEdge(data, nodes, "foot_cutter_xform", "foot_boolean", "inputB");

            nodes["foot_bevel"] = data.AddNode("PolyBevel", new Vector2(750, legBase + 900));
            SetParam(nodes["foot_bevel"], "offset", "0.004", "float");
            SetParam(nodes["foot_bevel"], "divisions", "1", "int");
            AddEdge(data, nodes, "foot_boolean", "foot_bevel");

            nodes["foot_xform"] = data.AddNode("Transform", new Vector2(1000, legBase + 900));
            SetParam(nodes["foot_xform"], "translate", "0.88,-0.35,0", "Vector3");
            SetParam(nodes["foot_xform"], "rotate", "0,0,-15", "Vector3");
            AddEdge(data, nodes, "foot_bevel", "foot_xform");

            // =====================================================
            //  Leg Group: Merge Single Leg -> Radial Array ×6
            // =====================================================
            nodes["merge_single_leg"] = data.AddNode("Merge", new Vector2(1250, legBase + 500));
            AddMergeEdges(data, nodes, "merge_single_leg", new[]
            {
                "upper_leg_xform", "lower_leg_xform", "foot_xform"
            });

            nodes["leg_array"] = data.AddNode("Array", new Vector2(1500, legBase + 500));
            SetParam(nodes["leg_array"], "mode", "radial", "string");
            SetParam(nodes["leg_array"], "count", "6", "int");
            SetParam(nodes["leg_array"], "axis", "0,1,0", "Vector3");
            SetParam(nodes["leg_array"], "center", "0,0,0", "Vector3");
            SetParam(nodes["leg_array"], "fullAngle", "360", "float");
            AddEdge(data, nodes, "merge_single_leg", "leg_array");

            // =====================================================
            //  Final Assembly
            // =====================================================
            nodes["merge_final"] = data.AddNode("Merge", new Vector2(1700, 1200));
            AddMergeEdges(data, nodes, "merge_final", new[]
            {
                "body_subdivide", "leg_array", "light_01", "light_02"
            });

            nodes["fuse"] = data.AddNode("Fuse", new Vector2(1950, 1200));
            SetParam(nodes["fuse"], "distance", "0.001", "float");
            AddEdge(data, nodes, "merge_final", "fuse");

            nodes["normals"] = data.AddNode("Normal", new Vector2(2200, 1200));
            SetParam(nodes["normals"], "type", "point", "string");
            SetParam(nodes["normals"], "cuspAngle", "60", "float");
            SetParam(nodes["normals"], "weightByArea", "true", "bool");
            AddEdge(data, nodes, "fuse", "normals");

            nodes["uv_project"] = data.AddNode("UVProject", new Vector2(2450, 1200));
            SetParam(nodes["uv_project"], "projectionType", "cubic", "string");
            AddEdge(data, nodes, "normals", "uv_project");

            nodes["save_prefab"] = data.AddNode("SavePrefab", new Vector2(2700, 1200));
            SetParam(nodes["save_prefab"], "assetPath", "Assets/PCGOutput/CrabMine.prefab", "string");
            SetParam(nodes["save_prefab"], "prefabName", "CrabMine", "string");
            SetParam(nodes["save_prefab"], "addCollider", "true", "bool");
            SetParam(nodes["save_prefab"], "convexCollider", "true", "bool");
            AddEdge(data, nodes, "uv_project", "save_prefab");

            // =====================================================
            //  Groups
            // =====================================================
            AddGroup(data, nodes, "Body (主体)",
                new[] { "body_disc", "top_dome", "bottom_plate", "armor_panel",
                    "handle", "canister", "cap_sphere", "antenna_rod", "antenna_base",
                    "antenna_tip", "light_01", "light_02",
                    "merge_body", "body_subdivide" },
                new Vector2(-20, -40), new Vector2(1300, 2200));

            AddGroup(data, nodes, "Leg (腿部)",
                new[] { "leg_upper", "leg_lower", "foot_base", "foot_cutter",
                    "joint_mount", "joint_elbow", "joint_ankle",
                    "merge_single_leg", "leg_array" },
                new Vector2(-20, legBase - 40), new Vector2(1700, 1300));

            AddGroup(data, nodes, "Output (输出)",
                new[] { "merge_final", "fuse", "normals", "uv_project", "save_prefab" },
                new Vector2(1680, 1160), new Vector2(1100, 300));

            // =====================================================
            //  Sticky Note
            // =====================================================
            data.StickyNotes.Add(new PCGStickyNoteData
            {
                NoteId = System.Guid.NewGuid().ToString(),
                Title = "Crab Mine Robot",
                Content =
                    "蟹形地雷机器人 - 程序化白模\n\n" +
                    "部件结构：\n" +
                    "- 主体盘 Tube → PolyBevel\n" +
                    "- 顶部穹顶 Tube → Transform → PolyBevel → Inset\n" +
                    "- 装甲裙板 Box → Bevel → Inset → Array(radial×8)\n" +
                    "- 圆筒端盖 Sphere → Clip → Mirror\n" +
                    "- 天线 Tube + Sphere\n" +
                    "- 大腿 Box → Bevel → Extrude\n" +
                    "- 小腿 Box → Bevel → Extrude\n" +
                    "- 足爪 Box → Boolean(subtract) → Bevel\n" +
                    "- 关节球 Sphere×3\n" +
                    "- 单腿组装 → Array(radial×6)\n\n" +
                    "使用方法：\n" +
                    "1. 菜单 PCG Toolkit > Examples > Create CrabMine Graph\n" +
                    "2. 在 Node Editor 中打开生成的 asset\n" +
                    "3. 执行图生成 Prefab",
                Position = new Vector2(-400, -100),
                Size = new Vector2(350, 550)
            });

            // =====================================================
            //  Exposed Parameters
            // =====================================================
            data.ExposedParameters.Add(new PCGExposedParamInfo
            {
                NodeId = nodes["body_disc"].NodeId,
                ParamName = "radiusOuter"
            });
            data.ExposedParameters.Add(new PCGExposedParamInfo
            {
                NodeId = nodes["leg_array"].NodeId,
                ParamName = "count"
            });
            data.ExposedParameters.Add(new PCGExposedParamInfo
            {
                NodeId = nodes["armor_panel_array"].NodeId,
                ParamName = "count"
            });
            data.ExposedParameters.Add(new PCGExposedParamInfo
            {
                NodeId = nodes["body_disc_bevel"].NodeId,
                ParamName = "offset"
            });

            // =====================================================
            //  Save Asset
            // =====================================================
            string dir = "Assets/PCGToolkit/Examples";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/PCGToolkit", "Examples");

            string path = $"{dir}/CrabMine_Robot.asset";
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
            Debug.Log($"[CrabMineExampleBuilder] Created CrabMine graph at {path} ({data.Nodes.Count} nodes, {data.Edges.Count} edges)");
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
            string fromKey, string toKey, string toPort = "input")
        {
            data.AddEdge(nodes[fromKey].NodeId, "geometry", nodes[toKey].NodeId, toPort);
        }

        private static void AddMergeEdges(PCGGraphData data, Dictionary<string, PCGNodeData> nodes,
            string mergeKey, string[] fromKeys)
        {
            for (int i = 0; i < fromKeys.Length; i++)
            {
                AddEdge(data, nodes, fromKeys[i], mergeKey, $"input{i}");
            }
        }

        private static void AddGroup(PCGGraphData data, Dictionary<string, PCGNodeData> nodes,
            string title, string[] partIds, Vector2 position, Vector2 size)
        {
            var nodeIds = new List<string>();
            foreach (var id in partIds)
            {
                if (nodes.ContainsKey(id))
                    nodeIds.Add(nodes[id].NodeId);
                if (nodes.ContainsKey(id + "_xform"))
                    nodeIds.Add(nodes[id + "_xform"].NodeId);
                // Also include related nodes by naming convention
                if (nodes.ContainsKey(id + "_bevel"))
                    nodeIds.Add(nodes[id + "_bevel"].NodeId);
                if (nodes.ContainsKey(id + "_inset"))
                    nodeIds.Add(nodes[id + "_inset"].NodeId);
                if (nodes.ContainsKey(id + "_extrude"))
                    nodeIds.Add(nodes[id + "_extrude"].NodeId);
                if (nodes.ContainsKey(id + "_array"))
                    nodeIds.Add(nodes[id + "_array"].NodeId);
                if (nodes.ContainsKey(id + "_clip"))
                    nodeIds.Add(nodes[id + "_clip"].NodeId);
                if (nodes.ContainsKey(id + "_mirror"))
                    nodeIds.Add(nodes[id + "_mirror"].NodeId);
                if (nodes.ContainsKey(id + "_boolean"))
                    nodeIds.Add(nodes[id + "_boolean"].NodeId);
                if (nodes.ContainsKey(id + "_merge"))
                    nodeIds.Add(nodes[id + "_merge"].NodeId);
            }

            // Deduplicate
            var uniqueIds = new HashSet<string>(nodeIds);

            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = title,
                NodeIds = new List<string>(uniqueIds),
                Position = position,
                Size = size
            });
        }
    }
}
