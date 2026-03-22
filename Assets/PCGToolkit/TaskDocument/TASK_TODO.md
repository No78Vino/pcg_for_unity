Repository: No78Vino/pcg_for_unity

## Problem
`PCGGraphRunner` is a MonoBehaviour located at `Assets/PCGToolkit/Editor/Graph/PCGGraphRunner.cs`. Unity compiles scripts in `Editor/` folders into the Editor-only assembly, which means MonoBehaviours there cannot be attached to GameObjects. The user cannot use PCGGraphRunner as a scene component.

## Solution

### Step 1: Create a Runtime directory
Create `Assets/PCGToolkit/Runtime/` directory for scripts that need to be accessible outside the Editor assembly.

### Step 2: Move data-only classes to Runtime
Move the following files from `Assets/PCGToolkit/Editor/Graph/` to `Assets/PCGToolkit/Runtime/`:

1. **`PCGGraphData.cs`** — This is a ScriptableObject that needs to be referenceable by the Runner. It contains: `PCGSerializedParameter`, `PCGNodeData`, `JsonWrapper`, `PCGEdgeData`, `PCGGroupData`, `PCGStickyNoteData`, and `PCGGraphData`. None of these have Editor-only dependencies (they only use `UnityEngine`).

2. **`PCGExposedParam.cs`** — Pure data class, no Editor dependencies. Used as a serialized field in PCGGraphRunner.

### Step 3: Move PCGGraphRunner to Runtime with #if UNITY_EDITOR guards
Move `Assets/PCGToolkit/Editor/Graph/PCGGraphRunner.cs` to `Assets/PCGToolkit/Runtime/PCGGraphRunner.cs`.

Modify it as follows:
- Replace `using UnityEditor;` with `#if UNITY_EDITOR` / `using UnityEditor;` / `#endif`
- Wrap the entire `Run()` method body with `#if UNITY_EDITOR` since it depends on `PCGGraphExecutor`, `PCGNodeRegistry`, `PCGGeometryToMesh`, `PCGContext`, `PCGGeometry` — all Editor-only types
- Wrap `ApplyOutputToScene()` with `#if UNITY_EDITOR` (it uses `AssetDatabase`)
- Wrap the `LastOutput` field's type reference — since `PCGGeometry` is in Editor, either:
    - Option A: Also move `PCGGeometry` and `PCGGeometryToMesh` to Runtime (bigger refactor)
    - Option B: Change `LastOutput` to `[System.NonSerialized] public object LastOutput;` and cast inside `#if UNITY_EDITOR` blocks

  **Recommended: Option B** for minimal changes. The field is already `[System.NonSerialized]` so it doesn't need to be a specific type for serialization.

The modified PCGGraphRunner.cs should look approximately like:

```csharp
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using PCGToolkit.Core;
#endif

namespace PCGToolkit.Graph
{
    [AddComponentMenu("PCG Toolkit/PCG Graph Runner")]
    public class PCGGraphRunner : MonoBehaviour
    {
        [Header("Graph Asset")]
        public PCGGraphData GraphAsset;

        [Header("Exposed Parameters")]
        public List<PCGExposedParam> ExposedParams = new List<PCGExposedParam>();

        [Header("Output")]
        public GameObject OutputTarget;
        public bool RunOnStart = false;
        public bool InstantiateOutput = true;

        [System.NonSerialized]
        public object LastOutput; // PCGGeometry at runtime, but type is Editor-only

        private void Start()
        {
            if (RunOnStart) Run();
        }

        public void Run()
        {
#if UNITY_EDITOR
            // ... existing Run() implementation, keeping all the PCGGraphExecutor logic ...
            // (copy the existing body here unchanged)
#else
            Debug.LogWarning("[PCGGraphRunner] Graph execution is only available in the Unity Editor.");
#endif
        }

        private void ApplyOutputToScene(object geoObj)
        {
#if UNITY_EDITOR
            var geo = geoObj as PCGToolkit.Core.PCGGeometry;
            if (geo == null) return;
            var mesh = PCGGeometryToMesh.Convert(geo);
            // ... rest of existing implementation ...
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            // ...
#endif
        }

        // SerializeValue stays the same (no Editor dependencies)
    }
}
```

### Step 4: Keep PCGGraphRunnerEditor in Editor
`Assets/PCGToolkit/Editor/Graph/PCGGraphRunnerEditor.cs` should stay where it is — it's a `CustomEditor` and belongs in the Editor folder. Update its `using` statements if namespaces changed.

### Step 5: Update namespace references
After moving files, ensure all `using PCGToolkit.Graph;` references in the Editor code still resolve correctly. The moved files should keep the same namespace `PCGToolkit.Graph` so existing references continue to work.

### Step 6: Verify
- Open Unity, let it recompile
- Verify PCGGraphRunner appears in Add Component menu under "PCG Toolkit/PCG Graph Runner"
- Verify you can drag a PCGGraphData .asset into the GraphAsset field
- Verify clicking "Run Graph" in the Inspector still executes correctly
- Verify the node editor's Save/Load still works with PCGGraphData