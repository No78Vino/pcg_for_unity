# AGENT开发指导方针

## AI 在 pcg_for_unity 中的定位

**AI 是工具的开发者，不是工具的使用者。**

AI Agent 的产出物是 **SubGraph（即封装好的 PCG 工具）**，而不是 3D 资产本身。3D 资产的审美决策权始终属于人（地编/环境美术），AI 负责的是让这些人手里的工具更强大、开发更快。

映射到架构的三层：

| 层 | 定位 |
|----|------|
| **底层** PCG 节点库 | AI 可以实现新的 `PCGNodeBase` 子类，扩充原子能力 |
| **中层** GraphView + SubGraph | AI 的**主战场**——用原子节点组装 SubGraph，封装为地编可用的工具 |
| **上层** Skill 接口 | AI 通过 Skill 调用来**开发和测试工具**，而非替代地编生产资产 |

具体来说，`AgentServer` 和 `SkillExecutor` 服务的对象是 AI Agent 在开发期的工具构建和验证流程，而不是制作期的资产批量生产： 

---

## AI Agent 必须遵守的原则

### 原则一：审美决策权归人，AI 不越界

AI 不做任何涉及空间构图、视觉节奏、叙事引导的决策。以下事项 **禁止** AI 自行决定：

- 物件摆放在哪里（位置、朝向）
- 某个区域的破损程度和方向
- 密度、高度、疏密等视觉参数的具体取值
- 关卡的动线和视觉焦点

AI 的职责是把这些决策**暴露为可控参数**，交给地编来定。

### 原则二：产出物是工具，不是资产

AI 通过 Skill 接口调用原子节点，组装出 SubGraph。SubGraph 就是一个"给定输入 → 可控输出"的工具。AI 的交付物是这个 SubGraph 本身，而不是 SubGraph 执行后产出的 Mesh/FBX/Prefab。

对应到代码，`ISkill.Execute()` 在 AI 的工作流中用于**测试和验证工具是否正确**，而不是用于批量生产最终资产： 

### 原则三：最大程度精炼可控要素

AI 构建 SubGraph 时，必须遵循"最小暴露面"原则——只把地编真正需要控制的参数暴露出去，其余全部封装在内部。

`SubGraphNode` 的 `GetSubGraphInputs()` / `GetSubGraphOutputs()` 定义了这个暴露面： 

一个好的 SubGraph 工具应该是：

```
暴露给地编的：
  - 笔刷范围（Geometry 输入）
  - 密度（Float）
  - 高度范围（Float2）
  - seed（Int，换一批随机结果）

封装在内部的（地编不需要碰）：
  - Scatter 的泊松盘算法参数
  - Mountain 的噪声频率/octaves
  - CopyToPoints 的朝向对齐逻辑
  - Normal 重算、UV 投影等技术细节
```

### 原则四：非控制需求的内容由 AI 代行填充

在地编确定了控制要素（范围、密度、高度等）之后，工具内部的所有"填充性"工作——随机分布、噪声变形、法线重算、UV 展开——由 SubGraph 内部的节点链自动完成。这是 AI 构建工具时应该做好的部分。

`ScatterNode` 的 `densityAttrib` 和 `group` 参数就是这个理念的体现——地编刷出范围（group），设定密度（densityAttrib），剩下的分布算法由节点内部处理：

### 原则五：Skill 嵌套服务于工具分层，不服务于资产批量生产

`SkillRegistry` 自动将所有 `IPCGNode` 注册为 Skill，Skill 嵌套的意义是让 AI 能用低层工具组装高层工具： 

```
原子 Skill（Scatter, Mountain, CopyToPoints...）
    ↓ AI 组装
低层 SubGraph Skill（土堆形态生成器）
    ↓ AI 组装
高层 SubGraph Skill（土堆分布器 = 形态 + 分布 + UV + 法线）
    ↓ 交付给地编使用
```

不是：~~原子 Skill → AI 链式调用 → 批量产出 100 个土堆资产~~ 

### 原则六：`IPCGNode` 统一接口的双重含义

`IPCGNode` 同时服务于 GraphView 和 AI Agent Skill 调用，但两者的使用场景不同：

| 调用方 | 场景 | 目的 |
|--------|------|------|
| GraphView（人类） | 制作期 | 地编通过可视化界面使用封装好的工具 |
| AI Agent（Skill） | 开发期 | AI 调用原子节点来组装和测试 SubGraph |

`PCGNodeSkillAdapter` 过滤掉 Geometry 端口只暴露标量参数，这在"AI 开发工具"的语境下是正确的——AI 需要理解每个节点的参数语义来正确组装节点图： 

---

## 一句话总结

> **AI Agent 在 pcg_for_unity 中的角色是"工具链的开发者"：它用原子节点组装出参数精炼、输出可控的 SubGraph 工具，然后交给地编美术使用。审美判断归人，流水线开发归 AI。**

---

## Graph 构建 API（第8轮新增，第9轮完善）

从第8轮迭代开始，AI Agent 可以通过 API **直接创建和保存 SubGraph**，无需人工在 GraphView 中操作。第9轮新增了完整的 CRUD 操作和端口校验。

### 完整 Action 列表

| Action | 说明 |
|--------|------|
| `list_nodes` | 查询所有可用节点类型及其端口定义（按 Category 分组） |
| `create_graph` | 创建一个新的空 PCGGraphData 实例 |
| `add_node` | 向指定图中添加节点（指定节点类型和位置） |
| `connect_nodes` | 连接两个节点的端口（含端口存在性校验） |
| `set_param` | 设置节点的参数值 |
| `execute_graph` | 执行指定图并返回输出摘要 |
| `save_graph` | 将图保存为 `.asset` 文件 |
| `get_graph_info` | 查询图的完整结构（节点、连线、参数） |
| `delete_node` | 删除节点及其关联的所有边 |
| `disconnect_nodes` | 断开两个节点之间的连线 |
| `delete_graph` | 删除内存中的图实例 |
| `list_graphs` | 列出所有活跃图的摘要（ID、名称、节点数） |

### 推荐工作流（含错误恢复）

```
1. list_nodes           → 了解有哪些原子节点可用
2. create_graph         → 创建空图
3. add_node × N         → 添加所需节点
4. connect_nodes × N    → 按数据流方向连线（端口错误会被自动拒绝并提示正确端口名）
5. set_param × N        → 配置每个节点的参数
6. execute_graph        → 验证图能正确执行并产出预期几何体
7. save_graph           → 保存为 .asset，交付给地编使用

错误恢复：
- 节点类型选错 → delete_node 删除后重新 add_node
- 连线错误 → disconnect_nodes 断开后重新 connect_nodes
- 整图作废 → delete_graph 清理后 create_graph 重建
- 查看当前状态 → list_graphs 查看所有活跃图
```

### 通信协议

- **HTTP**：POST JSON 到 `http://localhost:8765/`（默认端口）
- **WebSocket**：连接 `ws://localhost:8765/`，发送/接收 JSON 文本消息

### 会话管理

`AgentSession` 支持多图并行构建。每次 `create_graph` 返回唯一 `graph_id`，后续操作通过 `graph_id` 指定目标图。图在内存中维护，直到 `save_graph` 持久化或服务器停止。

### 与 Skill 层的关系

- `execute_skill` / `execute_pipeline`：直接调用单个/多个节点，适合快速测试
- `create_graph` / `add_node` / `connect_nodes` / `execute_graph` / `save_graph`：构建完整 SubGraph，适合交付工具

两者互补：先用 Skill 探索节点行为，再用 Graph API 组装工具。