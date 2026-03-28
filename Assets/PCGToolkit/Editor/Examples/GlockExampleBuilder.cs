using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Examples
{
    public static class GlockExampleBuilder
    {
        private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        [MenuItem("PCG Toolkit/Examples/Create Glock Graph")]
        public static void CreateGlockGraph()
        {
            var data = ScriptableObject.CreateInstance<PCGGraphData>();
            data.GraphName = "ProceduralGlock";

            var nodes = new Dictionary<string, PCGNodeData>();
            float row = 0;

            // =====================================================
            //  Group 1: Slide (套筒) — 9 primitives
            // =====================================================
            row = 0;
            CreateBoxAndTransform(data, nodes, "slide_main",
                0.16f, 0.14f, 0.62f, 0, 0.36f, 0.12f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_top_flat",
                0.12f, 0.02f, 0.62f, 0, 0.44f, 0.12f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_front_bevel",
                0.14f, 0.12f, 0.03f, 0, 0.35f, 0.44f, 5, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_rear_serrations",
                0.165f, 0.13f, 0.10f, 0, 0.36f, -0.18f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "ejection_port",
                0.08f, 0.06f, 0.12f, 0.04f, 0.38f, 0.05f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "front_sight",
                0.04f, 0.03f, 0.02f, 0, 0.46f, 0.38f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "rear_sight",
                0.06f, 0.035f, 0.025f, 0, 0.46f, -0.15f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "rear_sight_notch_left",
                0.015f, 0.035f, 0.025f, -0.025f, 0.46f, -0.15f, 0, 0, 0, row);

            row += 180;
            CreateBoxAndTransform(data, nodes, "rear_sight_notch_right",
                0.015f, 0.035f, 0.025f, 0.025f, 0.46f, -0.15f, 0, 0, 0, row);

            // Merge slide group
            nodes["merge_slide"] = data.AddNode("Merge", new Vector2(600, 720));
            string[] slideParts = {
                "slide_main", "slide_top_flat", "slide_front_bevel", "slide_rear_serrations",
                "ejection_port", "front_sight", "rear_sight", "rear_sight_notch_left", "rear_sight_notch_right"
            };
            AddMergeEdges(data, nodes, "merge_slide",
                System.Array.ConvertAll(slideParts, p => p + "_xform"));

            // =====================================================
            //  Group 2: Barrel (枪管) — 2 primitives
            // =====================================================
            float barrelBase = 1800;

            nodes["barrel_outer"] = data.AddNode("Tube", new Vector2(0, barrelBase));
            SetParam(nodes["barrel_outer"], "radiusOuter", "0.04", "float");
            SetParam(nodes["barrel_outer"], "radiusInner", "0", "float");
            SetParam(nodes["barrel_outer"], "height", "0.15", "float");
            SetParam(nodes["barrel_outer"], "columns", "16", "int");
            SetParam(nodes["barrel_outer"], "endCaps", "true", "bool");

            nodes["barrel_outer_xform"] = data.AddNode("Transform", new Vector2(300, barrelBase));
            SetParam(nodes["barrel_outer_xform"], "translate", "0,0.33,0.50", "Vector3");
            SetParam(nodes["barrel_outer_xform"], "rotate", "90,0,0", "Vector3");
            AddEdge(data, nodes, "barrel_outer", "barrel_outer_xform");

            nodes["barrel_bore"] = data.AddNode("Tube", new Vector2(0, barrelBase + 180));
            SetParam(nodes["barrel_bore"], "radiusOuter", "0.025", "float");
            SetParam(nodes["barrel_bore"], "radiusInner", "0", "float");
            SetParam(nodes["barrel_bore"], "height", "0.16", "float");
            SetParam(nodes["barrel_bore"], "columns", "16", "int");
            SetParam(nodes["barrel_bore"], "endCaps", "true", "bool");

            nodes["barrel_bore_xform"] = data.AddNode("Transform", new Vector2(300, barrelBase + 180));
            SetParam(nodes["barrel_bore_xform"], "translate", "0,0.33,0.50", "Vector3");
            SetParam(nodes["barrel_bore_xform"], "rotate", "90,0,0", "Vector3");
            AddEdge(data, nodes, "barrel_bore", "barrel_bore_xform");

            // Merge barrel group
            nodes["merge_barrel"] = data.AddNode("Merge", new Vector2(600, barrelBase + 90));
            AddMergeEdges(data, nodes, "merge_barrel",
                new[] { "barrel_outer_xform", "barrel_bore_xform" });

            // =====================================================
            //  Group 3: Frame (机匣) — 5 primitives
            // =====================================================
            float frameBase = 2300;
            row = frameBase;

            CreateBoxAndTransform(data, nodes, "frame_main",
                0.16f, 0.10f, 0.52f, 0, 0.24f, 0.10f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "frame_front_extension",
                0.16f, 0.08f, 0.12f, 0, 0.22f, 0.38f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "accessory_rail_1",
                0.17f, 0.012f, 0.04f, 0, 0.195f, 0.36f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "accessory_rail_2",
                0.17f, 0.012f, 0.04f, 0, 0.175f, 0.36f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "frame_rear_tang",
                0.15f, 0.06f, 0.06f, 0, 0.27f, -0.20f, 15, 0, 0, row);

            nodes["merge_frame"] = data.AddNode("Merge", new Vector2(600, frameBase + 360));
            string[] frameParts = {
                "frame_main", "frame_front_extension", "accessory_rail_1",
                "accessory_rail_2", "frame_rear_tang"
            };
            AddMergeEdges(data, nodes, "merge_frame",
                System.Array.ConvertAll(frameParts, p => p + "_xform"));

            // =====================================================
            //  Group 4: Trigger Guard (扳机护圈) — 4 primitives
            // =====================================================
            float tgBase = 3300;
            row = tgBase;

            CreateBoxAndTransform(data, nodes, "trigger_guard_front",
                0.12f, 0.015f, 0.08f, 0, 0.12f, 0.22f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "trigger_guard_bottom",
                0.12f, 0.015f, 0.14f, 0, 0.08f, 0.15f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "trigger_guard_front_curve",
                0.12f, 0.04f, 0.015f, 0, 0.10f, 0.26f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "trigger_guard_rear",
                0.12f, 0.04f, 0.015f, 0, 0.10f, 0.08f, 15, 0, 0, row);

            nodes["merge_trigger_guard"] = data.AddNode("Merge", new Vector2(600, tgBase + 270));
            string[] tgParts = {
                "trigger_guard_front", "trigger_guard_bottom",
                "trigger_guard_front_curve", "trigger_guard_rear"
            };
            AddMergeEdges(data, nodes, "merge_trigger_guard",
                System.Array.ConvertAll(tgParts, p => p + "_xform"));

            // =====================================================
            //  Group 5: Trigger (扳机) — 3 primitives
            // =====================================================
            float trigBase = 4100;
            row = trigBase;

            CreateBoxAndTransform(data, nodes, "trigger_body",
                0.03f, 0.06f, 0.015f, 0, 0.14f, 0.15f, -10, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "trigger_face",
                0.025f, 0.025f, 0.008f, 0, 0.12f, 0.155f, 0, 0, 0, row);

            row += 180;
            nodes["trigger_pivot"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["trigger_pivot"], "radiusOuter", "0.005", "float");
            SetParam(nodes["trigger_pivot"], "radiusInner", "0", "float");
            SetParam(nodes["trigger_pivot"], "height", "0.16", "float");
            SetParam(nodes["trigger_pivot"], "columns", "8", "int");
            SetParam(nodes["trigger_pivot"], "endCaps", "true", "bool");

            nodes["trigger_pivot_xform"] = data.AddNode("Transform", new Vector2(300, row));
            SetParam(nodes["trigger_pivot_xform"], "translate", "0,0.17,0.15", "Vector3");
            AddEdge(data, nodes, "trigger_pivot", "trigger_pivot_xform");

            nodes["merge_trigger"] = data.AddNode("Merge", new Vector2(600, trigBase + 180));
            AddMergeEdges(data, nodes, "merge_trigger",
                new[] { "trigger_body_xform", "trigger_face_xform", "trigger_pivot_xform" });

            // =====================================================
            //  Group 6: Grip (握把) — 6 primitives
            // =====================================================
            float gripBase = 4700;
            row = gripBase;

            CreateBoxAndTransform(data, nodes, "grip_main",
                0.15f, 0.32f, 0.10f, 0, -0.02f, -0.05f, -18, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "grip_front_strap",
                0.14f, 0.28f, 0.02f, 0, -0.01f, 0.01f, -15, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "grip_back_strap",
                0.14f, 0.28f, 0.02f, 0, -0.03f, -0.10f, -20, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "grip_left_panel",
                0.015f, 0.22f, 0.08f, -0.075f, -0.02f, -0.05f, -18, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "grip_right_panel",
                0.015f, 0.22f, 0.08f, 0.075f, -0.02f, -0.05f, -18, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "grip_bottom_swell",
                0.16f, 0.04f, 0.11f, 0, -0.17f, -0.12f, -18, 0, 0, row);

            nodes["merge_grip"] = data.AddNode("Merge", new Vector2(600, gripBase + 450));
            string[] gripParts = {
                "grip_main", "grip_front_strap", "grip_back_strap",
                "grip_left_panel", "grip_right_panel", "grip_bottom_swell"
            };
            AddMergeEdges(data, nodes, "merge_grip",
                System.Array.ConvertAll(gripParts, p => p + "_xform"));

            // =====================================================
            //  Group 7: Magazine (弹匣) — 3 primitives
            // =====================================================
            float magBase = 5900;
            row = magBase;

            CreateBoxAndTransform(data, nodes, "magazine_body",
                0.12f, 0.30f, 0.08f, 0, -0.04f, -0.05f, -18, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "magazine_baseplate",
                0.14f, 0.025f, 0.10f, 0, -0.22f, -0.16f, -18, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "magazine_baseplate_lip",
                0.14f, 0.015f, 0.015f, 0, -0.23f, -0.11f, -18, 0, 0, row);

            nodes["merge_magazine"] = data.AddNode("Merge", new Vector2(600, magBase + 180));
            string[] magParts = { "magazine_body", "magazine_baseplate", "magazine_baseplate_lip" };
            AddMergeEdges(data, nodes, "merge_magazine",
                System.Array.ConvertAll(magParts, p => p + "_xform"));

            // =====================================================
            //  Group 8: Controls (操控部件) — 6 primitives
            // =====================================================
            float ctrlBase = 6500;
            row = ctrlBase;

            CreateBoxAndTransform(data, nodes, "slide_lock",
                0.02f, 0.025f, 0.04f, -0.09f, 0.30f, 0.10f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_lock_lever",
                0.015f, 0.015f, 0.025f, -0.095f, 0.31f, 0.08f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "takedown_lever",
                0.025f, 0.02f, 0.03f, -0.09f, 0.24f, 0.18f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "magazine_release",
                0.02f, 0.02f, 0.02f, -0.085f, 0.20f, 0.06f, 0, 0, 0, row);

            row += 180;
            nodes["pin_front"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["pin_front"], "radiusOuter", "0.004", "float");
            SetParam(nodes["pin_front"], "radiusInner", "0", "float");
            SetParam(nodes["pin_front"], "height", "0.17", "float");
            SetParam(nodes["pin_front"], "columns", "8", "int");
            SetParam(nodes["pin_front"], "endCaps", "true", "bool");

            nodes["pin_front_xform"] = data.AddNode("Transform", new Vector2(300, row));
            SetParam(nodes["pin_front_xform"], "translate", "0,0.22,0.22", "Vector3");
            AddEdge(data, nodes, "pin_front", "pin_front_xform");

            row += 180;
            nodes["pin_rear"] = data.AddNode("Tube", new Vector2(0, row));
            SetParam(nodes["pin_rear"], "radiusOuter", "0.004", "float");
            SetParam(nodes["pin_rear"], "radiusInner", "0", "float");
            SetParam(nodes["pin_rear"], "height", "0.17", "float");
            SetParam(nodes["pin_rear"], "columns", "8", "int");
            SetParam(nodes["pin_rear"], "endCaps", "true", "bool");

            nodes["pin_rear_xform"] = data.AddNode("Transform", new Vector2(300, row));
            SetParam(nodes["pin_rear_xform"], "translate", "0,0.22,0.02", "Vector3");
            AddEdge(data, nodes, "pin_rear", "pin_rear_xform");

            nodes["merge_controls"] = data.AddNode("Merge", new Vector2(600, ctrlBase + 450));
            string[] ctrlParts = {
                "slide_lock", "slide_lock_lever", "takedown_lever", "magazine_release"
            };
            AddMergeEdges(data, nodes, "merge_controls", new[] {
                "slide_lock_xform", "slide_lock_lever_xform", "takedown_lever_xform",
                "magazine_release_xform", "pin_front_xform", "pin_rear_xform"
            });

            // =====================================================
            //  Group 9: Slide Details (套筒细节) — 3 primitives
            // =====================================================
            float detailBase = 7700;
            row = detailBase;

            CreateBoxAndTransform(data, nodes, "slide_channel_left",
                0.01f, 0.02f, 0.40f, -0.08f, 0.32f, 0.12f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_channel_right",
                0.01f, 0.02f, 0.40f, 0.08f, 0.32f, 0.12f, 0, 0, 0, row);
            row += 180;
            CreateBoxAndTransform(data, nodes, "slide_text_area",
                0.005f, 0.03f, 0.10f, -0.083f, 0.36f, 0.05f, 0, 0, 0, row);

            nodes["merge_slide_details"] = data.AddNode("Merge", new Vector2(600, detailBase + 180));
            string[] detailParts = { "slide_channel_left", "slide_channel_right", "slide_text_area" };
            AddMergeEdges(data, nodes, "merge_slide_details",
                System.Array.ConvertAll(detailParts, p => p + "_xform"));

            // =====================================================
            //  Final Assembly
            // =====================================================
            float asmX = 900;
            float asmY = 3600;

            nodes["merge_all"] = data.AddNode("Merge", new Vector2(asmX, asmY));
            AddMergeEdges(data, nodes, "merge_all", new[] {
                "merge_slide", "merge_barrel", "merge_frame",
                "merge_trigger_guard", "merge_trigger", "merge_grip",
                "merge_magazine", "merge_controls", "merge_slide_details"
            });

            nodes["fuse"] = data.AddNode("Fuse", new Vector2(asmX + 250, asmY));
            SetParam(nodes["fuse"], "distance", "0.001", "float");
            AddEdge(data, nodes, "merge_all", "fuse");

            nodes["normals"] = data.AddNode("Normal", new Vector2(asmX + 500, asmY));
            SetParam(nodes["normals"], "type", "point", "string");
            SetParam(nodes["normals"], "cuspAngle", "60", "float");
            SetParam(nodes["normals"], "weightByArea", "true", "bool");
            AddEdge(data, nodes, "fuse", "normals");

            nodes["uv_project"] = data.AddNode("UVProject", new Vector2(asmX + 750, asmY));
            SetParam(nodes["uv_project"], "projectionType", "cubic", "string");
            AddEdge(data, nodes, "normals", "uv_project");

            nodes["save_prefab"] = data.AddNode("SavePrefab", new Vector2(asmX + 1000, asmY));
            SetParam(nodes["save_prefab"], "assetPath", "Assets/PCGOutput/Glock.prefab", "string");
            SetParam(nodes["save_prefab"], "prefabName", "Glock", "string");
            SetParam(nodes["save_prefab"], "addCollider", "true", "bool");
            SetParam(nodes["save_prefab"], "convexCollider", "true", "bool");
            AddEdge(data, nodes, "uv_project", "save_prefab");

            // ---- Groups ----
            AddGroup(data, nodes, "Slide (套筒)", slideParts,
                new Vector2(-20, -40), new Vector2(700, 1700));
            // Fix: Barrel Group includes Tube nodes
            AddGroup(data, nodes, "Barrel (枪管)",
                new[] { "barrel_outer", "barrel_bore" },
                new Vector2(-20, barrelBase - 40), new Vector2(700, 420));
            AddGroup(data, nodes, "Frame (机匣)", frameParts,
                new Vector2(-20, frameBase - 40), new Vector2(700, 960));
            AddGroup(data, nodes, "Trigger Guard (扳机护圈)", tgParts,
                new Vector2(-20, tgBase - 40), new Vector2(700, 780));
            // Fix: Trigger Group includes trigger_pivot (Tube)
            AddGroup(data, nodes, "Trigger (扳机)",
                new[] { "trigger_body", "trigger_face", "trigger_pivot" },
                new Vector2(-20, trigBase - 40), new Vector2(700, 780));
            AddGroup(data, nodes, "Grip (握把)", gripParts,
                new Vector2(-20, gripBase - 40), new Vector2(700, 1140));
            AddGroup(data, nodes, "Magazine (弹匣)", magParts,
                new Vector2(-20, magBase - 40), new Vector2(700, 600));
            // Fix: Controls Group includes pin_front and pin_rear (Tube)
            AddGroup(data, nodes, "Controls (操控部件)",
                new[] { "slide_lock", "slide_lock_lever", "takedown_lever", "magazine_release", "pin_front", "pin_rear" },
                new Vector2(-20, ctrlBase - 40), new Vector2(700, 1320));
            AddGroup(data, nodes, "Slide Details (套筒细节)", detailParts,
                new Vector2(-20, detailBase - 40), new Vector2(700, 600));

            // ---- Sticky Note ----
            data.StickyNotes.Add(new PCGStickyNoteData
            {
                NoteId = System.Guid.NewGuid().ToString(),
                Title = "Procedural Glock",
                Content = "Glock 手枪程序化白模\n\n35 个基础多面体 (Box+Tube)\n9 个部件组\n\n结构：\n- Slide (套筒) 9 件\n- Barrel (枪管) 2 件\n- Frame (机匣) 5 件\n- Trigger Guard 4 件\n- Trigger 3 件\n- Grip (握把) 6 件\n- Magazine 3 件\n- Controls 6 件\n- Slide Details 3 件\n\n使用方法：\n1. 菜单 PCG Toolkit > Examples > Create Glock Graph\n2. 在 Node Editor 中打开生成的 asset\n3. 执行图生成 Prefab",
                Position = new Vector2(-350, -100),
                Size = new Vector2(300, 500)
            });

            // ---- Save Asset ----
            string dir = "Assets/PCGToolkit/Examples";
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/PCGToolkit", "Examples");

            string path = $"{dir}/ProceduralGlock.asset";
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = data;
            Debug.Log($"[GlockExampleBuilder] Created Glock graph at {path} ({data.Nodes.Count} nodes, {data.Edges.Count} edges)");
        }

        // Helper: Create a Box node + Transform node pair
        private static void CreateBoxAndTransform(
            PCGGraphData data, Dictionary<string, PCGNodeData> nodes, string id,
            float sizeX, float sizeY, float sizeZ,
            float tx, float ty, float tz,
            float rx, float ry, float rz,
            float layoutY)
        {
            nodes[id] = data.AddNode("Box", new Vector2(0, layoutY));
            SetParam(nodes[id], "sizeX", sizeX.ToString(IC), "float");
            SetParam(nodes[id], "sizeY", sizeY.ToString(IC), "float");
            SetParam(nodes[id], "sizeZ", sizeZ.ToString(IC), "float");

            string xformId = id + "_xform";
            nodes[xformId] = data.AddNode("Transform", new Vector2(300, layoutY));
            SetParam(nodes[xformId], "translate",
                $"{tx.ToString(IC)},{ty.ToString(IC)},{tz.ToString(IC)}", "Vector3");
            if (rx != 0 || ry != 0 || rz != 0)
            {
                SetParam(nodes[xformId], "rotate",
                    $"{rx.ToString(IC)},{ry.ToString(IC)},{rz.ToString(IC)}", "Vector3");
            }
            AddEdge(data, nodes, id, xformId);
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

        /// <summary>
        /// 将多个源节点按顺序连接到 Merge 节点的 input0, input1, ... 端口
        /// </summary>
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
            }

            data.Groups.Add(new PCGGroupData
            {
                GroupId = System.Guid.NewGuid().ToString(),
                Title = title,
                NodeIds = nodeIds,
                Position = position,
                Size = size
            });
        }
    }
}
