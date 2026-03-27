# Deform Skill 文档

## 概述
- **类别**: Deform (Tier 2)
- **节点数量**: 8
- **适用场景**: 噪声变形、地形生成、弯曲、扭转、锥化、晶格变形、平滑、表面爬行

## 节点列表

### Noise (`Noise`)
- **Houdini 对标**: Mountain SOP 增强版
- **功能**: 通用噪声变形（Perlin/Worley/Curl）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | noiseType | String | "perlin" | 否 | 噪声类型（perlin/worley/curl） |
  | amplitude | Float | 0.5 | 否 | 噪声振幅 |
  | frequency | Float | 1.0 | 否 | 噪声频率 |
  | octaves | Int | 3 | 否 | 叠加层数 |
  | offset | Vector3 | (0,0,0) | 否 | 噪声空间偏移 |
  | direction | String | "normal" | 否 | 变形方向（normal/axis/3d） |
  | axis | Vector3 | (0,1,0) | 否 | axis 模式下的变形轴向 |
  | group | String | "" | 否 | 仅对指定点分组变形 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Mountain (`Mountain`)
- **Houdini 对标**: Mountain SOP
- **功能**: 用噪声函数对几何体进行变形（产生山脉/地形效果）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | height | Float | 1.0 | 否 | 噪声高度/振幅 |
  | frequency | Float | 1.0 | 否 | 噪声频率 |
  | octaves | Int | 4 | 否 | 分形叠加层数 |
  | lacunarity | Float | 2.0 | 否 | 频率递增倍数 |
  | persistence | Float | 0.5 | 否 | 振幅递减比例 |
  | seed | Int | 0 | 否 | 随机种子 |
  | noiseType | String | "perlin" | 否 | 噪声类型（perlin/simplex/value） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Bend (`Bend`)
- **Houdini 对标**: Bend SOP
- **功能**: 沿指定轴弯曲几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | angle | Float | 90.0 | 否 | 弯曲角度 |
  | upAxis | String | "y" | 否 | 弯曲轴向（x/y/z） |
  | captureOrigin | Vector3 | (0,0,0) | 否 | 弯曲起始点 |
  | captureLength | Float | 1.0 | 否 | 受影响的长度范围 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 弯曲后的几何体 |

### Twist (`Twist`)
- **Houdini 对标**: Twist SOP
- **功能**: 沿指定轴扭转几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | angle | Float | 180.0 | 否 | 总扭转角度 |
  | axis | String | "y" | 否 | 扭转轴（x/y/z） |
  | origin | Vector3 | (0,0,0) | 否 | 扭转中心 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 扭转后的几何体 |

### Taper (`Taper`)
- **Houdini 对标**: Taper SOP
- **功能**: 沿指定轴对几何体进行锥化（渐变缩放）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | scaleStart | Float | 1.0 | 否 | 起始端缩放 |
  | scaleEnd | Float | 0.0 | 否 | 结束端缩放 |
  | axis | String | "y" | 否 | 锥化轴（x/y/z） |
  | origin | Vector3 | (0,0,0) | 否 | 锥化中心 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 锥化后的几何体 |

### Lattice (`Lattice`)
- **Houdini 对标**: Lattice SOP
- **功能**: 使用晶格控制点对几何体进行自由变形（FFD）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | lattice | Geometry | null | 否 | 变形后的晶格控制点 |
  | divisionsX | Int | 2 | 否 | X 方向晶格分段 |
  | divisionsY | Int | 2 | 否 | Y 方向晶格分段 |
  | divisionsZ | Int | 2 | 否 | Z 方向晶格分段 |
  | deformX | Float | 0.5 | 否 | X 方向变形强度 |
  | deformY | Float | 0.5 | 否 | Y 方向变形强度 |
  | deformZ | Float | 0.5 | 否 | Z 方向变形强度 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Smooth (`Smooth`)
- **Houdini 对标**: Smooth SOP
- **功能**: 对几何体进行拉普拉斯平滑
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | iterations | Int | 10 | 否 | 平滑迭代次数 |
  | strength | Float | 0.5 | 否 | 平滑强度（0~1） |
  | group | String | "" | 否 | 仅平滑指定分组的点（留空=全部） |
  | preserveVolume | Bool | false | 否 | 保持体积（HC Laplacian） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 平滑后的几何体 |

### Creep (`Creep`)
- **Houdini 对标**: Creep SOP
- **功能**: 将点投射到目标表面上（沿最近面爬行）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 要变形的几何体 |
  | target | Geometry | null | 是 | 目标表面几何体 |
  | offset | Float | 0.0 | 否 | 投射后沿法线偏移距离 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 变形后的几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 有机地形
```
Grid → Mountain → Smooth → Normal
```
**说明**: 创建平面网格，用 Mountain 噪声生成地形起伏，Smooth 平滑地形使其更自然，Normal 重算法线使光照正确。

### Recipe 2: 弯曲柱体
```
Tube → Bend → Twist → Normal
```
**说明**: 创建圆柱体，Bend 弯曲成弧形，Twist 添加扭转效果，最后 Normal 重算法线。

### Recipe 3: 风化效果
```
Geometry → Noise(low frequency) → Noise(high frequency) → Smooth
```
**说明**: 先用低频噪声产生大尺度变形（整体风化形状），叠加高频噪声增加细节（裂纹/凹坑），Smooth 平滑过度尖锐的细节。
