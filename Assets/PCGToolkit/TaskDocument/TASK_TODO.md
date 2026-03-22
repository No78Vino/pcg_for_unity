Repository: No78Vino/pcg_for_unity

## Goal
Implement the E5 feature from TASK_TODO.md: an "Expose" toggle button in the PCGNodeInspectorWindow that allows users to mark parameters as exposed to PCGGraphRunner without modifying node source code.

## File Changes

### 1. `Assets/PCGToolkit/Editor/Graph/PCGGraphData.cs`

**Add new data class** (before `PCGGraphData` class, around line 103):
```csharp
[Serializable]
public class PCGExposedParamInfo
{
    public string NodeId;
    public string ParamName;
}
```

**Add field to `PCGGraphData`** (after line 116, alongside Groups and StickyNotes):
```csharp
public List<PCGExposedParamInfo> ExposedParameters = new List<PCGExposedParamInfo>();
```

**Update `Clear()` method** (line 164-170) to also clear ExposedParameters:
```csharp
ExposedParameters.Clear();
```

**Update `Clone()` method** (line 175-196) to copy ExposedParameters:
```csharp
copy.ExposedParameters = new List<PCGExposedParamInfo>();
foreach (var ep in ExposedParameters)
    copy.ExposedParameters.Add(new PCGExposedParamInfo { NodeId = ep.NodeId, ParamName = ep.ParamName });
```

### 2. `Assets/PCGToolkit/Editor/Graph/PCGGraphView.cs`

**Add a tracking field** (near line 14, alongside other private fields):
```csharp
private List<PCGExposedParamInfo> _exposedParams = new List<PCGExposedParamInfo>();
```

**Add public methods** for Inspector to query/modify exposed state:
```csharp
public bool IsParamExposed(string nodeId, string paramName)
{
    return _exposedParams.Exists(e => e.NodeId == nodeId && e.ParamName == paramName);
}

public void SetParamExposed(string nodeId, string paramName, bool exposed)
{
    if (exposed)
    {
        if (!IsParamExposed(nodeId, paramName))
            _exposedParams.Add(new PCGExposedParamInfo { NodeId = nodeId, ParamName = paramName });
    }
    else
    {
        _exposedParams.RemoveAll(e => e.NodeId == nodeId && e.ParamName == paramName);
    }
    OnGraphChanged?.Invoke();
}
```

**Update `SaveToGraphData()`** (around line 803, before `return data;`):
```csharp
// Serialize exposed parameters
data.ExposedParameters = new List<PCGExposedParamInfo>(_exposedParams);
```

**Update `LoadGraph()`** (around line 719, before the closing brace):
```csharp
// Restore exposed parameters
_exposedParams = data.ExposedParameters != null 
    ? new List<PCGExposedParamInfo>(data.ExposedParameters) 
    : new List<PCGExposedParamInfo>();
```

### 3. `Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs`

**In `CreateInspectorParam()` method** (around line 418-444, in the `headerRow` construction):

After the description label is added to `headerRow` (around line 444), and before `container.Add(headerRow)` (line 445), add the Expose toggle:

```csharp
// E5: Expose toggle — only for non-Geometry parameter ports
if (schema.PortType != PCGPortType.Geometry && schema.PortType != PCGPortType.Any)
{
    var exposeToggle = new Toggle("")
    {
        value = _graphView != null && _graphView.IsParamExposed(nodeVisual.NodeId, schema.Name),
        tooltip = "Expose this parameter to PCGGraphRunner"
    };
    exposeToggle.style.width = 20;
    exposeToggle.style.marginLeft = 4;
    // Visual indicator: tint the toggle when exposed
    if (exposeToggle.value)
    {
        exposeToggle.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.3f, 0.8f, 1.0f));
    }
    exposeToggle.RegisterValueChangedCallback(evt =>
    {
        _graphView?.SetParamExposed(nodeVisual.NodeId, schema.Name, evt.newValue);
    });
    headerRow.Add(exposeToggle);
}
```

### 4. `Assets/PCGToolkit/Editor/Graph/PCGGraphRunnerEditor.cs`

**Update `SyncExposedParams()` method** (lines 117-176):

Change the inner loop to check BOTH `schema.Exposed` (code-level) AND `graphData.ExposedParameters` (graph-level). Replace the condition on line 136:

```csharp
// Old: if (!schema.Exposed) continue;
// New: check both code-level and graph-level exposed state
bool isExposedInCode = schema.Exposed;
bool isExposedInGraph = _runner.GraphAsset.ExposedParameters != null &&
    _runner.GraphAsset.ExposedParameters.Exists(
        e => e.NodeId == nodeData.NodeId && e.ParamName == schema.Name);
if (!isExposedInCode && !isExposedInGraph) continue;
```

This ensures backward compatibility: parameters marked `Exposed = true` in code still work, AND parameters toggled in the Inspector also work.

## Testing
After implementation:
1. Open PCG Node Editor, create a few nodes (e.g., BoxNode, ExtrudeNode)
2. Select a node, check the Inspector — each parameter should have a small toggle on the right of the header row
3. Toggle a parameter's expose button ON
4. Save the graph
5. In a scene, add PCGGraphRunner component, assign the saved graph
6. Click "Sync Exposed Params from Graph" — the toggled parameter should appear
7. Verify that code-level `Exposed = true` parameters also still appear
8. Verify that reloading the graph preserves the exposed toggle state