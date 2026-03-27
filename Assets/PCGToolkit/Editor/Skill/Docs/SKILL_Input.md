# Input Skill 文档

## 概述
- **类别**: Input (Tier 1)
- **节点数量**: 3
- **适用场景**: 从 Unity 场景读取 GameObject 网格、点云、交互式选择结果，将外部数据导入 PCG 管线

## 节点列表

### Scene Object Input (`SceneObjectInput`)
- **Houdini 对标**: File SOP / Object Merge
- **功能**: 读取场景 GameObject 的 MeshFilter，转为 PCGGeometry
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | target | SceneObject | null | 否 | 场景中的 GameObject（需要 MeshFilter） |
  | applyTransform | Bool | false | 否 | 是否将 world 变换烘焙到顶点 |
  | readMaterials | Bool | true | 否 | 是否将材质路径写入 @material 属性 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 转换后的 PCGGeometry |

### Scene Points Input (`ScenePointsInput`)
- **Houdini 对标**: Object Merge (Points)
- **功能**: 将场景 GameObject 的子对象位置转为点云 PCGGeometry
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | target | SceneObject | null | 否 | 父 GameObject（子对象用作点云） |
  | includeRoot | Bool | false | 否 | 是否包含根对象自身 |
  | readNames | Bool | true | 否 | 将子对象名写入 @name 属性 |
  | space | String | "World" | 否 | 坐标空间（World/Local） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 点云 PCGGeometry（仅含点，无面） |

### Scene Selection Input (`SceneSelectionInput`)
- **Houdini 对标**: N/A（Unity 特有交互式选择）
- **功能**: 读取场景交互选择结果，输出带选择 Group 的几何体
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | target | SceneObject | null | 否 | 场景中的 GameObject（需要 MeshFilter） |
  | groupName | String | "selected" | 否 | 输出的 Group 名称 |
  | applyTransform | Bool | true | 否 | 是否烘焙世界变换 |
  | readMaterials | Bool | true | 否 | 是否读取材质 |
  | serializedSelection | String | "" | 否 | 序列化的选择数据（自动管理，勿手动修改） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 带选择 Group 的 PCGGeometry |

## 常见组合模式 (Recipes)

### Recipe 1: 场景网格编辑
```
SceneObjectInput → Extrude → Normal → SavePrefab
```
**说明**: 从 Unity 场景中读取 GameObject 的网格，进行挤出和法线处理后保存为 Prefab 资产。

### Recipe 2: 点云驱动实例
```
ScenePointsInput → CopyToPoints → Instance
```
**说明**: 读取场景中父物体下所有子对象位置作为点云，将几何体复制到每个子对象位置，实现场景驱动的实例化布置。

### Recipe 3: 交互式局部编辑
```
SceneSelectionInput → Blast → Extrude → Merge
```
**说明**: 读取用户在场景中交互选择的面（输出为 selected 分组），Blast 删除选中面，Extrude 在空洞处挤出新的几何体，Merge 合并结果。
