# 当前待实现节点任务清单

以下是所有待实现功能的 PCG 节点列表，按 Tier 分组，每个节点包含输入端、输出端设计及功能描述。

**已实现节点** 用 ~~删除线~~ 标记。

---

## Tier 4 — Curve

| 节点 | 输入端 | 输出端 | 功能描述 |
|---|---|---|---|
| `CurveCreateNode` | `curveType: String`（bezier/nurbs/polyline）, `order: Int`, `closed: Bool`, `pointCount: Int` | `geometry: Geometry` | 创建曲线几何体（无几何体输入），控制点和曲线类型存储为 Detail 属性 |
| ~~`ResampleNode`~~ | ~~已实现~~ | ~~已实现~~ | ~~按等弧长间距或固定段数重采样曲线，生成均匀分布的点序列~~ |
| `SweepNode` | `backbone: Geometry`（必填）, `crossSection: Geometry`（可选）, `scale: Float`, `twist: Float`, `divisions: Int`, `capEnds: Bool` | `geometry: Geometry` | 沿 backbone 曲线扫掠截面轮廓生成实体网格；使用 Frenet 坐标系定向截面，相邻截面间生成四边形面 |
| ~~`CarveNode`~~ | ~~已实现~~ | ~~已实现~~ | ~~按归一化弧长参数 U（0~1）裁剪曲线，保留 [firstU, secondU] 范围内的部分，可在切割点插入新顶点~~ |
| `FilletNode` | `input: Geometry`（必填）, `radius: Float`, `divisions: Int` | `geometry: Geometry` | 将折线的每个尖角替换为圆弧过渡，每个角生成 `divisions` 段弧线 |

---

## Tier 5 — Deform

所有节点均有 `input: Geometry`（必填）和 `geometry: Geometry` 输出，仅修改点位置不改变拓扑。

| 节点 | 额外输入端 | 功能描述 |
|---|---|---|
| ~~`MountainNode`~~ | ~~已实现~~ | ~~对每个点沿法线方向施加分形噪声（fBm）位移，产生地形起伏效果~~ |
| ~~`BendNode`~~ | ~~已实现~~ | ~~在捕获区域内将几何体沿圆弧弯曲，点的位移量与其在 upAxis 上的位置成比例~~ |
| ~~`TwistNode`~~ | ~~已实现~~ | ~~沿指定轴旋转几何体截面，旋转角度与点在轴上的位置成比例（螺旋扭曲）~~ |
| ~~`TaperNode`~~ | ~~已实现~~ | ~~沿指定轴线性插值截面缩放比例，从 scaleStart 渐变到 scaleEnd（锥化效果）~~ |
| `LatticeNode` | `input: Geometry`（必填）, `lattice: Geometry`（必填）, `restLattice: Geometry`（可选）, `divisionsX/Y/Z: Int` | 自由变形（FFD）：计算每个点在 restLattice 中的三线性参数坐标，用 lattice 控制点的 Bernstein 基函数求出变形后位置 |
| ~~`SmoothNode`~~ | ~~已实现~~ | ~~拉普拉斯平滑：迭代将每个点移向邻居重心；`preserveVolume=true` 时使用 HC-Laplacian 修正以防止体积收缩~~ |

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
| ~~`ExportMeshNode`~~ | ~~已实现~~ | ~~已实现~~ | ~~将 PCGGeometry 转换为 Unity Mesh 资产并保存；可选在场景中创建 MeshRenderer 预览对象~~ |
| `ExportFBXNode` | `input: Geometry`（必填）, `assetPath: String` | `geometry: Geometry`（直通） | 通过 `com.unity.formats.fbx` 包的 `ModelExporter` 将几何体导出为 FBX 文件 |
| ~~`SavePrefabNode`~~ | ~~已实现~~ | ~~已实现~~ | ~~将几何体包装为带 MeshFilter/MeshRenderer（可选 MeshCollider）的 GameObject，用 `PrefabUtility.SaveAsPrefabAsset` 保存为 Prefab~~ |
| ~~`SaveMaterialNode`~~ | ~~已实现~~ | ~~已实现~~ | ~~创建 Unity Material 资产并保存；几何体仅用于判断是否需要材质预览赋值~~ |
| `SaveSceneNode` | `input: Geometry`（必填）, `assetPath: String` | `geometry: Geometry`（直通） | 将几何体实例化为场景对象，调用 `EditorSceneManager.SaveScene` 保存当前场景 |
| `LODGenerateNode` | `input: Geometry`（必填）, `assetPath: String`, `lodLevels: Int` | `geometry: Geometry`（直通，全精度） | 为输入几何体生成多级 LOD：每级用 Decimate 逻辑降面，组装为 `LODGroup` 组件，可选保存为 Prefab |

---

## 实现进度汇总

| Tier | 分类 | 总数 | 已实现 | 待实现 |
|------|------|------|--------|--------|
| Tier 4 | Curve | 5 | 2 | 3 |
| Tier 5 | Deform | 6 | 5 | 1 |
| Tier 6 | Topology | 6 | 0 | 6 |
| Tier 7 | Procedural | 3 | 0 | 3 |
| Tier 8 | Output | 6 | 3 | 3 |
| **总计** | | **26** | **10** | **16** |

---

**说明：**
- 所有节点的 `geometry` 输出端类型均为 `PCGGeometry`，这是系统内部统一的几何数据容器。
- 所有节点的几何体输入端（`input`/`inputA`/`inputB` 等）标注"必填"的，若未连接则执行器会报错。
- Const 系列节点（`ConstFloatNode`、`ConstIntNode`、`ConstBoolNode`、`ConstStringNode`、`ConstVector3Node`、`ConstColorNode`）输出的是标量/向量值而非几何体，通过 `PCGContext.GlobalVariables` 注入下游节点的参数端口，这些节点的 Execute 体也均为 TODO 待实现。