# Utility Skill 文档

## 概述
- **类别**: Utility (Tier 1)
- **节点数量**: 22
- **适用场景**: 常量输出、数学运算、值域重映射、随机值、曲线映射、比较、条件分支、分组操作、子图复用、循环处理、缓存

## 节点列表

### Const Float (`ConstFloat`)
- **Houdini 对标**: Constant Float
- **功能**: 输出一个常量浮点数
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | Float | 0.0 | 否 | 浮点数值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Float | 输出值 |

### Const Int (`ConstInt`)
- **Houdini 对标**: Constant Int
- **功能**: 输出一个常量整数
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | Int | 0 | 否 | 整数值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Int | 输出值 |

### Const Bool (`ConstBool`)
- **Houdini 对标**: Constant Bool
- **功能**: 输出一个常量布尔值
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | Bool | false | 否 | 布尔值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Bool | 输出值 |

### Const String (`ConstString`)
- **Houdini 对标**: Constant String
- **功能**: 输出一个常量字符串
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | String | "" | 否 | 字符串值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | String | 输出值 |

### Const Vector3 (`ConstVector3`)
- **Houdini 对标**: Constant Vector
- **功能**: 输出一个常量三维向量
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | x | Float | 0.0 | 否 | X 分量 |
  | y | Float | 0.0 | 否 | Y 分量 |
  | z | Float | 0.0 | 否 | Z 分量 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Vector3 | 输出向量 |

### Const Color (`ConstColor`)
- **Houdini 对标**: Constant Color
- **功能**: 输出一个常量颜色
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | r | Float | 1.0 | 否 | 红色通道 (0~1) |
  | g | Float | 1.0 | 否 | 绿色通道 (0~1) |
  | b | Float | 1.0 | 否 | 蓝色通道 (0~1) |
  | a | Float | 1.0 | 否 | 透明通道 (0~1) |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Color | 输出颜色 |

### Math (Float) (`MathFloat`)
- **Houdini 对标**: Math SOP (Float)
- **功能**: 对浮点数进行数学运算
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | a | Float | 0.0 | 否 | 操作数 A |
  | b | Float | 0.0 | 否 | 操作数 B |
  | operation | String | "add" | 否 | 运算类型（add/subtract/multiply/divide/mod/pow/min/max/abs/floor/ceil/round/sqrt/sin/cos/tan） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | result | Float | 计算结果 |

### Math (Vector) (`MathVector`)
- **Houdini 对标**: Math SOP (Vector)
- **功能**: 对向量进行数学运算
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | a | Vector3 | (0,0,0) | 否 | 向量 A |
  | b | Vector3 | (0,0,0) | 否 | 向量 B |
  | operation | String | "add" | 否 | 运算类型（add/subtract/multiply/divide/dot/cross/normalize/length/distance/lerp） |
  | t | Float | 0.5 | 否 | 插值参数（用于 lerp，0~1） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | vector | Vector3 | 向量结果 |
  | scalar | Float | 标量结果（dot/length/distance） |

### Fit Range (`FitRange`)
- **Houdini 对标**: Fit SOP
- **功能**: 将值从旧范围重映射到新范围
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | Float | 0.0 | 否 | 输入值 |
  | oldMin | Float | 0.0 | 否 | 旧范围最小值 |
  | oldMax | Float | 1.0 | 否 | 旧范围最大值 |
  | newMin | Float | 0.0 | 否 | 新范围最小值 |
  | newMax | Float | 1.0 | 否 | 新范围最大值 |
  | clamp | Bool | false | 否 | 是否将结果限制在新范围内 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Float | 重映射后的值 |

### Random (`Random`)
- **Houdini 对标**: Rand SOP
- **功能**: 输出随机 Float/Int/Vector3 值
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | seed | Int | 0 | 否 | 随机种子 |
  | min | Float | 0.0 | 否 | 最小值 |
  | max | Float | 1.0 | 否 | 最大值 |
  | outputType | String | "float" | 否 | 输出类型（float/int/vector3） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Float | 随机值 (Float) |
  | valueInt | Int | 随机值 (Int) |
  | valueVec3 | Vector3 | 随机值 (Vector3) |

### Ramp (`Ramp`)
- **Houdini 对标**: Ramp Parameter
- **功能**: 曲线 Ramp 映射（线性/平滑/阶梯）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | value | Float | 0.0 | 否 | 输入值（0~1） |
  | mode | String | "smooth" | 否 | 插值模式（linear/smooth/step） |
  | key0Pos | Float | 0.0 | 否 | 关键帧0位置 |
  | key0Val | Float | 0.0 | 否 | 关键帧0值 |
  | key1Pos | Float | 0.5 | 否 | 关键帧1位置 |
  | key1Val | Float | 1.0 | 否 | 关键帧1值 |
  | key2Pos | Float | 1.0 | 否 | 关键帧2位置 |
  | key2Val | Float | 0.0 | 否 | 关键帧2值 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | value | Float | 映射后的值 |

### Compare (`Compare`)
- **Houdini 对标**: Compare SOP
- **功能**: 比较两个值，输出 Bool 结果
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | a | Float | 0.0 | 否 | 第一个值 |
  | b | Float | 0.0 | 否 | 第二个值 |
  | operation | String | "equal" | 否 | 比较运算（equal/notEqual/greater/less/greaterEqual/lessEqual） |
  | tolerance | Float | 0.0001 | 否 | equal/notEqual 的容差 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | result | Bool | 比较结果 |

### Switch (`Switch`)
- **Houdini 对标**: Switch SOP
- **功能**: 根据索引或表达式从多个输入中选择一个几何体输出
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input0 | Geometry | null | 否 | 第一个输入几何体 |
  | input1 | Geometry | null | 否 | 第二个输入几何体 |
  | input2 | Geometry | null | 否 | 第三个输入几何体 |
  | input3 | Geometry | null | 否 | 第四个输入几何体 |
  | index | Int | 0 | 否 | 选择输入的索引（0-3） |
  | expression | String | "" | 否 | 选择表达式（优先于 index） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 选中的几何体 |

### Split (`Split`)
- **Houdini 对标**: Blast SOP (双输出模式)
- **功能**: 按 Group 拆分几何体为 matched 和 unmatched 两路
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | group | String | "" | 否 | 用于拆分的 PrimGroup 名称 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | matched | Geometry | 属于指定 Group 的面 |
  | unmatched | Geometry | 不属于指定 Group 的面 |

### For Each (`ForEach`)
- **Houdini 对标**: ForEach SOP
- **功能**: 对每个 Group/Piece/迭代执行子图
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | subGraphPath | String | "" | 否 | 子图资源路径（Assets/...） |
  | mode | String | "byGroup" | 否 | 迭代模式（byGroup/byPiece/count） |
  | iterations | Int | 1 | 否 | count 模式下的迭代次数 |
  | feedback | Bool | false | 否 | count 模式下是否只输出最终迭代结果 |
  | valueAttrib | String | "" | 否 | 要读取值的属性名（注入到 value 变量） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 所有迭代结果合并后的几何体 |

### Cache (`Cache`)
- **Houdini 对标**: File Cache SOP
- **功能**: 缓存几何体到磁盘，避免上游重复计算
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | mode | String | "auto" | 否 | 缓存模式（auto/always_write/always_read/bypass） |
  | cacheName | String | "" | 否 | 自定义缓存名称（留空则自动生成） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体 |

### Null (`Null`)
- **Houdini 对标**: Null SOP
- **功能**: 直通传递几何体，不做任何修改（用于组织图结构和标记检查点）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 否 | 输入几何体 |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 直通输出（与输入完全相同） |

### Group Combine (`GroupCombine`)
- **Houdini 对标**: Group Combine SOP
- **功能**: 对两个 Group 做集合运算（Union/Intersect/Subtract）
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 是 | 输入几何体 |
  | groupA | String | "" | 否 | 第一个分组名 |
  | groupB | String | "" | 否 | 第二个分组名 |
  | operation | String | "union" | 否 | 集合运算（union/intersect/subtract） |
  | resultGroup | String | "combined" | 否 | 结果分组名 |
  | groupType | String | "prim" | 否 | 分组类型（point/prim） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 输出几何体（带结果分组） |

### SubGraph (`SubGraph`)
- **Houdini 对标**: SubNetwork / Object Merge
- **功能**: 实例化并执行子图
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | input | Geometry | null | 否 | 传入子图的几何体 |
  | subGraphPath | String | "" | 否 | 子图资源路径（Assets/...） |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 子图输出的几何体 |

### SubGraph Input (`SubGraphInput`)
- **Houdini 对标**: SubNetwork Input
- **功能**: 定义子图的输入端口
- **输入端口**: 无（动态端口在运行时创建）
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | Geometry | 从父图传入的几何体 |

### SubGraph Output (`SubGraphOutput`)
- **Houdini 对标**: SubNetwork Output
- **功能**: 定义子图的输出端口
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | geometry | Geometry | null | 是 | 输出到父图的几何体 |
- **输出端口**: 无

## 常见组合模式 (Recipes)

### Recipe 1: 参数化控制
```
ConstFloat → Random → Scatter
```
**说明**: ConstFloat 提供种子值传递给 Random 生成随机散布数量，Random 输出用于 Scatter 的 count 参数。

### Recipe 2: 条件分支
```
Geometry → Split → [BranchA, BranchB] → Merge
```
**说明**: 用 Split 按分组将几何体分为两路，每路独立处理后用 Merge 合并，实现条件性差异化处理。

### Recipe 3: 子图复用
```
SubGraphInput → [nodes] → SubGraphOutput
```
**说明**: 在子图内部用 SubGraphInput 接收父图数据，经过一系列处理后通过 SubGraphOutput 返回结果，实现可复用的子图模块。

### Recipe 4: 循环处理
```
Geometry → ForEach → [Transform, Noise] → Merge
```
**说明**: ForEach 按 Group 或 Piece 迭代，每次迭代对当前元素执行 Transform 和 Noise 变形，所有结果自动合并输出。

### Recipe 5: 数学驱动
```
ConstFloat → MathFloat → FitRange → Ramp → AttributeSet
```
**说明**: 常量值经数学运算后通过 FitRange 重映射到新范围，Ramp 添加非线性曲线映射，最终结果用于设置属性值。
