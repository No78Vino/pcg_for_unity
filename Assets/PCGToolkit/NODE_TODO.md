# 当前待实现节点任务清单

以下是所有待实现功能的 PCG 节点列表，按 Tier 分组，每个节点包含输入端、输出端设计及功能描述。

---

## Tier 0 — Create / Utility / Attribute

### Create 类（几何体生成）

所有 Create 节点无几何体输入，直接生成 `PCGGeometry`。

| 节点 | 输入端（参数） | 输出端 | 功能描述 |
|---|---|---|---|
| `BoxNode` | `sizeX/Y/Z: Float`, `center: Vector3` | `geometry: Geometry` | 生成轴对齐的长方体，8个顶点 + 6个四边形面 |
| `SphereNode` | `radius: Float`, `rows/cols: Int`, `center: Vector3` | `geometry: Geometry` | 生成 UV 球体，按经纬度细分 |
| `TubeNode` | `radius: Float`, `height: Float`, `rows/cols: Int` | `geometry: Geometry` | 生成圆柱管体 |
| `GridNode` | `sizeX/Z: Float`, `rowsX/Z: Int` | `geometry: Geometry` | 生成平面网格 |
| `CircleNode` | `radius: Float`, `divisions: Int` | `geometry: Geometry` | 生成圆形多边形 |
| `LineNode` | `origin: Vector3`, `direction: Vector3`, `length: Float`, `points: Int` | `geometry: Geometry` | 生成折线段 |
| `TorusNode` | `outerRadius/innerRadius: Float`, `rows/cols: Int` | `geometry: Geometry` | 生成圆环体 |

### Utility 类（几何体操作）

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `MergeNode` | `input0: Geometry`（必填）, `input1..N: Geometry` | `geometry: Geometry` | 将多路几何体流合并为一个，追加 Points/Primitives/Edges |
| `DeleteNode` | `input: Geometry`（必填）, `group: String`, `filter: String`, `deleteNonSelected: Bool` | `geometry: Geometry` | 按组名或表达式过滤删除点/面/边；`deleteNonSelected=true` 时反选删除 |
| `GroupCreateNode` | `input: Geometry`（必填）, `groupName: String`, `groupType: String`, `filter: String`, `baseGroup: String` | `geometry: Geometry` | 在几何体上创建命名点组或面组，写入 `PointGroups`/`PrimGroups` |
| `ImportMeshNode` | `assetPath: String` | `geometry: Geometry` | 从 Unity 项目读取 Mesh 资产，转换为 `PCGGeometry`（顶点→Points，三角形→Primitives） |
| `TransformNode` | `input: Geometry`（必填）, `translate: Vector3`, `rotate: Vector3`, `scale: Vector3` | `geometry: Geometry` | 对所有点应用平移/旋转（欧拉角）/缩放变换 |

### Attribute 类（属性管理）

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `AttributeCreateNode` | `input: Geometry`（必填）, `name: String`, `class: String`（point/vertex/primitive/detail）, `type: String`（float/int/vector3/vector4/color/string）, `defaultFloat/Vector3/String` | `geometry: Geometry` | 在指定层级（点/顶点/面/细节）创建新属性，并用默认值填充所有元素 |
| `AttributeSetNode` | `input: Geometry`（必填）, `name: String`, `class: String`, `expression: String`, `group: String`, `valueFloat: Float`, `valueVector3: Vector3` | `geometry: Geometry` | 修改已有属性值；支持常量赋值或逐元素表达式求值（类似 Houdini AttribWrangle），可限定到命名组 |

---

## Tier 1 — Core Geometry

所有节点均有 `input: Geometry`（必填）和 `geometry: Geometry` 输出。

| 节点 | 额外输入端 | 功能描述 |
|---|---|---|
| `BooleanNode` | `inputA: Geometry`（必填）, `inputB: Geometry`（必填）, `operation: String`（union/intersect/subtract） | 对两个几何体执行 CSG 布尔运算（并集/交集/差集），后端使用 geometry3Sharp `MeshBoolean` |
| `ExtrudeNode` | `group: String`, `distance: Float`, `inset: Float`, `divisions: Int`, `outputFront/Side: Bool` | 将选中面沿法线方向挤出，生成侧壁四边形；可控制内缩量、挤出段数、是否输出顶面/侧面 |
| `ClipNode` | `origin: Vector3`, `normal: Vector3`, `keepAbove: Bool` | 用平面（origin+normal 定义）裁切几何体，保留平面一侧；跨平面的面需插值生成新顶点和封口面 |
| `BlastNode` | `group: String`, `groupType: String`（point/primitive）, `deleteNonSelected: Bool` | 按组名删除点或面；`deleteNonSelected=true` 时仅保留组内元素（与 DeleteNode 的区别在于以组为核心操作单元） |
| `FuseNode` | `distance: Float`, `group: String` | 将距离小于阈值的顶点合并为一个，更新所有 Primitives 的索引引用；后端使用 geometry3Sharp 空间加速结构 |
| `NormalNode` | `type: String`（point/primitive/vertex）, `cuspAngle: Float`, `weightByArea: Bool` | 重新计算法线并写入 `AttributeStore`（属性名 `"N"`）；支持平面法线、点法线（面积加权平均）、顶点分裂法线（硬边角度阈值） |
| `MeasureNode` | `type: String`（area/perimeter/curvature/volume）, `attribName: String` | 非破坏性节点：计算几何体度量值（面积/周长/曲率/体积）并写入属性，几何体本身不变 |

---

## Tier 2 — UV

所有节点均有 `input: Geometry`（必填）和 `geometry: Geometry` 输出，UV 坐标写入 `VertexAttribs`。

| 节点 | 额外输入端 | 功能描述 |
|---|---|---|
| `UVProjectNode` | `projectionType: String`（planar/cylindrical/spherical）, `axis: Vector3`, `scale: Vector2` | 按投影方式（平面/柱面/球面）将 3D 坐标映射为 UV，纯 C# 实现 |
| `UVTransformNode` | `translate: Vector2`, `rotate: Float`, `scale: Vector2` | 对已有 UV 坐标做平移/旋转/缩放变换，纯 C# 实现 |

> `UVUnwrapNode` 和 `UVLayoutNode` 已通过 xatlas 实现，不在待实现列表中。

---

## Tier 3 — Distribute

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `ScatterNode` | `input: Geometry`（必填）, `density: Float`, `seed: Int`, `relaxIterations: Int` | `geometry: Geometry` | 在输入几何体表面随机散布点云，支持泊松松弛以避免点过于密集 |
| `CopyToPointsNode` | `source: Geometry`（必填）, `target: Geometry`（必填）, `usePointOrient: Bool`, `usePointScale: Bool`, `pack: Bool` | `geometry: Geometry` | 将 source 几何体复制到 target 的每个点上，按点的 `orient`/`pscale` 属性应用 TRS 变换后合并输出 |
| `InstanceNode` | `source: Geometry`（必填）, `target: Geometry`（必填）, `instanceAttribute: String` | `geometry: Geometry` | 类似 CopyToPoints，但通过 `instanceAttribute` 属性从多个 source 中选择不同几何体实例 |
| `RayNode` | `input: Geometry`（必填）, `collider: Geometry`（必填）, `direction: Vector3`, `maxDistance: Float` | `geometry: Geometry` | 将输入几何体的点沿 direction 方向投射到 collider 表面，用于地形贴合放置 |

---

## Tier 4 — Curve

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `CurveCreateNode` | `curveType: String`（bezier/nurbs/polyline）, `order: Int`, `closed: Bool`, `pointCount: Int` | `geometry: Geometry` | 创建曲线几何体（无几何体输入），控制点和曲线类型存储为 Detail 属性 |
| `ResampleNode` | `input: Geometry`（必填）, `method: String`（length/count）, `length: Float`, `segments: Int`, `treatAsSubdivision: Bool` | `geometry: Geometry` | 按等弧长间距或固定段数重采样曲线，生成均匀分布的点序列 |
| `SweepNode` | `backbone: Geometry`（必填）, `crossSection: Geometry`（可选）, `scale: Float`, `twist: Float`, `divisions: Int`, `capEnds: Bool` | `geometry: Geometry` | 沿 backbone 曲线扫掠截面轮廓生成实体网格；使用 Frenet 坐标系定向截面，相邻截面间生成四边形面 |
| `CarveNode` | `input: Geometry`（必填）, `firstU: Float`, `secondU: Float`, `cutAtFirstU: Bool`, `cutAtSecondU: Bool` | `geometry: Geometry` | 按归一化弧长参数 U（0~1）裁剪曲线，保留 [firstU, secondU] 范围内的部分，可在切割点插入新顶点 |
| `FilletNode` | `input: Geometry`（必填）, `radius: Float`, `divisions: Int` | `geometry: Geometry` | 将折线的每个尖角替换为圆弧过渡，每个角生成 `divisions` 段弧线 |

---

## Tier 5 — Deform

所有节点均有 `input: Geometry`（必填）和 `geometry: Geometry` 输出，仅修改点位置不改变拓扑。

| 节点 | 额外输入端 | 功能描述 |
|---|---|---|
| `MountainNode` | `height: Float`, `frequency: Float`, `octaves: Int`, `lacunarity: Float`, `persistence: Float`, `seed: Int`, `noiseType: String`（perlin/simplex/value） | 对每个点沿法线方向施加分形噪声（fBm）位移，产生地形起伏效果 |
| `BendNode` | `angle: Float`, `upAxis: String`, `captureOrigin: Vector3`, `captureLength: Float` | 在捕获区域内将几何体沿圆弧弯曲，点的位移量与其在 upAxis 上的位置成比例 |
| `TwistNode` | `angle: Float`, `axis: String`（x/y/z）, `origin: Vector3` | 沿指定轴旋转几何体截面，旋转角度与点在轴上的位置成比例（螺旋扭曲） |
| `TaperNode` | `scaleStart: Float`, `scaleEnd: Float`, `axis: String`, `origin: Vector3` | 沿指定轴线性插值截面缩放比例，从 scaleStart 渐变到 scaleEnd（锥化效果） |
| `LatticeNode` | `input: Geometry`（必填）, `lattice: Geometry`（必填）, `restLattice: Geometry`（可选）, `divisionsX/Y/Z: Int` | 自由变形（FFD）：计算每个点在 restLattice 中的三线性参数坐标，用 lattice 控制点的 Bernstein 基函数求出变形后位置 |
| `SmoothNode` | `iterations: Int`, `strength: Float`（0~1）, `group: String`, `preserveVolume: Bool` | 拉普拉斯平滑：迭代将每个点移向邻居重心；`preserveVolume=true` 时使用 HC-Laplacian 修正以防止体积收缩 |

---

## Tier 6 — Topology

所有节点均有 `input: Geometry`（必填）和 `geometry: Geometry` 输出，修改网格连接关系。

| 节点 | 额外输入端 | 功能描述 |
|---|---|---|
| `PolyBevelNode` | `group: String`, `distance: Float`, `segments: Int` | 对选中边或点进行倒角，插入新的边循环；后端使用 geometry3Sharp `MeshBevel` |
| `PolyBridgeNode` | `groupA: String`, `groupB: String`, `divisions: Int` | 在两个开放边界循环之间生成连接多边形带；需要两个面组作为输入标识 |
| `PolyFillNode` | `group: String`, `fillType: String` | 填充开放边界循环，生成封口面；使用 LibTessDotNet 对 N 边形进行三角剖分 |
| `RemeshNode` | `targetEdgeLength: Float`, `iterations: Int`, `enableSmoothing: Bool`, `enableProjection: Bool` | 重新三角化网格以达到目标边长，使用 geometry3Sharp `Remesher` |
| `DecimateNode` | `targetTriangleCount: Int` 或 `targetRatio: Float`, `preserveBoundary: Bool` | 在保持形状的前提下减少多边形数量，使用 geometry3Sharp `Reducer` |
| `ConvexDecompositionNode` | `maxHulls: Int`, `maxVerticesPerHull: Int` | 将网格分解为多个凸包，适合物理碰撞体；使用 MIConvexHull 库，每个凸包作为独立面组输出 |

---

## Tier 7 — Procedural

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `WFCNode` | `tileSet: Geometry`（必填）, `gridWidth: Int`, `gridHeight: Int`, `seed: Int`, `maxRetries: Int` | `geometry: Geometry` | 波函数坍缩（WFC）：从 tileSet 的面组中读取邻接规则，通过约束传播为每个网格单元分配瓦片，再用 CopyToPoints 模式实例化瓦片几何体 |
| `LSystemNode` | `axiom: String`, `rules: String`, `iterations: Int`, `angle: Float`, `stepLength: Float`, `seed: Int` | `geometry: Geometry` | L-System：对公理字符串迭代应用重写规则，再用海龟图形解释器（`F`=前进，`[`/`]`=分支栈）将结果字符串转换为折线几何体 |
| `VoronoiFractureNode` | `input: Geometry`（必填）, `points: Geometry`（种子点）, `seed: Int`, `numPieces: Int`, `interior: Bool` | `geometry: Geometry` | Voronoi 破碎：以种子点计算 Voronoi 图，用 Clipper2（2D）或 geometry3Sharp BSP（3D）将输入网格裁切为各 Voronoi 单元，每个碎片作为独立面组输出 |

---

## Tier 8 — Output

所有输出节点均有 `geometry: Geometry` 直通输出端（输入几何体原样传递，不做修改），以便节点可插入图中间而不断链。

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `ExportMeshNode` | `input: Geometry`（必填）, `assetPath: String`, `createRenderer: Bool` | `geometry: Geometry`（直通） | 将 PCGGeometry 转换为 Unity Mesh 资产并保存；可选在场景中创建 MeshRenderer 预览对象 |
| `ExportFBXNode` | `input: Geometry`（必填）, `assetPath: String` | `geometry: Geometry`（直通） | 通过 `com.unity.formats.fbx` 包的 `ModelExporter` 将几何体导出为 FBX 文件 |
| `SavePrefabNode` | `input: Geometry`（必填）, `assetPath: String`, `addCollider: Bool`, `materialPath: String` | `geometry: Geometry`（直通） | 将几何体包装为带 MeshFilter/MeshRenderer（可选 MeshCollider）的 GameObject，用 `PrefabUtility.SaveAsPrefabAsset` 保存为 Prefab |
| `SaveMaterialNode` | `input: Geometry`（直通，不消耗）, `assetPath: String`, `shaderName: String`, `albedoColor: Color`, `metallic: Float`, `smoothness: Float` | `geometry: Geometry`（直通） | 创建 Unity Material 资产并保存；几何体仅用于判断是否需要材质预览赋值 |
| `SaveSceneNode` | `input: Geometry`（必填）, `assetPath: String` | `geometry: Geometry`（直通） | 将几何体实例化为场景对象，调用 `EditorSceneManager.SaveScene` 保存当前场景 |
| `LODGenerateNode` | `input: Geometry`（必填）, `assetPath: String`, `lodLevels: Int` | `geometry: Geometry`（直通，全精度） | 为输入几何体生成多级 LOD：每级用 Decimate 逻辑降面，组装为 `LODGroup` 组件，可选保存为 Prefab |

---

**说明：**
- 所有节点的 `geometry` 输出端类型均为 `PCGGeometry`，这是系统内部统一的几何数据容器。
- 所有节点的几何体输入端（`input`/`inputA`/`inputB` 等）标注"必填"的，若未连接则执行器会报错。
- Const 系列节点（`ConstFloatNode`、`ConstIntNode`、`ConstBoolNode`、`ConstStringNode`、`ConstVector3Node`、`ConstColorNode`）输出的是标量/向量值而非几何体，通过 `PCGContext.GlobalVariables` 注入下游节点的参数端口，这些节点的 Execute 体也均为 TODO 待实现。