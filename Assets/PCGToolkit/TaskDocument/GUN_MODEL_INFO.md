基于图片中的 Glock 手枪，我将其拆分为 35 个简单多面体（Box 为主，少量 Tube），并使用你的 PCG Toolkit 的节点格式输出 JSON。坐标系约定：X=左右（枪厚度），Y=上下，Z=前后（枪口朝 Z+）。整体归一化到约 1.0 单位长度。

以下 JSON 可直接用于 PCG Toolkit 的 Graph 构建 API（`add_node` + `connect_nodes` + `set_param`）： [0-cite-0](#0-cite-0) [0-cite-1](#0-cite-1) [0-cite-2](#0-cite-2) [0-cite-3](#0-cite-3)

```json
{
  "plan_title": "Glock 手枪程序化建模 — 多面体分解",
  "repository": "No78Vino/pcg_for_unity",
  "coordinate_system": {
    "X": "左右（枪身厚度方向）",
    "Y": "上下（高度方向）",
    "Z": "前后（枪口朝 Z+ 方向）",
    "unit": "归一化，整枪长度约 1.0"
  },
  "overall_dimensions": {
    "length_Z": 1.0,
    "height_Y": 0.85,
    "width_X": 0.18
  },

  "parts": [
    {
      "group": "slide",
      "description": "套筒（上方金属滑动部分）",
      "primitives": [
        {
          "id": "slide_main",
          "label": "套筒主体",
          "node_type": "Box",
          "parameters": { "sizeX": 0.16, "sizeY": 0.14, "sizeZ": 0.62 },
          "transform": { "translate": [0, 0.36, 0.12] },
          "description": "套筒的主要矩形体"
        },
        {
          "id": "slide_top_flat",
          "label": "套筒顶面平台",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.02, "sizeZ": 0.62 },
          "transform": { "translate": [0, 0.44, 0.12] },
          "description": "套筒顶部略窄的平面"
        },
        {
          "id": "slide_front_bevel",
          "label": "套筒前端倒角",
          "node_type": "Box",
          "parameters": { "sizeX": 0.14, "sizeY": 0.12, "sizeZ": 0.03 },
          "transform": { "translate": [0, 0.35, 0.44], "rotate": [5, 0, 0] },
          "description": "套筒前端微微向下倾斜的倒角面"
        },
        {
          "id": "slide_rear_serrations",
          "label": "套筒后部防滑纹区域",
          "node_type": "Box",
          "parameters": { "sizeX": 0.165, "sizeY": 0.13, "sizeZ": 0.10 },
          "transform": { "translate": [0, 0.36, -0.18] },
          "description": "套筒后部带防滑纹的加宽区域"
        },
        {
          "id": "ejection_port",
          "label": "抛壳窗",
          "node_type": "Box",
          "parameters": { "sizeX": 0.08, "sizeY": 0.06, "sizeZ": 0.12 },
          "transform": { "translate": [0.04, 0.38, 0.05] },
          "description": "套筒右侧的抛壳窗开口（Boolean Subtract 用）"
        },
        {
          "id": "front_sight",
          "label": "准星",
          "node_type": "Box",
          "parameters": { "sizeX": 0.04, "sizeY": 0.03, "sizeZ": 0.02 },
          "transform": { "translate": [0, 0.46, 0.38] },
          "description": "套筒前端顶部的准星凸起"
        },
        {
          "id": "rear_sight",
          "label": "照门",
          "node_type": "Box",
          "parameters": { "sizeX": 0.06, "sizeY": 0.035, "sizeZ": 0.025 },
          "transform": { "translate": [0, 0.46, -0.15] },
          "description": "套筒后端顶部的照门凸起"
        },
        {
          "id": "rear_sight_notch_left",
          "label": "照门左翼",
          "node_type": "Box",
          "parameters": { "sizeX": 0.015, "sizeY": 0.035, "sizeZ": 0.025 },
          "transform": { "translate": [-0.025, 0.46, -0.15] },
          "description": "照门U形缺口的左侧翼"
        },
        {
          "id": "rear_sight_notch_right",
          "label": "照门右翼",
          "node_type": "Box",
          "parameters": { "sizeX": 0.015, "sizeY": 0.035, "sizeZ": 0.025 },
          "transform": { "translate": [0.025, 0.46, -0.15] },
          "description": "照门U形缺口的右侧翼"
        }
      ]
    },

    {
      "group": "barrel",
      "description": "枪管（从套筒前端伸出）",
      "primitives": [
        {
          "id": "barrel_outer",
          "label": "枪管外壁",
          "node_type": "Tube",
          "parameters": { "radiusOuter": 0.04, "radiusInner": 0.0, "height": 0.15, "columns": 16, "endCaps": true },
          "transform": { "translate": [0, 0.33, 0.50], "rotate": [90, 0, 0] },
          "description": "从套筒前端伸出的枪管，沿Z轴方向"
        },
        {
          "id": "barrel_bore",
          "label": "枪管内膛",
          "node_type": "Tube",
          "parameters": { "radiusOuter": 0.025, "radiusInner": 0.0, "height": 0.16, "columns": 16, "endCaps": true },
          "transform": { "translate": [0, 0.33, 0.50], "rotate": [90, 0, 0] },
          "description": "枪管内膛（Boolean Subtract 用）"
        }
      ]
    },

    {
      "group": "frame",
      "description": "枪身/机匣（中间连接部分）",
      "primitives": [
        {
          "id": "frame_main",
          "label": "机匣主体",
          "node_type": "Box",
          "parameters": { "sizeX": 0.16, "sizeY": 0.10, "sizeZ": 0.52 },
          "transform": { "translate": [0, 0.24, 0.10] },
          "description": "机匣的主要矩形体，位于套筒下方"
        },
        {
          "id": "frame_front_extension",
          "label": "机匣前端延伸（导轨区域）",
          "node_type": "Box",
          "parameters": { "sizeX": 0.16, "sizeY": 0.08, "sizeZ": 0.12 },
          "transform": { "translate": [0, 0.22, 0.38] },
          "description": "机匣前端向前延伸的部分，包含附件导轨"
        },
        {
          "id": "accessory_rail_1",
          "label": "附件导轨槽1",
          "node_type": "Box",
          "parameters": { "sizeX": 0.17, "sizeY": 0.012, "sizeZ": 0.04 },
          "transform": { "translate": [0, 0.195, 0.36] },
          "description": "Picatinny导轨的第一条凸起"
        },
        {
          "id": "accessory_rail_2",
          "label": "附件导轨槽2",
          "node_type": "Box",
          "parameters": { "sizeX": 0.17, "sizeY": 0.012, "sizeZ": 0.04 },
          "transform": { "translate": [0, 0.175, 0.36] },
          "description": "Picatinny导轨的第二条凸起"
        },
        {
          "id": "frame_rear_tang",
          "label": "机匣后端海狸尾",
          "node_type": "Box",
          "parameters": { "sizeX": 0.15, "sizeY": 0.06, "sizeZ": 0.06 },
          "transform": { "translate": [0, 0.27, -0.20], "rotate": [15, 0, 0] },
          "description": "机匣后端向上翘起的海狸尾部分，与握把顶部衔接"
        }
      ]
    },

    {
      "group": "trigger_guard",
      "description": "扳机护圈",
      "primitives": [
        {
          "id": "trigger_guard_front",
          "label": "护圈前段",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.015, "sizeZ": 0.08 },
          "transform": { "translate": [0, 0.12, 0.22] },
          "description": "扳机护圈的前方水平段"
        },
        {
          "id": "trigger_guard_bottom",
          "label": "护圈底段",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.015, "sizeZ": 0.14 },
          "transform": { "translate": [0, 0.08, 0.15] },
          "description": "扳机护圈的底部水平段"
        },
        {
          "id": "trigger_guard_front_curve",
          "label": "护圈前弯角",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.04, "sizeZ": 0.015 },
          "transform": { "translate": [0, 0.10, 0.26], "rotate": [0, 0, 0] },
          "description": "护圈前端的垂直连接段"
        },
        {
          "id": "trigger_guard_rear",
          "label": "护圈后段",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.04, "sizeZ": 0.015 },
          "transform": { "translate": [0, 0.10, 0.08], "rotate": [15, 0, 0] },
          "description": "护圈后端的倾斜连接段，与握把前缘衔接"
        }
      ]
    },

    {
      "group": "trigger",
      "description": "扳机",
      "primitives": [
        {
          "id": "trigger_body",
          "label": "扳机主体",
          "node_type": "Box",
          "parameters": { "sizeX": 0.03, "sizeY": 0.06, "sizeZ": 0.015 },
          "transform": { "translate": [0, 0.14, 0.15], "rotate": [-10, 0, 0] },
          "description": "扳机的主体薄片"
        },
        {
          "id": "trigger_face",
          "label": "扳机面",
          "node_type": "Box",
          "parameters": { "sizeX": 0.025, "sizeY": 0.025, "sizeZ": 0.008 },
          "transform": { "translate": [0, 0.12, 0.155] },
          "description": "扳机上的安全扳机面（中间凸起）"
        },
        {
          "id": "trigger_pivot",
          "label": "扳机轴销",
          "node_type": "Tube",
          "parameters": { "radiusOuter": 0.005, "radiusInner": 0.0, "height": 0.16, "columns": 8, "endCaps": true },
          "transform": { "translate": [0, 0.17, 0.15] },
          "description": "扳机旋转轴的销钉"
        }
      ]
    },

    {
      "group": "grip",
      "description": "握把",
      "primitives": [
        {
          "id": "grip_main",
          "label": "握把主体",
          "node_type": "Box",
          "parameters": { "sizeX": 0.15, "sizeY": 0.32, "sizeZ": 0.10 },
          "transform": { "translate": [0, -0.02, -0.05], "rotate": [-18, 0, 0] },
          "description": "握把的主要矩形体，向后倾斜约18度"
        },
        {
          "id": "grip_front_strap",
          "label": "握把前缘",
          "node_type": "Box",
          "parameters": { "sizeX": 0.14, "sizeY": 0.28, "sizeZ": 0.02 },
          "transform": { "translate": [0, -0.01, 0.01], "rotate": [-15, 0, 0] },
          "description": "握把前缘的防滑纹理区域"
        },
        {
          "id": "grip_back_strap",
          "label": "握把后缘",
          "node_type": "Box",
          "parameters": { "sizeX": 0.14, "sizeY": 0.28, "sizeZ": 0.02 },
          "transform": { "translate": [0, -0.03, -0.10], "rotate": [-20, 0, 0] },
          "description": "握把后缘的弧形区域"
        },
        {
          "id": "grip_left_panel",
          "label": "握把左侧面板",
          "node_type": "Box",
          "parameters": { "sizeX": 0.015, "sizeY": 0.22, "sizeZ": 0.08 },
          "transform": { "translate": [-0.075, -0.02, -0.05], "rotate": [-18, 0, 0] },
          "description": "握把左侧的纹理面板"
        },
        {
          "id": "grip_right_panel",
          "label": "握把右侧面板",
          "node_type": "Box",
          "parameters": { "sizeX": 0.015, "sizeY": 0.22, "sizeZ": 0.08 },
          "transform": { "translate": [0.075, -0.02, -0.05], "rotate": [-18, 0, 0] },
          "description": "握把右侧的纹理面板"
        },
        {
          "id": "grip_bottom_swell",
          "label": "握把底部膨大",
          "node_type": "Box",
          "parameters": { "sizeX": 0.16, "sizeY": 0.04, "sizeZ": 0.11 },
          "transform": { "translate": [0, -0.17, -0.12], "rotate": [-18, 0, 0] },
          "description": "握把底部略微膨大的区域"
        }
      ]
    },

    {
      "group": "magazine",
      "description": "弹匣",
      "primitives": [
        {
          "id": "magazine_body",
          "label": "弹匣主体",
          "node_type": "Box",
          "parameters": { "sizeX": 0.12, "sizeY": 0.30, "sizeZ": 0.08 },
          "transform": { "translate": [0, -0.04, -0.05], "rotate": [-18, 0, 0] },
          "description": "弹匣的主体，嵌入握把内部"
        },
        {
          "id": "magazine_baseplate",
          "label": "弹匣底板",
          "node_type": "Box",
          "parameters": { "sizeX": 0.14, "sizeY": 0.025, "sizeZ": 0.10 },
          "transform": { "translate": [0, -0.22, -0.16], "rotate": [-18, 0, 0] },
          "description": "弹匣底部的加宽底板，略微突出握把底部"
        },
        {
          "id": "magazine_baseplate_lip",
          "label": "弹匣底板前唇",
          "node_type": "Box",
          "parameters": { "sizeX": 0.14, "sizeY": 0.015, "sizeZ": 0.015 },
          "transform": { "translate": [0, -0.23, -0.11], "rotate": [-18, 0, 0] },
          "description": "弹匣底板前端的小凸唇"
        }
      ]
    },

    {
      "group": "controls",
      "description": "操控部件（卡笋、释放按钮等）",
      "primitives": [
        {
          "id": "slide_lock",
          "label": "套筒卡笋",
          "node_type": "Box",
          "parameters": { "sizeX": 0.02, "sizeY": 0.025, "sizeZ": 0.04 },
          "transform": { "translate": [-0.09, 0.30, 0.10] },
          "description": "左侧的套筒卡笋杆"
        },
        {
          "id": "slide_lock_lever",
          "label": "套筒卡笋拨片",
          "node_type": "Box",
          "parameters": { "sizeX": 0.015, "sizeY": 0.015, "sizeZ": 0.025 },
          "transform": { "translate": [-0.095, 0.31, 0.08] },
          "description": "套筒卡笋的拨动部分"
        },
        {
          "id": "takedown_lever",
          "label": "分解杆",
          "node_type": "Box",
          "parameters": { "sizeX": 0.025, "sizeY": 0.02, "sizeZ": 0.03 },
          "transform": { "translate": [-0.09, 0.24, 0.18] },
          "description": "左侧的分解杆"
        },
        {
          "id": "magazine_release",
          "label": "弹匣释放按钮",
          "node_type": "Box",
          "parameters": { "sizeX": 0.02, "sizeY": 0.02, "sizeZ": 0.02 },
          "transform": { "translate": [-0.085, 0.20, 0.06] },
          "description": "左侧的弹匣释放按钮"
        },
        {
          "id": "pin_front",
          "label": "前销钉",
          "node_type": "Tube",
          "parameters": { "radiusOuter": 0.004, "radiusInner": 0.0, "height": 0.17, "columns": 8, "endCaps": true },
          "transform": { "translate": [0, 0.22, 0.22] },
          "description": "机匣前部的固定销钉"
        },
        {
          "id": "pin_rear",
          "label": "后销钉",
          "node_type": "Tube",
          "parameters": { "radiusOuter": 0.004, "radiusInner": 0.0, "height": 0.17, "columns": 8, "endCaps": true },
          "transform": { "translate": [0, 0.22, 0.02] },
          "description": "机匣后部的固定销钉"
        }
      ]
    },

    {
      "group": "slide_details",
      "description": "套筒细节",
      "primitives": [
        {
          "id": "slide_channel_left",
          "label": "套筒左侧凹槽",
          "node_type": "Box",
          "parameters": { "sizeX": 0.01, "sizeY": 0.02, "sizeZ": 0.40 },
          "transform": { "translate": [-0.08, 0.32, 0.12] },
          "description": "套筒左侧的纵向装饰凹槽"
        },
        {
          "id": "slide_channel_right",
          "label": "套筒右侧凹槽",
          "node_type": "Box",
          "parameters": { "sizeX": 0.01, "sizeY": 0.02, "sizeZ": 0.40 },
          "transform": { "translate": [0.08, 0.32, 0.12] },
          "description": "套筒右侧的纵向装饰凹槽"
        },
        {
          "id": "slide_text_area",
          "label": "套筒铭文区域",
          "node_type": "Box",
          "parameters": { "sizeX": 0.005, "sizeY": 0.03, "sizeZ": 0.10 },
          "transform": { "translate": [-0.083, 0.36, 0.05] },
          "description": "套筒左侧的GLOCK铭文和序列号区域（浮雕面）"
        }
      ]
    }
  ],

  "node_graph": {
    "description": "将上述所有 primitives 通过 Box/Tube → Transform → Merge 的节点图组装",
    "total_primitives": 35,
    "workflow": [
      "1. 为每个 primitive 创建对应的 Box 或 Tube 节点",
      "2. 每个创建节点后接一个 Transform 节点，设置 translate 和 rotate",
      "3. 同一 group 内的 primitives 先 Merge 为一组",
      "4. 所有 group 的输出再 Merge 为最终整体",
      "5. 接 Fuse → Normal → UVProject → SavePrefab"
    ],
    "edges_pattern": {
      "per_primitive": "BoxNode/TubeNode → TransformNode",
      "per_group": "TransformNode × N → MergeNode (group)",
      "final": "MergeNode (group) × 9 → MergeNode (final) → FuseNode → NormalNode → UVProjectNode → SavePrefabNode"
    }
  },

  "parameter_serialization_reference": {
    "float": { "Key": "sizeX", "ValueJson": "0.16", "ValueType": "float" },
    "Vector3": { "Key": "translate", "ValueJson": "0,0.36,0.12", "ValueType": "Vector3" },
    "int": { "Key": "columns", "ValueJson": "16", "ValueType": "int" },
    "bool": { "Key": "endCaps", "ValueJson": "true", "ValueType": "bool" }
  },

  "notes": [
    "所有尺寸为相对值，整枪长度归一化到约 1.0 单位",
    "握把倾斜角度约 -18 度（Glock 标准握把角度约 108 度，即相对垂直偏 18 度）",
    "ejection_port 和 barrel_bore 应使用 BooleanNode(Subtract) 从主体中减去",
    "slide_channel 应使用 BooleanNode(Subtract) 从套筒中减去",
    "防滑纹可通过 Mountain 节点在 slide_rear_serrations 表面添加",
    "握把纹理可通过 Mountain 节点在 grip_left_panel / grip_right_panel 表面添加",
    "准星和照门的精确形状可通过后续 Extrude + Clip 进一步精修",
    "扳机护圈的弧形可通过 Subdivide(catmull-clark) + Smooth 实现",
    "Vector3 序列化格式：逗号分隔三个分量，无空格（如 '0,0.36,0.12'）"
  ]
}
```