# PCG 节点实现进度清单

> **最后更新：第8轮迭代**

---

## 实现进度汇总

| Tier | 分类 | 总数 | 已实现 | 说明 |
|------|------|------|--------|------|
| Tier 0 | Create | 16 | 16 | ✅ 全部完成 |
| Tier 0 | Attribute | 8 | 8 | ✅ 全部完成 |
| Tier 0 | Utility | 20 | 20 | ✅ 全部完成 |
| Tier 1 | Geometry | 20 | 20 | ✅ 全部完成 |
| Tier 2 | UV | 5 | 5 | ✅ 全部完成 |
| Tier 3 | Distribute | 6 | 6 | ✅ 全部完成 |
| Tier 4 | Curve | 6 | 6 | ✅ 全部完成 |
| Tier 5 | Deform | 8 | 8 | ✅ 全部完成 |
| Tier 6 | Topology | 8 | 8 | ✅ 全部完成（Remesh/Decimate 已切换 g3） |
| Tier 7 | Procedural | 3 | 3 | ✅ 全部完成（基础版本） |
| Tier 8 | Output | 7 | 7 | ✅ 全部完成 |
| **总计** | | **~109** | **~109** | **100%** |

---

## 第三方库集成状态

| 库 | 状态 | 使用节点 |
|---|---|---|
| `geometry3Sharp` | ✅ 深度集成 | BooleanNode (MeshBoolean), RemeshNode (Remesher), DecimateNode (Reducer), GeometryBridge (ToDMesh3/FromDMesh3) |
| `LibTessDotNet` | ✅ 已集成 | PolyFillNode 三角剖分 |
| `MIConvexHull` | ✅ 已集成 | ConvexDecompositionNode, VoronoiFractureNode |
| `Clipper2` | ✅ 已集成 | PolyExpand2DNode, VoronoiFractureNode (2D) |
| `xatlas` | ✅ 已集成 | UVUnwrapNode, UVLayoutNode |

---

## 各 Tier 节点详情

### Tier 0 — Create (16 nodes)
- [x] BoxNode, SphereNode, TubeNode, GridNode, CircleNode, LineNode, TorusNode
- [x] PlatonicSolidsNode, HeightfieldNode, FontNode, ImportMeshNode
- [x] MergeNode, DeleteNode, TransformNode, GroupCreateNode, GroupExpressionNode

### Tier 0 — Attribute (8 nodes)
- [x] AttributeCreateNode, AttributeSetNode, AttributeDeleteNode, AttributeCopyNode
- [x] AttributePromoteNode, AttributeRandomizeNode, AttributeTransferNode, AttributeWrangleNode

### Tier 0 — Utility (20 nodes)
- [x] ConstFloat/Int/Bool/String/Vector3/Color Nodes
- [x] MathFloatNode, CompareNode, FitRangeNode, RampNode, RandomNode
- [x] SwitchNode, SplitNode, NullNode, ForEachNode, GroupCombineNode
- [x] SubGraphInputNode, SubGraphOutputNode, SubGraphNode

### Tier 1 — Geometry (20 nodes)
- [x] ExtrudeNode, BooleanNode (✅ g3 MeshBoolean), SubdivideNode, NormalNode, FuseNode
- [x] ReverseNode, ClipNode, BlastNode, MeasureNode, SortNode
- [x] InsetNode, FacetNode, MirrorNode, PeakNode, TriangulateNode
- [x] ConnectivityNode, PackNode, UnpackNode, PolyExpand2DNode, MaterialAssignNode

### Tier 2 — UV (5 nodes)
- [x] UVProjectNode, UVUnwrapNode, UVLayoutNode, UVTransformNode, UVTrimSheetNode

### Tier 3 — Distribute (6 nodes)
- [x] ScatterNode, CopyToPointsNode, InstanceNode, RayNode, ArrayNode, PointsFromVolumeNode

### Tier 4 — Curve (6 nodes)
- [x] CurveCreateNode, ResampleNode, SweepNode, CarveNode, FilletNode, PolyWireNode

### Tier 5 — Deform (8 nodes)
- [x] MountainNode, BendNode, TwistNode, TaperNode, LatticeNode, SmoothNode, NoiseNode, CreepNode

### Tier 6 — Topology (8 nodes)
- [x] PolyBevelNode, PolyBridgeNode, PolyFillNode
- [x] RemeshNode (✅ 第8轮：已切换到 g3 Remesher)
- [x] DecimateNode (✅ 第8轮：已切换到 g3 Reducer)
- [x] ConvexDecompositionNode, EdgeDivideNode, PolySplitNode

### Tier 7 — Procedural (3 nodes)
- [x] WFCNode (基础版本)
- [x] LSystemNode (基础版本)
- [x] VoronoiFractureNode (基础版本)

### Tier 8 — Output (7 nodes)
- [x] SavePrefabNode, ExportFBXNode, ExportMeshNode, AssemblePrefabNode
- [x] SaveMaterialNode, SaveSceneNode, LODGenerateNode

### Input (2 nodes)
- [x] SceneObjectInputNode, ScenePointsInputNode
