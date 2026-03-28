## 一、蟹形地雷机器人结构分析

从概念图中可以识别出这是一个"Crab Mine"（蟹形地雷），具有折叠（Closed Mode）和展开行走（Open/Running Mode）两种形态。以下是其核心组成部件：

| 部件 | 形状描述 |
|------|----------|
| 主体盘 | 扁平圆柱体，直径远大于高度，类似地雷外壳 |
| 顶部穹顶 | 略微隆起的锥台/圆柱，带分段装甲板 |
| 装甲裙板 | 环绕主体的分段面板（约8-10段），带倒角边缘 |
| 顶部提手 | 小型长方体把手 |
| 背部圆筒 | 水平放置的圆柱体（电池/燃料罐） |
| 弹簧连接 | 圆筒与主体之间的螺旋弹簧 |
| 腿部×6 | 每条腿3段：大腿（粗方柱）、小腿（细方柱）、足爪（楔形方块） |
| 关节×12 | 腿部各段之间的球形关节 |
| 传感器/指示灯 | 小球体/小圆柱，分布在主体表面 |

---

## 二、基础多面体拆分 JSON

```json
{
  "model_name": "CrabMine_Robot",
  "description": "蟹形地雷机器人 - 基础多面体拆分",
  "unit": "meter",
  "reference_scale": "主体盘直径约1.0m",
  "parts": [
    {
      "id": "body_main_disc",
      "shape": "cylinder",
      "description": "主体盘 - 扁平圆柱",
      "position": [0, 0.08, 0],
      "rotation": [0, 0, 0],
      "size": { "radiusOuter": 0.5, "height": 0.12 },
      "segments": 24
    },
    {
      "id": "body_top_dome",
      "shape": "cylinder_tapered",
      "description": "顶部穹顶 - 略收窄的圆柱",
      "position": [0, 0.18, 0],
      "rotation": [0, 0, 0],
      "size": { "radiusBottom": 0.42, "radiusTop": 0.35, "height": 0.08 },
      "segments": 24
    },
    {
      "id": "body_bottom_plate",
      "shape": "cylinder",
      "description": "底部底板 - 薄圆柱",
      "position": [0, 0.0, 0],
      "rotation": [0, 0, 0],
      "size": { "radiusOuter": 0.48, "height": 0.03 },
      "segments": 24
    },
    {
      "id": "armor_panel_template",
      "shape": "box",
      "description": "装甲裙板模板（径向阵列×8）",
      "position": [0.46, 0.08, 0],
      "rotation": [0, 0, 0],
      "size": { "sizeX": 0.06, "sizeY": 0.10, "sizeZ": 0.35 },
      "array": { "mode": "radial", "count": 8, "axis": [0,1,0] }
    },
    {
      "id": "top_handle",
      "shape": "box",
      "description": "顶部提手",
      "position": [0, 0.26, 0],
      "rotation": [0, 0, 0],
      "size": { "sizeX": 0.15, "sizeY": 0.03, "sizeZ": 0.04 }
    },
    {
      "id": "back_canister",
      "shape": "cylinder",
      "description": "背部圆筒（燃料/电池罐）",
      "position": [0, 0.22, -0.35],
      "rotation": [0, 0, 90],
      "size": { "radiusOuter": 0.06, "height": 0.28 },
      "segments": 16
    },
    {
      "id": "canister_cap_a",
      "shape": "sphere",
      "description": "圆筒端盖A",
      "position": [0.14, 0.22, -0.35],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.06 }
    },
    {
      "id": "canister_cap_b",
      "shape": "sphere",
      "description": "圆筒端盖B",
      "position": [-0.14, 0.22, -0.35],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.06 }
    },
    {
      "id": "antenna_base",
      "shape": "cylinder",
      "description": "天线底座",
      "position": [0.05, 0.22, 0.1],
      "rotation": [0, 0, 0],
      "size": { "radiusOuter": 0.015, "height": 0.02 },
      "segments": 8
    },
    {
      "id": "antenna_rod",
      "shape": "cylinder",
      "description": "天线杆",
      "position": [0.05, 0.35, 0.1],
      "rotation": [0, 0, 0],
      "size": { "radiusOuter": 0.004, "height": 0.25 },
      "segments": 6
    },
    {
      "id": "indicator_light_01",
      "shape": "sphere",
      "description": "警告指示灯（三角标志旁）",
      "position": [0.15, 0.15, 0.38],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.015 }
    },
    {
      "id": "indicator_light_02",
      "shape": "sphere",
      "description": "红色状态灯",
      "position": [-0.2, 0.10, 0.40],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.012 }
    },
    {
      "id": "leg_mount_joint",
      "shape": "sphere",
      "description": "腿部安装关节球（×6，径向分布）",
      "position": [0.44, 0.04, 0],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.04 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "leg_upper",
      "shape": "box",
      "description": "大腿段（×6）",
      "position": [0.58, -0.02, 0],
      "rotation": [0, 0, -30],
      "size": { "sizeX": 0.22, "sizeY": 0.06, "sizeZ": 0.07 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "leg_elbow_joint",
      "shape": "sphere",
      "description": "肘关节球（×6）",
      "position": [0.72, -0.10, 0],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.035 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "leg_lower",
      "shape": "box",
      "description": "小腿段（×6）",
      "position": [0.82, -0.22, 0],
      "rotation": [0, 0, -60],
      "size": { "sizeX": 0.20, "sizeY": 0.05, "sizeZ": 0.06 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "leg_ankle_joint",
      "shape": "sphere",
      "description": "踝关节球（×6）",
      "position": [0.88, -0.35, 0],
      "rotation": [0, 0, 0],
      "size": { "radius": 0.03 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "leg_foot",
      "shape": "box",
      "description": "足爪（×6）- 楔形",
      "position": [0.92, -0.40, 0],
      "rotation": [0, 0, -15],
      "size": { "sizeX": 0.12, "sizeY": 0.04, "sizeZ": 0.08 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    },
    {
      "id": "foot_claw_tip",
      "shape": "box",
      "description": "爪尖（×6）- 用布尔切割成楔形",
      "position": [0.99, -0.42, 0],
      "rotation": [0, 0, -30],
      "size": { "sizeX": 0.06, "sizeY": 0.025, "sizeZ": 0.06 },
      "array": { "mode": "radial", "count": 6, "axis": [0,1,0] }
    }
  ]
}
```

---

## 三、PCG Graph JSON（AI Agent 可执行格式）

以下 JSON 严格遵循 `PCGGraphData` 的序列化格式（`PCGNodeData` + `PCGEdgeData`），使用了项目中实际存在的节点类型和参数名。 [0-cite-0](#0-cite-0)

```json
{
  "Version": 7,
  "GraphName": "CrabMine_ProceduralRobot",
  "Nodes": [
    {
      "NodeId": "node_body_disc",
      "NodeType": "Tube",
      "Position": [0, 0],
      "Parameters": [
        { "Key": "radiusOuter", "ValueJson": "0.5", "ValueType": "System.Single" },
        { "Key": "radiusInner", "ValueJson": "0", "ValueType": "System.Single" },
        { "Key": "height", "ValueJson": "0.12", "ValueType": "System.Single" },
        { "Key": "columns", "ValueJson": "24", "ValueType": "System.Int32" },
        { "Key": "endCaps", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_body_disc_bevel",
      "NodeType": "PolyBevel",
      "Position": [250, 0],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.015", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "2", "ValueType": "System.Int32" },
        { "Key": "group", "ValueJson": "", "ValueType": "System.String" }
      ]
    },
    {
      "NodeId": "node_top_dome",
      "NodeType": "Tube",
      "Position": [0, 200],
      "Parameters": [
        { "Key": "radiusOuter", "ValueJson": "0.42", "ValueType": "System.Single" },
        { "Key": "radiusInner", "ValueJson": "0", "ValueType": "System.Single" },
        { "Key": "height", "ValueJson": "0.08", "ValueType": "System.Single" },
        { "Key": "columns", "ValueJson": "24", "ValueType": "System.Int32" },
        { "Key": "endCaps", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_top_dome_transform",
      "NodeType": "Transform",
      "Position": [250, 200],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0,\"y\":0.10,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "uniformScale", "ValueJson": "1", "ValueType": "System.Single" }
      ]
    },
    {
      "NodeId": "node_top_dome_bevel",
      "NodeType": "PolyBevel",
      "Position": [500, 200],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.01", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "1", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_top_dome_inset",
      "NodeType": "Inset",
      "Position": [750, 200],
      "Parameters": [
        { "Key": "distance", "ValueJson": "0.02", "ValueType": "System.Single" },
        { "Key": "group", "ValueJson": "", "ValueType": "System.String" },
        { "Key": "outputInner", "ValueJson": "true", "ValueType": "System.Boolean" },
        { "Key": "outputSide", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_bottom_plate",
      "NodeType": "Tube",
      "Position": [0, 400],
      "Parameters": [
        { "Key": "radiusOuter", "ValueJson": "0.48", "ValueType": "System.Single" },
        { "Key": "radiusInner", "ValueJson": "0", "ValueType": "System.Single" },
        { "Key": "height", "ValueJson": "0.03", "ValueType": "System.Single" },
        { "Key": "columns", "ValueJson": "24", "ValueType": "System.Int32" },
        { "Key": "endCaps", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_bottom_plate_transform",
      "NodeType": "Transform",
      "Position": [250, 400],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0,\"y\":-0.06,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_armor_panel",
      "NodeType": "Box",
      "Position": [0, 600],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.06", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.10", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.35", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0.46,\"y\":0.08,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_armor_panel_bevel",
      "NodeType": "PolyBevel",
      "Position": [250, 600],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.008", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "2", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_armor_panel_inset",
      "NodeType": "Inset",
      "Position": [500, 600],
      "Parameters": [
        { "Key": "distance", "ValueJson": "0.01", "ValueType": "System.Single" },
        { "Key": "outputInner", "ValueJson": "true", "ValueType": "System.Boolean" },
        { "Key": "outputSide", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_armor_panel_array",
      "NodeType": "Array",
      "Position": [750, 600],
      "Parameters": [
        { "Key": "mode", "ValueJson": "radial", "ValueType": "System.String" },
        { "Key": "count", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "axis", "ValueJson": "{\"x\":0,\"y\":1,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "fullAngle", "ValueJson": "360", "ValueType": "System.Single" }
      ]
    },
    {
      "NodeId": "node_handle",
      "NodeType": "Box",
      "Position": [0, 800],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.15", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.03", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.04", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0.26,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_handle_bevel",
      "NodeType": "PolyBevel",
      "Position": [250, 800],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.005", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "2", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_canister",
      "NodeType": "Tube",
      "Position": [0, 1000],
      "Parameters": [
        { "Key": "radiusOuter", "ValueJson": "0.06", "ValueType": "System.Single" },
        { "Key": "radiusInner", "ValueJson": "0", "ValueType": "System.Single" },
        { "Key": "height", "ValueJson": "0.28", "ValueType": "System.Single" },
        { "Key": "columns", "ValueJson": "16", "ValueType": "System.Int32" },
        { "Key": "endCaps", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_canister_transform",
      "NodeType": "Transform",
      "Position": [250, 1000],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0,\"y\":0.22,\"z\":-0.35}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":90}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_canister_cap_sphere",
      "NodeType": "Sphere",
      "Position": [0, 1150],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.06", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "16", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_canister_cap_clip",
      "NodeType": "Clip",
      "Position": [250, 1150],
      "Parameters": [
        { "Key": "origin", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "normal", "ValueJson": "{\"x\":1,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "keepAbove", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_canister_cap_transform_a",
      "NodeType": "Transform",
      "Position": [500, 1100],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0.14,\"y\":0.22,\"z\":-0.35}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_canister_cap_mirror",
      "NodeType": "Mirror",
      "Position": [750, 1100],
      "Parameters": [
        { "Key": "origin", "ValueJson": "{\"x\":0,\"y\":0.22,\"z\":-0.35}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "normal", "ValueJson": "{\"x\":1,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "keepOriginal", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_antenna_rod",
      "NodeType": "Tube",
      "Position": [0, 1350],
      "Parameters": [
        { "Key": "radiusOuter", "ValueJson": "0.004", "ValueType": "System.Single" },
        { "Key": "radiusInner", "ValueJson": "0", "ValueType": "System.Single" },
        { "Key": "height", "ValueJson": "0.25", "ValueType": "System.Single" },
        { "Key": "columns", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "endCaps", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_antenna_transform",
      "NodeType": "Transform",
      "Position": [250, 1350],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0.05,\"y\":0.35,\"z\":0.1}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_antenna_tip",
      "NodeType": "Sphere",
      "Position": [0, 1450],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.008", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":0.05,\"y\":0.48,\"z\":0.1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_merge_body",
      "NodeType": "Merge",
      "Position": [1000, 400],
      "Parameters": []
    },
    {
      "NodeId": "node_leg_upper",
      "NodeType": "Box",
      "Position": [0, 1700],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.22", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.06", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.07", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_leg_upper_bevel",
      "NodeType": "PolyBevel",
      "Position": [250, 1700],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.008", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "2", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_leg_upper_extrude",
      "NodeType": "Extrude",
      "Position": [500, 1700],
      "Parameters": [
        { "Key": "distance", "ValueJson": "0.005", "ValueType": "System.Single" },
        { "Key": "inset", "ValueJson": "0.01", "ValueType": "System.Single" },
        { "Key": "individual", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_leg_mount_joint",
      "NodeType": "Sphere",
      "Position": [0, 1850],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.04", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "12", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":-0.13,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_leg_elbow_joint",
      "NodeType": "Sphere",
      "Position": [0, 2000],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.035", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "12", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":0.13,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_leg_lower",
      "NodeType": "Box",
      "Position": [0, 2150],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.20", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.05", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.06", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_leg_lower_bevel",
      "NodeType": "PolyBevel",
      "Position": [250, 2150],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.006", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "2", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_leg_lower_extrude",
      "NodeType": "Extrude",
      "Position": [500, 2150],
      "Parameters": [
        { "Key": "distance", "ValueJson": "0.004", "ValueType": "System.Single" },
        { "Key": "inset", "ValueJson": "0.008", "ValueType": "System.Single" },
        { "Key": "individual", "ValueJson": "true", "ValueType": "System.Boolean" }
      ]
    },
    {
      "NodeId": "node_leg_ankle_joint",
      "NodeType": "Sphere",
      "Position": [0, 2300],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.03", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "10", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":0.12,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_leg_foot_base",
      "NodeType": "Box",
      "Position": [0, 2450],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.12", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.04", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.08", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_foot_claw_cutter",
      "NodeType": "Box",
      "Position": [0, 2550],
      "Parameters": [
        { "Key": "sizeX", "ValueJson": "0.10", "ValueType": "System.Single" },
        { "Key": "sizeY", "ValueJson": "0.08", "ValueType": "System.Single" },
        { "Key": "sizeZ", "ValueJson": "0.10", "ValueType": "System.Single" },
        { "Key": "center", "ValueJson": "{\"x\":0.04,\"y\":0.03,\"z\":0}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_foot_claw_cutter_transform",
      "NodeType": "Transform",
      "Position": [250, 2550],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":-25}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_foot_boolean_cut",
      "NodeType": "Boolean",
      "Position": [500, 2500],
      "Parameters": [
        { "Key": "operation", "ValueJson": "difference", "ValueType": "System.String" }
      ]
    },
    {
      "NodeId": "node_foot_bevel",
      "NodeType": "PolyBevel",
      "Position": [750, 2500],
      "Parameters": [
        { "Key": "offset", "ValueJson": "0.004", "ValueType": "System.Single" },
        { "Key": "divisions", "ValueJson": "1", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_merge_upper_leg_segment",
      "NodeType": "Merge",
      "Position": [750, 1800],
      "Parameters": []
    },
    {
      "NodeId": "node_leg_upper_transform",
      "NodeType": "Transform",
      "Position": [1000, 1800],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0.55,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":-30}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_merge_lower_leg_segment",
      "NodeType": "Merge",
      "Position": [750, 2250],
      "Parameters": []
    },
    {
      "NodeId": "node_leg_lower_transform",
      "NodeType": "Transform",
      "Position": [1000, 2250],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0.75,\"y\":-0.12,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":-60}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_foot_transform",
      "NodeType": "Transform",
      "Position": [1000, 2500],
      "Parameters": [
        { "Key": "translate", "ValueJson": "{\"x\":0.88,\"y\":-0.35,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "rotate", "ValueJson": "{\"x\":0,\"y\":0,\"z\":-15}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "scale", "ValueJson": "{\"x\":1,\"y\":1,\"z\":1}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_merge_single_leg",
      "NodeType": "Merge",
      "Position": [1250, 2100],
      "Parameters": []
    },
    {
      "NodeId": "node_leg_radial_array",
      "NodeType": "Array",
      "Position": [1500, 2100],
      "Parameters": [
        { "Key": "mode", "ValueJson": "radial", "ValueType": "System.String" },
        { "Key": "count", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "axis", "ValueJson": "{\"x\":0,\"y\":1,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "center", "ValueJson": "{\"x\":0,\"y\":0,\"z\":0}", "ValueType": "UnityEngine.Vector3" },
        { "Key": "fullAngle", "ValueJson": "360", "ValueType": "System.Single" }
      ]
    },
    {
      "NodeId": "node_indicator_light_01",
      "NodeType": "Sphere",
      "Position": [0, 1550],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.015", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":0.15,\"y\":0.15,\"z\":0.38}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_indicator_light_02",
      "NodeType": "Sphere",
      "Position": [250, 1550],
      "Parameters": [
        { "Key": "radius", "ValueJson": "0.012", "ValueType": "System.Single" },
        { "Key": "rows", "ValueJson": "6", "ValueType": "System.Int32" },
        { "Key": "columns", "ValueJson": "8", "ValueType": "System.Int32" },
        { "Key": "center", "ValueJson": "{\"x\":-0.2,\"y\":0.10,\"z\":0.40}", "ValueType": "UnityEngine.Vector3" }
      ]
    },
    {
      "NodeId": "node_body_subdivide",
      "NodeType": "Subdivide",
      "Position": [1250, 400],
      "Parameters": [
        { "Key": "iterations", "ValueJson": "1", "ValueType": "System.Int32" }
      ]
    },
    {
      "NodeId": "node_merge_final",
      "NodeType": "Merge",
      "Position": [1750, 1200],
      "Parameters": []
    },
    {
      "NodeId": "node_output",
      "NodeType": "Output",
      "Position": [2000, 1200],
      "Parameters": []
    }
  ],
  "Edges": [
    {
      "EdgeId": "e001",
      "OutputNodeId": "node_body_disc",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_body_disc_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e002",
      "OutputNodeId": "node_top_dome",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_top_dome_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e003",
      "OutputNodeId": "node_top_dome_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_top_dome_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e004",
      "OutputNodeId": "node_top_dome_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_top_dome_inset",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e005",
      "OutputNodeId": "node_bottom_plate",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_bottom_plate_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e006",
      "OutputNodeId": "node_armor_panel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_armor_panel_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e007",
      "OutputNodeId": "node_armor_panel_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_armor_panel_inset",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e008",
      "OutputNodeId": "node_armor_panel_inset",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_armor_panel_array",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e009",
      "OutputNodeId": "node_handle",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_handle_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e010",
      "OutputNodeId": "node_canister",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_canister_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e011",
      "OutputNodeId": "node_canister_cap_sphere",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_canister_cap_clip",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e012",
      "OutputNodeId": "node_canister_cap_clip",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_canister_cap_transform_a",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e013",
      "OutputNodeId": "node_canister_cap_transform_a",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_canister_cap_mirror",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e014",
      "OutputNodeId": "node_antenna_rod",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_antenna_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e015",
      "OutputNodeId": "node_body_disc_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_0"
    },
    {
      "EdgeId": "e016",
      "OutputNodeId": "node_top_dome_inset",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_1"
    },
    {
      "EdgeId": "e017",
      "OutputNodeId": "node_bottom_plate_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_2"
    },
    {
      "EdgeId": "e018",
      "OutputNodeId": "node_armor_panel_array",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_3"
    },
    {
      "EdgeId": "e019",
      "OutputNodeId": "node_handle_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_4"
    },
    {
      "EdgeId": "e020",
      "OutputNodeId": "node_canister_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_5"
    },
    {
      "EdgeId": "e021",
      "OutputNodeId": "node_canister_cap_mirror",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_6"
    },
    {
      "EdgeId": "e022",
      "OutputNodeId": "node_antenna_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_7"
    },
    {
      "EdgeId": "e023",
      "OutputNodeId": "node_antenna_tip",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_8"
    },
    {
      "EdgeId": "e024",
      "OutputNodeId": "node_indicator_light_01",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_9"
    },
    {
      "EdgeId": "e025",
      "OutputNodeId": "node_indicator_light_02",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_body",
      "InputPortName": "meshIn_10"
    },
    {
      "EdgeId": "e026",
      "OutputNodeId": "node_merge_body",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_body_subdivide",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e030",
      "OutputNodeId": "node_leg_upper",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_upper_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e031",
      "OutputNodeId": "node_leg_upper_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_upper_extrude",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e032",
      "OutputNodeId": "node_leg_upper_extrude",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_upper_leg_segment",
      "InputPortName": "meshIn_0"
    },
    {
      "EdgeId": "e033",
      "OutputNodeId": "node_leg_mount_joint",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_upper_leg_segment",
      "InputPortName": "meshIn_1"
    },
    {
      "EdgeId": "e034",
      "OutputNodeId": "node_leg_elbow_joint",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_upper_leg_segment",
      "InputPortName": "meshIn_2"
    },
    {
      "EdgeId": "e035",
      "OutputNodeId": "node_merge_upper_leg_segment",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_upper_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e040",
      "OutputNodeId": "node_leg_lower",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_lower_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e041",
      "OutputNodeId": "node_leg_lower_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_lower_extrude",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e042",
      "OutputNodeId": "node_leg_lower_extrude",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_lower_leg_segment",
      "InputPortName": "meshIn_0"
    },
    {
      "EdgeId": "e043",
      "OutputNodeId": "node_leg_ankle_joint",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_lower_leg_segment",
      "InputPortName": "meshIn_1"
    },
    {
      "EdgeId": "e044",
      "OutputNodeId": "node_merge_lower_leg_segment",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_lower_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e050",
      "OutputNodeId": "node_leg_foot_base",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_foot_boolean_cut",
      "InputPortName": "meshA"
    },
    {
      "EdgeId": "e051",
      "OutputNodeId": "node_foot_claw_cutter",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_foot_claw_cutter_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e052",
      "OutputNodeId": "node_foot_claw_cutter_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_foot_boolean_cut",
      "InputPortName": "meshB"
    },
    {
      "EdgeId": "e053",
      "OutputNodeId": "node_foot_boolean_cut",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_foot_bevel",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e054",
      "OutputNodeId": "node_foot_bevel",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_foot_transform",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e060",
      "OutputNodeId": "node_leg_upper_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_single_leg",
      "InputPortName": "meshIn_0"
    },
    {
      "EdgeId": "e061",
      "OutputNodeId": "node_leg_lower_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_single_leg",
      "InputPortName": "meshIn_1"
    },
    {
      "EdgeId": "e062",
      "OutputNodeId": "node_foot_transform",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_single_leg",
      "InputPortName": "meshIn_2"
    },
    {
      "EdgeId": "e063",
      "OutputNodeId": "node_merge_single_leg",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_leg_radial_array",
      "InputPortName": "meshIn"
    },
    {
      "EdgeId": "e070",
      "OutputNodeId": "node_body_subdivide",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_final",
      "InputPortName": "meshIn_0"
    },
    {
      "EdgeId": "e071",
      "OutputNodeId": "node_leg_radial_array",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_merge_final",
      "InputPortName": "meshIn_1"
    },
    {
      "EdgeId": "e080",
      "OutputNodeId": "node_merge_final",
      "OutputPortName": "meshOut",
      "InputNodeId": "node_output",
      "InputPortName": "meshIn"
    }
  ],
  "Groups": [
    {
      "GroupId": "group_body",
      "Title": "Body Assembly",
      "NodeIds": [
        "node_body_disc", "node_body_disc_bevel",
        "node_top_dome", "node_top_dome_transform", "node_top_dome_bevel", "node_top_dome_inset",
        "node_bottom_plate", "node_bottom_plate_transform",
        "node_armor_panel", "node_armor_panel_bevel", "node_armor_panel_inset", "node_armor_panel_array",
        "node_handle", "node_handle_bevel",
        "node_canister", "node_canister_transform",
        "node_canister_cap_sphere", "node_canister_cap_clip", "node_canister_cap_transform_a", "node_canister_cap_mirror",
        "node_antenna_rod", "node_antenna_transform", "node_antenna_tip",
        "node_indicator_light_01", "node_indicator_light_02",
        "node_merge_body", "node_body_subdivide"
      ]
    },
    {
      "GroupId": "group_leg",
      "Title": "Single Leg Assembly",
      "NodeIds": [
        "node_leg_upper", "node_leg_upper_bevel", "node_leg_upper_extrude",
        "node_leg_mount_joint", "node_leg_elbow_joint",
        "node_merge_upper_leg_segment", "node_leg_upper_transform",
        "node_leg_lower", "node_leg_lower_bevel", "node_leg_lower_extrude",
        "node_leg_ankle_joint",
        "node_merge_lower_leg_segment", "node_leg_lower_transform",
        "node_leg_foot_base", "node_foot_claw_cutter", "node_foot_claw_cutter_transform",
        "node_foot_boolean_cut", "node_foot_bevel", "node_foot_transform",
        "node_merge_single_leg", "node_leg_radial_array"
      ]
    },
    {
      "GroupId": "group_final",
      "Title": "Final Output",
      "NodeIds": ["node_merge_final", "node_output"]
    }
  ],
  "StickyNotes": [
    {
      "NoteId": "note_01",
      "Title": "Crab Mine Robot - PCG Procedural Model",
      "Content": "主体盘使用Tube(radiusInner=0)生成实心圆柱，PolyBevel倒角边缘。\n装甲裙板通过Box→Bevel→Inset→Array(radial×8)生成。\n圆筒端盖使用Sphere→Clip(半球)→Mirror实现对称。\n足爪使用Boolean(difference)切割出楔形。\n单腿组装后通过Array(radial×6)生成全部6条腿。",
      "Position": [-300, -100]
    }
  ],
  "ExposedParameters": [
    {
      "ParamId": "param_body_radius",
      "Name": "BodyRadius",
      "ValueJson": "0.5",
      "ValueType": "System.Single",
      "LinkedNodeId": "node_body_disc",
      "LinkedParamKey": "radiusOuter"
    },
    {
      "ParamId": "param_leg_count",
      "Name": "LegCount",
      "ValueJson": "6",
      "ValueType": "System.Int32",
      "LinkedNodeId": "node_leg_radial_array",
      "LinkedParamKey": "count"
    },
    {
      "ParamId": "param_armor_panel_count",
      "Name": "ArmorPanelCount",
      "ValueJson": "8",
      "ValueType": "System.Int32",
      "LinkedNodeId": "node_armor_panel_array",
      "LinkedParamKey": "count"
    },
    {
      "ParamId": "param_bevel_offset",
      "Name": "GlobalBevelOffset",
      "ValueJson": "0.015",
      "ValueType": "System.Single",
      "LinkedNodeId": "node_body_disc_bevel",
      "LinkedParamKey": "offset"
    }
  ]
}
```