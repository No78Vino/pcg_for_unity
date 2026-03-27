# Output Skill 文档

## 概述
- **类别**: Output (Tier 3)
- **节点数量**: 7
- **适用场景**: 导出 FBX/Mesh 资产、保存 Prefab/材质/场景、组装层级 Prefab、LOD 生成

## 节点列表

### Export FBX (`ExportFBX`)
- **Houdini 对标**: ROP FBX Output
- **功能**: 将几何体导出为 FBX 文件
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | fbxPath | String | "Assets/PCGOutput/output.fbx" | 否 | 导出路径（Assets/ 开头，.fbx 结尾） |
  | exportMaterials | Bool | true | 否 | 是否导出材质 |
  | copyTextures | Bool | false | 否 | 是否复制纹理 |
  | exportAnimations | Bool | false | 否 | 是否导出动画 |
  | enabled | Bool | true | 否 | 是否执行导出 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 透传输入几何体 |
  | fbxPath | String | 导出的 FBX 文件路径 |

### Export Mesh (`ExportMesh`)
- **Houdini 对标**: ROP Geometry Output
- **功能**: 将几何体导出为 Unity Mesh 资产和 Prefab
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | assetPath | String | "Assets/PCGOutput/output.prefab" | 否 | 保存路径（Assets/ 开头，.prefab 结尾） |
  | createRenderer | Bool | true | 否 | 是否创建 MeshRenderer 并保存为 Prefab |
  | enabled | Bool | true | 否 | 是否执行导出（false 则透传跳过） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 透传输入几何体 |

### Save Prefab (`SavePrefab`)
- **Houdini 对标**: ROP Alembic Output
- **功能**: 将几何体保存为 Unity Prefab 资产
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | assetPath | String | "Assets/PCGOutput/output.prefab" | 否 | 保存路径（Assets/ 开头，.prefab 结尾） |
  | prefabName | String | "" | 否 | Prefab 名称（留空则使用路径中的文件名） |
  | addCollider | Bool | false | 否 | 是否添加 MeshCollider |
  | convexCollider | Bool | false | 否 | 碰撞体是否为凸包 |
  | enabled | Bool | true | 否 | 是否执行导出 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 透传输入几何体 |

### Assemble Prefab (`AssemblePrefab`)
- **Houdini 对标**: N/A（Unity 特有）
- **功能**: 将多个 Geometry 组装为带层级结构的 Prefab（父物体 + 多子物体）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input0 | Geometry | null | 是 | 子部件 0 |
  | input1 | Geometry | null | 否 | 子部件 1 |
  | input2 | Geometry | null | 否 | 子部件 2 |
  | input3 | Geometry | null | 否 | 子部件 3 |
  | input4 | Geometry | null | 否 | 子部件 4 |
  | input5 | Geometry | null | 否 | 子部件 5 |
  | input6 | Geometry | null | 否 | 子部件 6 |
  | input7 | Geometry | null | 否 | 子部件 7 |
  | assetPath | String | "Assets/PCGOutput/assembly.prefab" | 否 | Prefab 保存路径 |
  | rootName | String | "PCG_Assembly" | 否 | 根物体名称 |
  | addColliders | Bool | false | 否 | 是否为每个子物体添加 MeshCollider |
  | enabled | Bool | true | 否 | 是否执行 Prefab 组装 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | prefabPath | String | 保存的 Prefab 路径 |

### Save Material (`SaveMaterial`)
- **Houdini 对标**: N/A（Unity 特有）
- **功能**: 创建并保存 Unity Material 资产
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | assetPath | String | "Assets/PCGOutput/material.mat" | 否 | 保存路径（Assets/ 开头，.mat 结尾） |
  | shaderType | String | "Standard" | 否 | 着色器类型（Standard/URP_Lit/HDRP_Lit/Custom） |
  | customShader | String | "" | 否 | 自定义着色器名称（shaderType=Custom 时生效） |
  | albedoColor | Color | (0.8,0.8,0.8,1) | 否 | 基础颜色 |
  | albedoTexture | String | "" | 否 | 基础颜色纹理路径 |
  | normalMapPath | String | "" | 否 | 法线贴图路径 |
  | metallicMapPath | String | "" | 否 | 金属度/粗糙度贴图路径 |
  | occlusionMapPath | String | "" | 否 | 环境遮蔽贴图路径 |
  | metallic | Float | 0.0 | 否 | 金属度（0~1） |
  | smoothness | Float | 0.5 | 否 | 平滑度（0~1） |
  | emissionColor | Color | (0,0,0,1) | 否 | 自发光颜色（黑色=无自发光） |
  | tiling | Vector3 | (1,1,0) | 否 | 贴图 Tiling |
  | texOffset | Vector3 | (0,0,0) | 否 | 贴图 Offset |
  | renderMode | String | "opaque" | 否 | 渲染模式（opaque/cutout/transparent/fade） |
  | enabled | Bool | true | 否 | 是否执行材质保存 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | material | String | 创建的 Material 路径 |

### Save Scene (`SaveScene`)
- **Houdini 对标**: N/A（Unity 特有）
- **功能**: 组装几何体并保存为 Unity Scene
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | scenePath | String | "Assets/PCGOutput/PCGScene.unity" | 否 | 场景保存路径 |
  | sceneName | String | "" | 否 | 场景名称（留空则使用路径中的文件名） |
  | createNewScene | Bool | true | 否 | 是否创建新场景（false 则追加到当前场景） |
  | objectName | String | "PCGOutput" | 否 | 生成的 GameObject 名称 |
  | addCollider | Bool | false | 否 | 是否添加碰撞体 |
  | position | Vector3 | (0,0,0) | 否 | 物体位置 |
  | rotation | Vector3 | (0,0,0) | 否 | 物体旋转（欧拉角） |
  | scale | Vector3 | (1,1,1) | 否 | 物体缩放 |
  | enabled | Bool | true | 否 | 是否执行场景保存 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 透传输入几何体 |
  | scenePath | String | 保存的场景路径 |

### LOD Generate (`LODGenerate`)
- **Houdini 对标**: N/A（Unity 特有）
- **功能**: 为几何体自动生成 LOD（细节层次）链
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | lodCount | Int | 3 | 否 | LOD 级别数量 |
  | lodRatio | Float | 0.5 | 否 | 每级 LOD 的面数比例 |
  | screenPercentages | String | "0.8,0.4,0.1" | 否 | 各级 LOD 的屏幕占比（逗号分隔） |
  | createGroup | Bool | true | 否 | 为每级 LOD 创建分组 |
  | enabled | Bool | true | 否 | 是否执行 LOD 生成 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 包含所有 LOD 的几何体 |

## 常见组合模式 (Recipes)

### Recipe 1: 完整资产导出
```
Geometry → Normal → UVProject → MaterialAssign → SavePrefab
```
**说明**: 处理几何体后重算法线，添加 UV 投影，分配材质，最后保存为 Prefab 资产。这是完整的资产生产流水线。

### Recipe 2: LOD 链
```
Mesh → Decimate → LODGenerate → SavePrefab
```
**说明**: 对高面数模型先手动 Decimate 测试目标面数，再用 LODGenerate 自动生成多级 LOD 链，最后保存为 Prefab。

### Recipe 3: FBX 导出
```
Geometry → Triangulate → Normal → ExportFBX
```
**说明**: 将几何体统一转为三角形（FBX 兼容），重算法线，导出为 FBX 文件供其他 DCC 工具使用。
