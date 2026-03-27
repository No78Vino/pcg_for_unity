# {CategoryDisplayName} Skill 文档

## 概述
- **类别**: {CategoryEnum} (Tier {TierNumber})
- **节点数量**: {NodeCount}
- **适用场景**: {UseCaseDescription}

## 节点列表

### {NodeDisplayName} (`{NodeName}`)
- **Houdini 对标**: {HoudiniSOPName}
- **功能**: {Description}
- **输入端口**:
  | 端口名 | 类型 | 默认值 | 必填 | 说明 |
  |--------|------|--------|------|------|
  | {PortName} | {PortType} | {DefaultValue} | {Required} | {PortDescription} |
- **输出端口**:
  | 端口名 | 类型 | 说明 |
  |--------|------|------|
  | geometry | PCGGeometry | {OutputDescription} |
- **使用示例**: `{UpstreamNode} → {ThisNode} → {DownstreamNode}`
- **AI Agent 调用示例**:
  ```json
  { "skill": "{NodeName}", "parameters": { {ExampleParams} } }
  ```
- **注意事项**: {Caveats}

## 常见组合模式 (Recipes)

### Recipe 1: {RecipeName}
```
{Node1} → {Node2} → {Node3}
```
**说明**: {RecipeDescription}
