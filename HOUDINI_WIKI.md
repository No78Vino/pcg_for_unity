# Houdini SOP 节点参考手册

> 本文档整理自 SideFX Houdini 官方文档
> 官方地址: https://www.sidefx.com/docs/houdini/nodes/sop/

---

## 目录

1. [基础几何创建 (Primitives)](#基础几何创建-primitives)
2. [属性操作 (Attribute)](#属性操作-attribute)
3. [几何修改 (Geometry)](#几何修改-geometry)
4. [拓扑操作 (Topology)](#拓扑操作-topology)
5. [曲线操作 (Curve)](#曲线操作-curve)
6. [UV 操作 (UV)](#uv-操作-uv)
7. [点云与散布 (Points & Scatter)](#点云与散布-points--scatter)
8. [变形操作 (Deform)](#变形操作-deform)
9. [群组操作 (Group)](#群组操作-group)
10. [VDB 与体积 (VDB & Volume)](#vdb-与体积-vdb--volume)
11. [模拟相关 (Simulation)](#模拟相关-simulation)
12. [角色与绑定 (Character & Rigging)](#角色与绑定-character--rigging)
13. [群体系统 (Crowd & Agent)](#群体系统-crowd--agent)
14. [毛发与羽毛 (Hair & Feather)](#毛发与羽毛-hair--feather)
15. [肌肉系统 (Muscle)](#肌肉系统-muscle)
16. [材质与纹理 (Material & Texture)](#材质与纹理-material--texture)
17. [导入导出 (I/O)](#导入导出-io)
18. [工具节点 (Utility)](#工具节点-utility)
19. [高度场 (Height Field)](#高度场-height-field)
20. [海洋 (Ocean)](#海洋-ocean)
21. [地形 (Terrain)](#地形-terrain)
22. [机器学习 (Machine Learning)](#机器学习-machine-learning)
23. [USD 集成](#usd-集成)
24. [实验性/Labs 节点](#实验性labs-节点)

---

## 基础几何创建 (Primitives)

| 节点名称 | 功能描述 |
|---------|---------|
| `box` | 创建立方体/长方体 |
| `sphere` | 创建球体或椭球体 |
| `tube` | 创建圆柱/圆管 |
| `grid` | 创建平面网格 |
| `circle` | 创建圆/多边形/弧 |
| `line` | 创建线段 |
| `torus` | 创建环面（甜甜圈）|
| `platonic` | 创建柏拉图立体（正四面体、正六面体等）|
| `torus` | 创建环面 |
| `metaball` | 创建元球 |
| `metagroups` | 元球分组 |
| `superquad` | 创建超二次曲面 |
| `font` | 创建3D文字 |
| `starburst` | 星形几何体 |

---

## 属性操作 (Attribute)

### 创建与修改
| 节点名称 | 功能描述 |
|---------|---------|
| `attribcreate` | 创建新属性 |
| `attribdelete` | 删除属性 |
| `attribwrangle` | VEX表达式修改属性 |
| `attribvop` | VOP网络修改属性 |
| `attribexpression` | 表达式修改属性 |
| `attribrandomize` | 随机化属性值 |
| `attribremap` | 重映射属性值范围 |
| `attribnoise` | 添加噪声到属性 |
| `attribpaint` | 绘制属性 |
| `attribadjustfloat` | 调整浮点属性 |
| `attribadjustinteger` | 调整整数属性 |
| `attribadjustvector` | 调整向量属性 |
| `attribadjustcolor` | 调整颜色属性 |
| `attribadjustdict` | 调整字典属性 |
| `attribadjustarray` | 调整数组属性 |

### 传输与复制
| 节点名称 | 功能描述 |
|---------|---------|
| `attribtransfer` | 传输属性 |
| `attribtransferbyuv` | 按UV传输属性 |
| `attribcopy` | 复制属性 |
| `attribpromote` | 提升属性层级（点/面/细节）|
| `attribinterpolate` | 插值属性 |
| `attribmirror` | 镜像属性 |

### 转换与处理
| 节点名称 | 功能描述 |
|---------|---------|
| `attribcast` | 转换属性类型 |
| `attribblur` | 模糊/平滑属性 |
| `attribcombine` | 组合属性 |
| `attribcomposite` | 合成属性 |
| `attribfade` | 渐变属性 |
| `attribfill` | 填充属性 |
| `attribfrompieces` | 从碎片获取属性 |
| `attribfromvolume` | 从体积获取属性 |
| `attribfrommap` | 从贴图获取属性 |
| `attribfromparm` | 从参数获取属性 |
| `attribreorient` | 重定向属性 |
| `attribsort` | 排序属性 |
| `attribstringedit` | 编辑字符串属性 |
| `attribswap` | 交换属性 |
| `attribcombine` | 组合属性 |

### 其他
| 节点名称 | 功能描述 |
|---------|---------|
| `attribute` | 属性操作通用节点 |
| `color` | 设置颜色属性 |
| `normal` | 计算法线 |
| `rest` | 存储静止位置 |
| `name` | 设置名称属性 |

---

## 几何修改 (Geometry)

### 布尔与切割
| 节点名称 | 功能描述 |
|---------|---------|
| `boolean` | CSG布尔运算 |
| `booleanfracture` | 布尔碎裂 |
| `clip` | 平面裁剪 |
| `polycut` | 多边形切割 |
| `carve` | 曲线裁切 |
| `trim` | 修剪曲线 |
| `blast` | 按条件删除元素 |
| `delete` | 删除元素 |

### 挤出与延伸
| 节点名称 | 功能描述 |
|---------|---------|
| `extrude` | 面挤出 |
| `polyextrude` | 多边形挤出 |
| `polyloft` | 多边形放样 |
| `polybridge` | 多边形桥接 |
| `revolve` | 旋转成型 |
| `sweep` | 沿路径扫掠 |

### 倒角与圆角
| 节点名称 | 功能描述 |
|---------|---------|
| `polybevel` | 边倒角 |
| `fillet` | 曲线倒角/圆角 |
| `crease` | 设置折痕权重 |

### 细分与平滑
| 节点名称 | 功能描述 |
|---------|---------|
| `subdivide` | 细分曲面 |
| `smooth` | 平滑几何体 |
| `relax` | 放松几何体 |
| `dissolve` | 溶解边/点 |
| `refine` | 细化曲线 |

### 拓扑修改
| 节点名称 | 功能描述 |
|---------|---------|
| `divide` | 分割多边形 |
| `triangulate2d` | 2D三角化 |
| `polyfill` | 填充孔洞 |
| `polypatch` | 面片补洞 |
| `polyspline` | 多边形样条 |
| `polysplit` | 分割多边形 |
| `polypath` | 多边形路径 |
| `polysoup` | 多边形汤 |
| `polyreduce` | 减少多边形 |
| `remesh` | 重新网格化 |
| `quadremesh` | 四边形重网格化 |
| `remeshgrid` | 网格重网格化 |
| `remeshbubbles` | 气泡重网格化 |

### 其他修改
| 节点名称 | 功能描述 |
|---------|---------|
| `reverse` | 翻转法线 |
| `fuse` | 合并重叠点 |
| `matchtopology` | 匹配拓扑 |
| `clean` | 清理几何体 |
| `facet` | 面片操作 |
| `hole` | 创建孔洞 |
| `cap` | 封盖 |

---

## 拓扑操作 (Topology)

### 边操作
| 节点名称 | 功能描述 |
|---------|---------|
| `edgecollapse` | 边坍缩 |
| `edgecusp` | 边锐化 |
| `edgedivide` | 边分割 |
| `edgeequalize` | 边等分 |
| `edgeflip` | 边翻转 |
| `edgefracture` | 边碎裂 |
| `edgerelax` | 边放松 |
| `edgestraighten` | 边拉直 |
| `edgetransport` | 边传输 |

### 连接与合并
| 节点名称 | 功能描述 |
|---------|---------|
| `merge` | 合并几何体 |
| `mergepacked` | 合并打包几何体 |
| `connectivity` | 计算连通性 |
| `connectadjacentpieces` | 连接相邻碎片 |

---

## 曲线操作 (Curve)

| 节点名称 | 功能描述 |
|---------|---------|
| `curve` | 创建曲线 |
| `drawcurve` | 绘制曲线 |
| `resample` | 重采样曲线 |
| `carve` | 裁切曲线 |
| `fillet` | 倒角/圆角 |
| `curvesect` | 曲线相交 |
| `ends` | 封闭/开放曲线端点 |
| `orientalongcurve` | 沿曲线定向 |
| `circlespline` | 圆形样条 |
| `crosssectionsurface` | 截面曲面 |
| `rails` | 轨道曲面 |
| `measure` | 测量曲线长度 |

---

## UV 操作 (UV)

| 节点名称 | 功能描述 |
|---------|---------|
| `uvproject` | UV投影 |
| `uvflatten` | UV展开 |
| `uvlayout` | UV布局 |
| `uvunwrap` | UV解包 |
| `uvpelt` | UV剥离 |
| `uvtransform` | UV变换 |
| `uvfuse` | UV合并 |
| `uvbrush` | UV笔刷 |
| `uvedit` | UV编辑 |
| `uvquickshade` | 快速UV着色 |
| `uvautoseam` | 自动UV接缝 |
| `uvflattenfrompoints` | 从点展开UV |
| `texture` | 纹理映射 |
| `texturemaskpaint` | 纹理遮罩绘制 |
| `textureopticalflow` | 纹理光流 |
| `texturefeature` | 纹理特征 |

---

## 点云与散布 (Points & Scatter)

### 点操作
| 节点名称 | 功能描述 |
|---------|---------|
| `scatter` | 表面散布点 |
| `scatteralign` | 对齐散布 |
| `copytopoints` | 复制到点 |
| `copytocurves` | 复制到曲线 |
| `pointgenerate` | 生成点 |
| `pointjitter` | 点抖动 |
| `pointreplicate` | 点复制 |
| `pointweld` | 点焊接 |
| `pointvelocity` | 点速度 |
| `pointsfromvolume` | 从体积生成点 |
| `pointdeform` | 点变形 |
| `pointcapture` | 点捕获 |
| `pointcloudiso` | 点云等值面 |
| `pointcloudmeasure` | 点云测量 |
| `pointcloudnormal` | 点云法线 |
| `pointcloudreduce` | 点云简化 |
| `pointcloudsurface` | 点云表面 |

### 实例化
| 节点名称 | 功能描述 |
|---------|---------|
| `instance` | 实例化 |
| `copyxform` | 复制变换 |
| `findinstances` | 查找实例 |
| `pack` | 打包几何体 |
| `unpack` | 解包几何体 |
| `unpackpoints` | 解包点 |
| `repack` | 重新打包 |
| `packpoints` | 打包点 |
| `packinject` | 打包注入 |

---

## 变形操作 (Deform)

| 节点名称 | 功能描述 |
|---------|---------|
| `bend` | 弯曲变形 |
| `twist` | 扭转变形 |
| `taper` | 锥化变形 |
| `lattice` | 晶格变形 |
| `latticefromvolume` | 从体积创建晶格 |
| `bulge` | 凸起变形 |
| `magnet` | 磁性变形 |
| `smooth` | 平滑 |
| `softpeak` | 柔性峰值 |
| `softxform` | 柔性变换 |
| `xform` | 变换 |
| `xformaxis` | 轴变换 |
| `xformbyattrib` | 按属性变换 |
| `xformpieces` | 按碎片变换 |
| `pathdeform` | 路径变形 |
| `creep` | 爬行变形 |
| `wiredeform` | 线变形 |
| `wirecapture` | 线捕获 |
| `pointdeform` | 点变形 |
| `surfacedeform` | 表面变形 |
| `shrinkwrap` | 收缩包裹 |
| `wrinkledeformer` | 皱纹变形器 |
| `deltamush` | Delta Mush平滑 |
| `dembones_skinningconverter` | Dem Bones蒙皮转换 |

---

## 群组操作 (Group)

| 节点名称 | 功能描述 |
|---------|---------|
| `groupcreate` | 创建群组 |
| `groupdelete` | 删除群组 |
| `groupcombine` | 组合群组 |
| `groupcopy` | 复制群组 |
| `groupexpand` | 扩展群组 |
| `groupexpression` | 表达式群组 |
| `groupfindpath` | 查找路径群组 |
| `groupfromattribboundary` | 从属性边界创建群组 |
| `groupinvert` | 反转群组 |
| `grouppaint` | 绘制群组 |
| `grouppromote` | 提升群组 |
| `grouprange` | 范围群组 |
| `grouprename` | 重命名群组 |
| `groupsfromname` | 从名称创建群组 |
| `grouptransfer` | 传输群组 |
| `groupbylasso` | 套索群组 |

---

## VDB 与体积 (VDB & Volume)

### VDB 创建与转换
| 节点名称 | 功能描述 |
|---------|---------|
| `vdb` | 创建VDB |
| `vdbfrompolygons` | 从多边形创建VDB |
| `vdbfromparticles` | 从粒子创建VDB |
| `vdbfromparticlefluid` | 从粒子流体创建VDB |
| `convertvdb` | 转换VDB |
| `vdbtospheres` | VDB转球体 |
| `vdbtopologytosdf` | VDB拓扑转SDF |

### VDB 处理
| 节点名称 | 功能描述 |
|---------|---------|
| `vdbactivate` | 激活VDB |
| `vdbactivatesdf` | 激活VDB SDF |
| `vdbadvectsdf` | SDF对流 |
| `vdbadvectpoints` | 点对流 |
| `vdbanalysis` | VDB分析 |
| `vdbclip` | VDB裁剪 |
| `vdbcombine` | VDB组合 |
| `vdbconvexclipsdf` | 凸裁剪SDF |
| `vdbdiagnostics` | VDB诊断 |
| `vdbextrapolate` | VDB外推 |
| `vdbfracture` | VDB碎裂 |
| `vdbmorphsdf` | SDF变形 |
| `vdbocclusionmask` | 遮挡遮罩 |
| `vdbpotentialflow` | 势流 |
| `vdbprojectnondivergent` | 投影无散 |
| `vdbrasterizefrustum` | 视锥光栅化 |
| `vdbrenormalizesdf` | SDF重归一化 |
| `vdbresample` | VDB重采样 |
| `vdbreshapesdf` | SDF重塑 |
| `vdbsegmentbyconnectivity` | 连通性分割 |
| `vdbsmooth` | VDB平滑 |
| `vdbsmoothsdf` | SDF平滑 |
| `vdbvisualizetree` | 可视化树 |

### VDB 点操作
| 节点名称 | 功能描述 |
|---------|---------|
| `vdbpointsdelete` | 删除VDB点 |
| `vdbpointsgroup` | VDB点群组 |

### VDB 合并与转换
| 节点名称 | 功能描述 |
|---------|---------|
| `vdbmerge` | 合并VDB |
| `vdbvectormerge` | 向量合并 |
| `vdbvectorsplit` | 向量分割 |
| `vdblod` | VDB LOD |

### Volume 创建与处理
| 节点名称 | 功能描述 |
|---------|---------|
| `volume` | 创建体积 |
| `volumevop` | 体积VOP |
| `volumewrangle` | 体积Wrangle |
| `volumebound` | 体积边界 |
| `volumeblur` | 体积模糊 |
| `volumebreak` | 体积断裂 |
| `volumecombine` | 体积组合 |
| `volumecompress` | 体积压缩 |
| `volumeconvolve3` | 体积卷积 |
| `volumedeform` | 体积变形 |
| `volumefeather` | 体积羽化 |
| `volumefft` | 体积FFT |
| `volumefromattrib` | 从属性创建体积 |
| `volumemerge` | 体积合并 |
| `volumemix` | 体积混合 |
| `volumenoisefog` | 体积雾噪声 |
| `volumenoisesdf` | SDF噪声 |
| `volumenoisevector` | 向量噪声 |
| `volumenormalize` | 体积归一化 |
| `volumepatch` | 体积补丁 |
| `volumeramp` | 体积渐变 |
| `volumerasterize` | 体积光栅化 |
| `volumerasterizeattributes` | 光栅化属性 |
| `volumerasterizecurve` | 光栅化曲线 |
| `volumerasterizehair` | 光栅化毛发 |
| `volumerasterizelattice` | 光栅化晶格 |
| `volumerasterizeparticles` | 光栅化粒子 |
| `volumerasterizepoints` | 光栅化点 |
| `volumereduce` | 体积缩减 |
| `volumeresample` | 体积重采样 |
| `volumeresize` | 体积调整大小 |
| `volumesdf` | 体积SDF |
| `volumeslice` | 体积切片 |
| `volumesplice` | 体积拼接 |
| `volumestamp` | 体积标记 |
| `volumesurface` | 体积表面 |
| `volumetrail` | 体积轨迹 |
| `volumevectorjoin` | 向量合并 |
| `volumevectorsplit` | 向量分割 |
| `volumevelocity` | 体积速度 |
| `volumevelocityfromcurves` | 从曲线获取速度 |
| `volumevelocityfromsurface` | 从表面获取速度 |
| `volumevisualization` | 体积可视化 |
| `volumeambientocclusion` | 体积环境光遮蔽 |
| `volumeanalysis` | 体积分析 |
| `volumearrivaltime` | 到达时间 |
| `volumeopticalflow` | 体积光流 |
| `volumeadjustfog` | 调整雾体积 |
| `particlefluidsurface` | 粒子流体表面 |
| `isooffset` | 等值偏移 |

---

## 模拟相关 (Simulation)

### Pyro (烟火)
| 节点名称 | 功能描述 |
|---------|---------|
| `pyrosolver` | Pyro求解器 |
| `pyrosource` | Pyro源 |
| `pyrosourceinstance` | Pyro实例源 |
| `pyrosourcepack` | Pyro打包源 |
| `pyrosourcespread` | Pyro扩散源 |
| `pyrospawnsources` | 生成Pyro源 |
| `pyroburstsource` | Pyro爆发源 |
| `pyroscatterfromburst` | 从爆发散布 |
| `pyrothrusterexhaust` | 推进器排气 |
| `pyrotrailpath` | Pyro轨迹路径 |
| `pyrotrailsource` | Pyro轨迹源 |
| `pyrobakevolume` | Pyro烘焙体积 |
| `pyropostprocess` | Pyro后处理 |

### FLIP (流体)
| 节点名称 | 功能描述 |
|---------|---------|
| `flipsolver` | FLIP求解器 |
| `flipsource` | FLIP源 |
| `flipboundary` | FLIP边界 |
| `flipcollide` | FLIP碰撞 |
| `flipcontainer` | FLIP容器 |
| `flipvolumecombine` | FLIP体积组合 |
| `fluidcompress` | 流体压缩 |
| `particlefluidsurface` | 粒子流体表面 |
| `particlefluidtank` | 粒子流体罐 |
| `particletrail` | 粒子轨迹 |

### Vellum (布料/软体)
| 节点名称 | 功能描述 |
|---------|---------|
| `vellumsolver` | Vellum求解器 |
| `vellumconstraints` | Vellum约束 |
| `vellumconstraints_grain` | Vellum颗粒约束 |
| `vellumconstraintproperty` | Vellum约束属性 |
| `vellumattachconstraints` | Vellum附加约束 |
| `vellumbrush` | Vellum笔刷 |
| `vellumconfiguremuscles` | Vellum肌肉配置 |
| `vellumconfiguretissue` | Vellum组织配置 |
| `vellumdrape` | Vellum垂挂 |
| `vellumio` | Vellum I/O |
| `vellumpack` | Vellum打包 |
| `vellumpostprocess` | Vellum后处理 |
| `vellumrefframe` | Vellum参考帧 |
| `vellumrestblend` | Vellum静止混合 |
| `vellumunpack` | Vellum解包 |
| `vellumxformpieces` | Vellum碎片变换 |
| `skinsolvervellum` | 皮肤Vellum求解器 |
| `musclesolvervellum` | 肌肉Vellum求解器 |
| `tissuesolvervellum` | 组织Vellum求解器 |

### RBD (刚体)
| 节点名称 | 功能描述 |
|---------|---------|
| `rbdbulletsolver` | RBD子弹求解器 |
| `rbdconfigure` | RBD配置 |
| `rbdpack` | RBD打包 |
| `rbdunpack` | RBD解包 |
| `rbdcluster` | RBD聚类 |
| `rbdmaterialfracture` | RBD材质碎裂 |
| `rbdxform` | RBD变换 |
| `rbdpaint` | RBD绘制 |
| `rbddeformpieces` | RDB变形碎片 |
| `rbddeformingtoanimated` | RBD变形转动画 |
| `rbdconnectedfaces` | RBD连接面 |
| `rbddisconnectedfaces` | RBD断开面 |
| `rbdexplodedview` | RBD爆炸视图 |
| `rbdfindinstances` | RBD查找实例 |
| `rbdgroupconstraints` | RBD约束群组 |
| `rbdguidesetup` | RBD引导设置 |
| `rbdinteriordetail` | RBD内部细节 |
| `rbdio` | RBD I/O |
| `rbdmatchtransforms` | RBD匹配变换 |
| `rbdconstraintproperties` | RBD约束属性 |
| `rbdconetwistconstraintproperties` | RBD锥形扭曲约束属性 |
| `rbdconstraintsfromcurves` | 从曲线创建RBD约束 |
| `rbdconstraintsfromlines` | 从线创建RBD约束 |
| `rbdconstraintsfromrules` | 从规则创建RBD约束 |
| `rbdconvertconstraints` | RBD转换约束 |
| `rbdcardeform` | RBD汽车变形 |
| `rbdcarfollowpath` | RBD汽车跟随路径 |
| `rbdcarfracture` | RBD汽车碎裂 |
| `rbdcarrig` | RBD汽车绑定 |
| `rbdcartransform` | RBD汽车变换 |

### MPM (物质点法)
| 节点名称 | 功能描述 |
|---------|---------|
| `mpmsolver` | MPM求解器 |
| `mpmsource` | MPM源 |
| `mpmcontainer` | MPM容器 |
| `mpmcollider` | MPM碰撞器 |
| `mpmsurface` | MPM表面 |
| `mpmdebrissource` | MPM碎片源 |
| `mpmdeformpieces` | MPM变形碎片 |
| `mpmpostfracture` | MPM后碎裂 |

### Whitewater (白水)
| 节点名称 | 功能描述 |
|---------|---------|
| `whitewatersolver` | 白水求解器 |
| `whitewatersource` | 白水源 |
| `whitewaterpostprocess` | 白水后处理 |

### 其他模拟
| 节点名称 | 功能描述 |
|---------|---------|
| `solver` | 通用求解器 |
| `ripplesolver` | 涟漪求解器 |
| `shallowwatersolver` | 浅水求解器 |
| `grainsource` | 颗粒源 |
| `collisionsource` | 碰撞源 |
| `debrissource` | 碎片源 |
| `otissolver` | Otis求解器 |

---

## 角色与绑定 (Character & Rigging)

### KineFX 核心
| 节点名称 | 功能描述 |
|---------|---------|
| `kinefx--skeleton` | 骨骼 |
| `kinefx--skeletonmirror` | 骨骼镜像 |
| `kinefx--skeletonblend` | 骨骼混合 |
| `kinefx--rigpose` | 绑定姿态 |
| `kinefx--rigstashpose` | 绑定存储姿态 |
| `kinefx--rigmirrorpose` | 绑定镜像姿态 |
| `kinefx--rigmatchpose` | 绑定匹配姿态 |
| `kinefx--rigdoctor` | 绑定医生 |
| `kinefx--rigattribvop` | 绑定属性VOP |
| `kinefx--rigattribwrangle` | 绑定属性Wrangle |
| `kinefx--rigcopytransforms` | 绑定复制变换 |
| `kinefx--rigpython` | 绑定Python |

### 关节与捕获
| 节点名称 | 功能描述 |
|---------|---------|
| `kinefx--configurejoints` | 配置关节 |
| `kinefx--configurejointlimits` | 配置关节限制 |
| `kinefx--orientjoints` | 定向关节 |
| `kinefx--parentjoints` | 父级关节 |
| `kinefx--deletejoints` | 删除关节 |
| `kinefx--groupjoints` | 关节群组 |
| `kinefx--jointcapturebiharmonic` | 关节双调和捕获 |
| `kinefx--jointcapturepaint` | 关节捕获绘制 |
| `kinefx--jointcaptureproximity` | 关节近距离捕获 |
| `kinefx--capturepackedgeo` | 打包捕获几何体 |
| `kinefx--jointdeform` | 关节变形 |

### IK/FK
| 节点名称 | 功能描述 |
|---------|---------|
| `kinefx--fullbodyik` | 全身IK |
| `kinefx--fbikconfiguretargets` | FBIK配置目标 |
| `kinefx--ikchains` | IK链 |
| `kinefx--reversefoot` | 反向脚 |
| `kinefx--fktransfer` | FK传输 |

### 角色处理
| 节点名称 | 功能描述 |
|---------|---------|
| `kinefx--characterpack` | 角色打包 |
| `kinefx--characterunpack` | 角色解包 |
| `kinefx--characterblendshapes` | 角色混合变形 |
| `kinefx--characterblendshapesadd` | 添加角色混合变形 |
| `kinefx--characterblendshapesextract` | 提取角色混合变形 |
| `kinefx--characterblendshapechannels` | 角色混合变形通道 |
| `kinefx--characterio` | 角色I/O |
| `kinefx--attachjointgeo` | 附加关节几何体 |

### 动作与动画
| 节点名称 | 功能描述 |
|---------|---------|
| `kinefx--motionclip` | 动作剪辑 |
| `kinefx--motionclipevaluate` | 动作剪辑评估 |
| `kinefx--motionclipextract` | 动作剪辑提取 |
| `kinefx--motionclipblend` | 动作剪辑混合 |
| `kinefx--motionclipcycle` | 动作剪辑循环 |
| `kinefx--motionclipretime` | 动作剪辑重定时 |
| `kinefx--motionclipsequence` | 动作剪辑序列 |
| `kinefx--motionclipupdate` | 动作剪辑更新 |
| `kinefx--motionclipextractlocomotion` | 提取移动动作 |
| `kinefx--motionclipextractkeyposes` | 提取关键姿态 |
| `kinefx--motionclipposedelete` | 删除动作姿态 |
| `kinefx--motionclipposeinsert` | 插入动作姿态 |
| `kinefx--motionmixer` | 动作混合器 |
| `kinefx--motionmixerfetch` | 动作混合器获取 |
| `kinefx--motionmixerretime` | 动作混合器重定时 |
| `kinefx--motionmixersmooth` | 动作混合器平滑 |
| `kinefx--motionmixertransform` | 动作混合器变换 |
| `kinefx--mocapimport` | 动捕导入 |
| `kinefx--mocapstream` | 动捕流 |

### APEX 绑定
| 节点名称 | 功能描述 |
|---------|---------|
| `apex--graph` | APEX图 |
| `apex--invokegraph` | 调用APEX图 |
| `apex--buildfkgraph` | 构建FK图 |
| `apex--configurecharacter` | 配置角色 |
| `apex--configurecontrols` | 配置控制 |
| `apex--configuregraph` | 配置图 |
| `apex--autorigbuilder` | 自动绑定构建器 |
| `apex--autorigcomponent` | 自动绑定组件 |
| `apex--mapcharacter` | 映射角色 |
| `apex--packcharacter` | 打包角色 |
| `apex--unpackcharacter` | 解包角色 |
| `apex--layoutgraph` | 布局图 |
| `apex--mergegraph` | 合并图 |
| `apex--script` | APEX脚本 |
| `apex--controlextract` | 控制提取 |
| `apex--controlupdateparms` | 控制更新参数 |
| `apex--addgroom` | 添加梳理 |
| `apex--addmldeformer` | 添加ML变形器 |
| `apex--addwrinkles` | 添加皱纹 |
| `apex--animationfromskeleton` | 从骨骼获取动画 |
| `apex--sceneaddanimation` | 场景添加动画 |
| `apex--sceneaddcharacter` | 场景添加角色 |
| `apex--sceneaddprop` | 场景添加道具 |
| `apex--sceneanimate` | 场景动画 |
| `apex--scenecopyanimation` | 场景复制动画 |
| `apex--sceneinvoke` | 场景调用 |

### 捕获与变形
| 节点名称 | 功能描述 |
|---------|---------|
| `capture` | 捕获 |
| `capturelayerpaint` | 捕获层绘制 |
| `capturemirror` | 捕获镜像 |
| `captureoverride` | 捕获覆盖 |
| `captureproximity` | 捕获近距离 |
| `captureattribpack` | 捕获属性打包 |
| `captureattribunpack` | 捕获属性解包 |
| `capturecorrect` | 捕获修正 |
| `bonecapturebiharmonic` | 骨骼双调和捕获 |
| `bonecapturelines` | 骨骼捕获线 |
| `armaturecapture` | 骨架捕获 |
| `armaturedeform` | 骨架变形 |
| `bonedeform` | 骨骼变形 |
| `bonelink` | 骨骼链接 |
| `skin` | 蒙皮 |
| `skindeform` | 蒙皮变形 |
| `skinproperties` | 蒙皮属性 |
| `skinsolidify` | 蒙皮固化 |

### 姿态空间变形
| 节点名称 | 功能描述 |
|---------|---------|
| `posespacedeform` | 姿态空间变形 |
| `posespacedeformcombine` | 姿态空间变形组合 |
| `posespaceedit` | 姿态空间编辑 |
| `posespaceeditconfigure` | 姿态空间编辑配置 |
| `posescope` | 姿态范围 |

### 其他绑定
| 节点名称 | 功能描述 |
|---------|---------|
| `blendshapes` | 混合变形 |
| `extracttpose` | 提取T姿态 |
| `settpose` | 设置T姿态 |
| `frankenmuscle` | Franken肌肉 |
| `frankenmusclepaint` | Franken肌肉绘制 |

---

## 群体系统 (Crowd & Agent)

### Agent 核心
| 节点名称 | 功能描述 |
|---------|---------|
| `agent` | Agent |
| `agentedit` | Agent编辑 |
| `agentprep` | Agent准备 |
| `agentunpack` | Agent解包 |
| `agentproxy` | Agent代理 |
| `agentlayer` | Agent层 |
| `agentlookat` | Agent注视 |
| `agentmetadata` | Agent元数据 |
| `agentrelationship` | Agent关系 |
| `agentterrainadaptation` | Agent地形适应 |
| `agenttransformgroup` | Agent变换组 |
| `agentvellumunpack` | Agent Vellum解包 |

### Agent 动画
| 节点名称 | 功能描述 |
|---------|---------|
| `agentclip` | Agent剪辑 |
| `agentclipproperties` | Agent剪辑属性 |
| `agentcliptransitiongraph` | Agent剪辑过渡图 |
| `agentconfigurejoints` | Agent配置关节 |
| `agentconstraintnetwork` | Agent约束网络 |
| `agentcollisionlayer` | Agent碰撞层 |
| `agentdefinitioncache` | Agent定义缓存 |

### Crowd
| 节点名称 | 功能描述 |
|---------|---------|
| `crowdsource` | 群体源 |
| `crowdassignlayers` | 群体分配层 |
| `crowdmotionpath` | 群体运动路径 |
| `crowdmotionpatharcinglayer` | 运动路径弧形层 |
| `crowdmotionpathavoid` | 运动路径避让 |
| `crowdmotionpathedit` | 运动路径编辑 |
| `crowdmotionpathevaluate` | 运动路径评估 |
| `crowdmotionpathfollow` | 运动路径跟随 |
| `crowdmotionpathlayer` | 运动路径层 |
| `crowdmotionpathretime` | 运动路径重定时 |
| `crowdmotionpathtransition` | 运动路径过渡 |
| `crowdmotionpathtrigger` | 运动路径触发 |

---

## 毛发与羽毛 (Hair & Feather)

### 毛发
| 节点名称 | 功能描述 |
|---------|---------|
| `hairgen` | 毛发生成 |
| `hairclump` | 毛发簇 |
| `haircardgen` | 毛发卡片生成 |
| `hairgrowthfield` | 毛发生长场 |
| `fur` | 皮毛 |

### 引导线
| 节点名称 | 功能描述 |
|---------|---------|
| `guidegroom` | 引导线梳理 |
| `guidedraw` | 引导线绘制 |
| `guideinit` | 引导线初始化 |
| `guideprocess` | 引导线处理 |
| `guideadvect` | 引导线对流 |
| `guideclumpcenter` | 引导线簇中心 |
| `guidecollidevdb` | 引导线VDB碰撞 |
| `guidedeform` | 引导线变形 |
| `guidefill` | 引导线填充 |
| `guidefindstrays` | 引导线查找杂散 |
| `guidegroup` | 引导线群组 |
| `guideinterpolationmesh` | 引导线插值网格 |
| `guidemask` | 引导线遮罩 |
| `guidepartition` | 引导线分区 |
| `guideskinattriblookup` | 引导线蒙皮属性查找 |
| `guidesurface` | 引导线表面 |
| `guidetangentspace` | 引导线切线空间 |
| `guidetransfer` | 引导线传输 |
| `guidevolume` | 引导线体积 |
| `reguide` | 重新引导 |

### 毛发打包/解包
| 节点名称 | 功能描述 |
|---------|---------|
| `packgroom` | 打包梳理 |
| `unpackgroom` | 解包梳理 |
| `groomblend` | 梳理混合 |
| `groomfetch` | 梳理获取 |
| `groomswitch` | 梳理切换 |
| `fibergroom` | 纤维梳理 |

### 羽毛
| 节点名称 | 功能描述 |
|---------|---------|
| `featherprimitive` | 羽毛图元 |
| `featherbarbtangents` | 羽毛须切线 |
| `featherbarbxform` | 羽毛须变换 |
| `featherclump` | 羽毛簇 |
| `featherdeform` | 羽毛变形 |
| `featherdeintersect` | 羽毛去交 |
| `featherinstancepool` | 羽毛实例池 |
| `feathermatchuncondensed` | 羽毛匹配非压缩 |
| `feathermindist` | 羽毛最小距离 |
| `feathernoise` | 羽毛噪声 |
| `feathernormalize` | 羽毛归一化 |
| `featherray` | 羽毛射线 |
| `featherresample` | 羽毛重采样 |
| `feathershapeorg` | 羽毛形状组织 |
| `feathersurfaceblend` | 羽毛表面混合 |
| `feathersurface` | 羽毛表面 |
| `feathertemplateassign` | 羽毛模板分配 |
| `feathertemplatefromshape` | 从形状创建羽毛模板 |
| `feathertemplateinterpolate` | 羽毛模板插值 |
| `featheruncondense` | 羽毛非压缩 |
| `featherutility` | 羽毛工具 |
| `feathervisualize` | 羽毛可视化 |
| `featherwidth` | 羽毛宽度 |

---

## 肌肉系统 (Muscle)

| 节点名称 | 功能描述 |
|---------|---------|
| `muscleid` | 肌肉ID |
| `musclemerge` | 肌肉合并 |
| `musclemirror` | 肌肉镜像 |
| `musclepaint` | 肌肉绘制 |
| `musclepreroll` | 肌肉预卷 |
| `muscleproperties` | 肌肉属性 |
| `musclepropertiesotis` | 肌肉属性Otis |
| `musclesolidify` | 肌肉固化 |
| `muscleadjustvolume` | 肌肉调整体积 |
| `muscleautotensionlines` | 肌肉自动张力线 |
| `muscleconstraintpropertiesotis` | 肌肉约束属性Otis |
| `muscleconstraintpropertiesvellum` | 肌肉约束属性Vellum |
| `muscledeform` | 肌肉变形 |
| `muscledeintersect` | 肌肉去交 |
| `muscleflex` | 肌肉弯曲 |
| `muscletensionlines` | 肌肉张力线 |
| `muscletensionlinesactivate` | 肌肉张力线激活 |
| `muscletransfer` | 肌肉传输 |
| `otisconfiguremuscleandtissue` | Otis配置肌肉和组织 |
| `tissueproperties` | 组织属性 |
| `tissuepropertiesotis` | 组织属性Otis |
| `tissuesolidify` | 组织固化 |
| `tissuesolidifyotis` | 组织固化Otis |

---

## 材质与纹理 (Material & Texture)

| 节点名称 | 功能描述 |
|---------|---------|
| `material` | 材质 |
| `risshader` | RIS着色器 |
| `texture` | 纹理 |
| `texturemaskpaint` | 纹理遮罩绘制 |
| `textureopticalflow` | 纹理光流 |
| `texturefeature` | 纹理特征 |
| `paintcolorvolume` | 绘制颜色体积 |
| `paintfogvolume` | 绘制雾体积 |
| `paintsdfvolume` | 绘制SDF体积 |

---

## 导入导出 (I/O)

### 文件操作
| 节点名称 | 功能描述 |
|---------|---------|
| `file` | 文件 |
| `filecache` | 文件缓存 |
| `cache` | 缓存 |
| `cacheif` | 条件缓存 |
| `stash` | 存储 |

### Alembic
| 节点名称 | 功能描述 |
|---------|---------|
| `alembic` | Alembic |
| `alembicgroup` | Alembic组 |
| `alembicprimitive` | Alembic图元 |
| `rop_alembic` | Alembic输出 |

### FBX
| 节点名称 | 功能描述 |
|---------|---------|
| `rop_fbx` | FBX输出 |
| `kinefx--rop_fbxanimoutput` | FBX动画输出 |
| `kinefx--rop_fbxcharacteroutput` | FBX角色输出 |
| `kinefx--fbxanimimport` | FBX动画导入 |
| `kinefx--fbxcharacterimport` | FBX角色导入 |
| `kinefx--fbxskinimport` | FBX蒙皮导入 |

### GLTF
| 节点名称 | 功能描述 |
|---------|---------|
| `gltf` | GLTF |
| `rop_gltf` | GLTF输出 |
| `kinefx--gltfanimimport` | GLTF动画导入 |
| `kinefx--gltfcharacterimport` | GLTF角色导入 |
| `kinefx--gltfskinimport` | GLTF蒙皮导入 |
| `kinefx--rop_gltfcharacteroutput` | GLTF角色输出 |

### USD
| 节点名称 | 功能描述 |
|---------|---------|
| `usdimport` | USD导入 |
| `usdexport` | USD输出 |
| `usdconfigure` | USD配置 |
| `usdconfigureprimsfrompoints` | 从点配置USD Prims |
| `unpackusd` | 解包USD |
| `kinefx--usdanimimport` | USD动画导入 |
| `kinefx--usdcharacterimport` | USD角色导入 |
| `kinefx--usdskinimport` | USD蒙皮导入 |

### 其他格式
| 节点名称 | 功能描述 |
|---------|---------|
| `rop_geometry` | 几何体输出 |
| `rop_geometryraw` | 原始几何体输出 |
| `lidarimport` | LiDAR导入 |
| `rawimport` | 原始导入 |
| `tableimport` | 表格导入 |
| `mdd` | MDD动画 |
| `unix` | Unix命令 |

---

## 工具节点 (Utility)

### 控制流
| 节点名称 | 功能描述 |
|---------|---------|
| `null` | 空节点 |
| `switch` | 切换 |
| `switchif` | 条件切换 |
| `merge` | 合并 |
| `output` | 输出 |
| `block_begin` | 块开始 |
| `block_end` | 块结束 |
| `compile_begin` | 编译开始 |
| `compile_end` | 编译结束 |
| `each` | 循环 |
| `invoke` | 调用 |
| `invokegraph` | 调用图 |
| `error` | 错误 |

### 变换与对齐
| 节点名称 | 功能描述 |
|---------|---------|
| `xform` | 变换 |
| `matchsize` | 匹配尺寸 |
| `matchaxis` | 匹配轴 |
| `align` | 对齐 |
| `mirror` | 镜像 |
| `fit` | 适配 |
| `bound` | 边界框 |

### 测量与分析
| 节点名称 | 功能描述 |
|---------|---------|
| `measure` | 测量 |
| `measurethickness` | 测量厚度 |
| `distancealonggeometry` | 沿几何体距离 |
| `distancefromgeometry` | 到几何体距离 |
| `distancefromtarget` | 到目标距离 |
| `intersectionanalysis` | 交叉分析 |
| `windingnumber` | 卷绕数 |
| `proximity` | 邻近度 |
| `extractcentroid` | 提取质心 |
| `extractcontours` | 提取轮廓 |
| `extracttransform` | 提取变换 |
| `extractpointfromcurve` | 从曲线提取点 |
| `exportobjtransforms` | 导出对象变换 |

### 选择与过滤
| 节点名称 | 功能描述 |
|---------|---------|
| `blast` | 删除 |
| `delete` | 删除 |
| `split` | 分割 |
| `splitbypointattrib` | 按点属性分割 |
| `splitpoints` | 分割点 |
| `sort` | 排序 |
| `enumerate` | 枚举 |

### 编程
| 节点名称 | 功能描述 |
|---------|---------|
| `python` | Python |
| `pythonsnippet` | Python片段 |
| `script` | 脚本 |
| `opencl` | OpenCL |
| `attribwrangle` | 属性Wrangle |
| `attribvop` | 属性VOP |
| `volumevop` | 体积VOP |
| `volumewrangle` | 体积Wrangle |
| `deformationwrangle` | 变形Wrangle |
| `subnet` | 子网 |

### 其他工具
| 节点名称 | 功能描述 |
|---------|---------|
| `object_merge` | 对象合并 |
| `object_merge` | 对象合并 |
| `visualize` | 可视化 |
| `visibility` | 可见性 |
| `explodedview` | 爆炸视图 |
| `shotsculpt` | 镜头雕刻 |
| `sculpt` | 雕刻 |
| `retime` | 重定时 |
| `timeshift` | 时间偏移 |
| `linearreduce` | 线性缩减 |
| `linearsolver` | 线性求解器 |
| `pca` | PCA |
| `rest` | 静止位置 |
| `control` | 控制 |
| `channel` | 通道 |
| `force` | 力 |
| `null` | 空 |

---

## 高度场 (Height Field)

### 创建与基础操作
| 节点名称 | 功能描述 |
|---------|---------|
| `heightfield` | 高度场 |
| `heightfield_file` | 高度场文件 |
| `heightfield_output` | 高度场输出 |
| `heightfield_copylayer` | 复制层 |
| `heightfield_crop` | 裁剪 |
| `heightfield_flatten` | 展平 |
| `heightfield_resample` | 重采样 |
| `heightfield_xform` | 变换 |
| `heightfield_remap` | 重映射 |
| `heightfield_tilesplice` | 瓦片拼接 |
| `heightfield_tilesplit` | 瓦片分割 |

### 侵蚀与地形
| 节点名称 | 功能描述 |
|---------|---------|
| `heightfield_erode` | 侵蚀 |
| `heightfield_erode_hydro` | 水力侵蚀 |
| `heightfield_erode_precipitation` | 降水侵蚀 |
| `heightfield_erode_thermal` | 热力侵蚀 |
| `heightfield_slump` | 滑坡 |
| `heightfield_terrace` | 梯田 |
| `heightfield_distort` | 扭曲 |
| `heightfield_distortbylayer` | 按层扭曲 |
| `heightfield_deform` | 变形 |

### 遮罩与绘制
| 节点名称 | 功能描述 |
|---------|---------|
| `heightfield_paint` | 绘制 |
| `heightfield_drawmask` | 绘制遮罩 |
| `heightfield_maskbyfeature` | 按特征遮罩 |
| `heightfield_maskbyobject` | 按对象遮罩 |
| `heightfield_maskbyocclusion` | 按遮挡遮罩 |
| `heightfield_pattern` | 图案 |

### 其他
| 节点名称 | 功能描述 |
|---------|---------|
| `heightfield_blur` | 模糊 |
| `heightfield_clip` | 裁剪 |
| `heightfield_noise` | 噪声 |
| `heightfield_patch` | 补丁 |
| `heightfield_project` | 投影 |
| `heightfield_quickshade` | 快速着色 |
| `heightfield_scatter` | 散布 |
| `heightfield_isolatelayer` | 隔离层 |
| `heightfield_layer` | 层 |
| `heightfield_layerclear` | 清除层 |
| `heightfield_layerproperty` | 层属性 |
| `heightfield_flowfield` | 流场 |
| `heightfield_cutoutbyobject` | 按对象切出 |
| `heightfield_visualize` | 可视化 |

---

## 海洋 (Ocean)

| 节点名称 | 功能描述 |
|---------|---------|
| `oceanspectrum` | 海洋频谱 |
| `oceanevaluate` | 海洋评估 |
| `oceansource` | 海洋源 |
| `oceanfoam` | 海洋泡沫 |
| `oceanwaves` | 海洋波浪 |

---

## 地形 (Terrain)

### Sky Field
| 节点名称 | 功能描述 |
|---------|---------|
| `skyfield` | 天空场 |
| `skyfieldfrommap` | 从贴图创建天空场 |
| `skyfieldnoise` | 天空场噪声 |
| `skyfieldpattern` | 天空场图案 |
| `skybox` | 天空盒 |

### Cloud
| 节点名称 | 功能描述 |
|---------|---------|
| `cloudnoise` | 云噪声 |
| `cloudadjustdensityprofile` | 云密度调整 |
| `cloudbillowynoise` | 云团噪声 |
| `cloudclip` | 云裁剪 |
| `cloudlight` | 云光照 |
| `cloudshapefromintersection` | 从交叉创建云形状 |
| `cloudshapefromline` | 从线创建云形状 |
| `cloudshapefrompolygon` | 从多边形创建云形状 |
| `cloudshapegenerate` | 云形状生成 |
| `cloudshapereplicate` | 云形状复制 |
| `cloudwispynoise` | 云丝状噪声 |

---

## 机器学习 (Machine Learning)

| 节点名称 | 功能描述 |
|---------|---------|
| `ml_deform` | ML变形 |
| `ml_attribgenerate` | ML属性生成 |
| `ml_poseserialize` | ML姿态序列化 |
| `ml_posedeserialize` | ML姿态反序列化 |
| `ml_posegenerate` | ML姿态生成 |
| `ml_regressioninference` | ML回归推理 |
| `ml_regressionkernel` | ML回归核 |
| `ml_regressionlinear` | ML回归线性 |
| `ml_regressionproximity` | ML回归邻近 |
| `ml_volumetilecomponent` | ML体积瓦片组件 |
| `ml_volumetileinference` | ML体积瓦片推理 |
| `ml_volumeupres` | ML体积上采样 |
| `onnx` | ONNX |
| `neuralpointsurface` | 神经点表面 |

---

## USD 集成

| 节点名称 | 功能描述 |
|---------|---------|
| `usdimport` | USD导入 |
| `usdexport` | USD输出 |
| `usdconfigure` | USD配置 |
| `usdconfigureprimsfrompoints` | 从点配置USD Prims |
| `unpackusd` | 解包USD |
| `lopimport` | LOP导入 |

---

## 实验性/Labs 节点

### Labs 工具
| 节点名称 | 功能描述 |
|---------|---------|
| `labs--2d_wavefunctioncollapse` | 2D波函数坍缩 |
| `labs--attribute_value_replace` | 属性值替换 |
| `labs--automatic_trim_texture` | 自动裁剪纹理 |
| `labs--autouv` | 自动UV |
| `labs--biome_configure` | 生物群系配置 |
| `labs--biome_configure_multibiomes` | 多生物群系配置 |
| `labs--box_clip` | 盒子裁剪 |
| `labs--calculate_slope` | 计算坡度 |
| `labs--color_blend` | 颜色混合 |
| `labs--curve_branches` | 曲线分支 |
| `labs--delight` | 去光照 |
| `labs--detail_mesh` | 细节网格 |
| `labs--distance_from_border` | 到边界距离 |
| `labs--edge_color` | 边颜色 |
| `labs--extract_borders` | 提取边界 |
| `labs--extract_filename` | 提取文件名 |
| `labs--fast_remesh` | 快速重网格 |
| `labs--fbx_archive_import` | FBX归档导入 |
| `labs--flowmap_guide` | 流向图引导 |
| `labs--flowmap_to_color` | 流向图转颜色 |
| `labs--flowmap_visualize` | 流向图可视化 |
| `labs--goz_export` | GoZ导出 |
| `labs--goz_import` | GoZ导入 |
| `labs--group_by_attribute` | 按属性分组 |
| `labs--group_by_measure` | 按测量分组 |
| `labs--group_curve_corners` | 曲线角分组 |
| `labs--hf_combine_masks` | 合并高度场遮罩 |
| `labs--hf_insert_mask` | 插入高度场遮罩 |
| `labs--houdini_icon` | Houdini图标 |
| `labs--lightning` | 闪电 |
| `labs--merge_small_islands` | 合并小岛 |
| `labs--mesh_sharpen` | 网格锐化 |
| `labs--mesh_slice` | 网格切片 |
| `labs--multi_bounding_box` | 多边界框 |
| `labs--niagara` | Niagara |
| `labs--obj_importer` | OBJ导入器 |
| `labs--osm_buildings` | OSM建筑 |
| `labs--osm_filter` | OSM过滤 |
| `labs--osm_import` | OSM导入 |
| `labs--path_deform` | 路径变形 |
| `labs--physics_painter` | 物理绘制 |
| `labs--pick_and_place` | 拾取放置 |
| `labs--polydeform` | 多边形变形 |
| `labs--procedural_smoke` | 程序化烟雾 |
| `labs--progressive_resample` | 渐进重采样 |
| `labs--quick_basic_tree` | 快速基础树 |
| `labs--rbd_edge_strip` | RBD边缘条 |
| `labs--remove_inside_faces` | 移除内部面 |
| `labs--road_generator` | 道路生成器 |
| `labs--scifi_panels` | 科幻面板 |
| `labs--sine_wave` | 正弦波 |
| `labs--sketchfab_output` | Sketchfab输出 |
| `labs--soften_normals` | 柔化法线 |
| `labs--splatter` | 泼溅 |
| `labs--static_fracture_export` | 静态碎裂导出 |
| `labs--straight_skeleton_3d` | 3D直骨架 |
| `labs--substance_material` | Substance材质 |
| `labs--terrain_layer_export` | 地形层导出 |
| `labs--terrain_layer_import` | 地形层导入 |
| `labs--testgeometry_luiz` | 测试几何体Luiz |
| `labs--testgeometry_paul` | 测试几何体Paul |
| `labs--tree_branch_generator-` | 树分支生成器 |
| `labs--tree_controller-` | 树控制器 |
| `labs--tree_leaf_generator` | 树叶生成器 |
| `labs--tree_simple_leaf` | 树简单叶 |
| `labs--tree_trunk_generator-` | 树干生成器 |
| `labs--trim_texture` | 裁剪纹理 |
| `labs--trim_texture_utility` | 裁剪纹理工具 |
| `labs--triplanar_displace` | 三平面位移 |
| `labs--turntable` | 转盘 |
| `labs--unreal_worldcomposition_prepare` | Unreal世界合成准备 |
| `labs--vector_field` | 向量场 |
| `labs--wang_tiles_decoder` | 王氏瓦片解码器 |
| `labs--wang_tiles_sample` | 王氏瓦片采样 |
| `labs--wfc_initialize` | WFC初始化 |
| `labs--wfc_sample_paint` | WFC采样绘制 |
| `labs--workitem_import` | 工作项导入 |

---

## 测试几何体

| 节点名称 | 功能描述 |
|---------|---------|
| `testgeometry_capybara` | 测试几何体：水豚 |
| `testgeometry_crag` | 测试几何体：Crag |
| `testgeometry_electra` | 测试几何体：Electra |
| `testgeometry_otto` | 测试几何体：Otto |
| `testgeometry_pighead` | 测试几何体：猪头 |
| `testgeometry_rubbertoy` | 测试几何体：橡胶玩具 |
| `testgeometry_shaderball` | 测试几何体：着色球 |
| `testgeometry_squab` | 测试几何体：幼鸽 |
| `testgeometry_templatebody` | 测试几何体：模板身体 |
| `testgeometry_templatehead` | 测试几何体：模板头部 |
| `testgeometry_tommy` | 测试几何体：Tommy |

---

## 统计信息

- **总节点数**: 1013+
- **数据来源**: SideFX Houdini 21.0 官方文档
- **文档生成时间**: 2026-03-21

---

## 参考链接

- 官方文档: https://www.sidefx.com/docs/houdini/nodes/sop/
- Houdini官网: https://www.sidefx.com/

---

*本文档由马鹿鹿🦌整理，用于 pcg_for_unity 项目参考*