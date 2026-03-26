```json
{
  "project": "No78Vino/pcg_for_unity",
  "tasks": [
    {
      "id": 1,
      "title": "Fix Geometry Spreadsheet row overlap",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGeometrySpreadsheetWindow.cs",
      "priority": "high",
      "bug_description": "All data rows render overlapping at y=0 because GUI.Label/GUI.Button use explicit Rect with y=0, ignoring GUILayout position. Virtual scrolling also lacks spacers.",
      "subtasks": [
        {
          "id": "1.1",
          "description": "In DrawHeader() (lines 221-253), replace GUI.Button(rect, lbl, EditorStyles.toolbarButton) with GUILayout.Button(lbl, EditorStyles.toolbarButton, GUILayout.Width(col.Width), GUILayout.Height(HEADER_HEIGHT)). Remove the manual Rect construction and xPos accumulation.",
          "target_method": "DrawHeader",
          "line_range": [221, 253]
        },
        {
          "id": "1.2",
          "description": "In DrawRow() (lines 255-274), replace GUI.Label(rect, val, style) with GUILayout.Label(val, style, GUILayout.Width(col.Width), GUILayout.Height(ROW_HEIGHT)). Remove the manual Rect construction and xPos accumulation.",
          "target_method": "DrawRow",
          "line_range": [255, 274]
        },
        {
          "id": "1.3",
          "description": "In DrawTable() (lines 199-218), add GUILayout.Space(first * ROW_HEIGHT) before the row loop, and GUILayout.Space((_filteredIndices.Length - last) * ROW_HEIGHT) after the row loop, so the ScrollView knows the total content height.",
          "target_method": "DrawTable",
          "line_range": [199, 218],
          "insert_before_loop": "GUILayout.Space(first * ROW_HEIGHT);",
          "insert_after_loop": "GUILayout.Space((_filteredIndices.Length - last) * ROW_HEIGHT);"
        }
      ]
    },
    {
      "id": 2,
      "title": "Fix Node Preview Wire/ShadedWire rendering",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
      "priority": "high",
      "bug_description": "In Wire and ShadedWire modes, _wireMaterial has no _Color set (defaults to opaque white), rendering a solid white surface that covers the preview. GL.wireframe may also not work reliably with PreviewRenderUtility.",
      "subtasks": [
        {
          "id": "2.1",
          "description": "In OnEnable(), after _wireMaterial is created (around line 88), set a visible wireframe color: _wireMaterial.SetColor(\"_Color\", new Color(0f, 1f, 0f, 1f));",
          "target_method": "OnEnable",
          "line_range": [80, 88],
          "code_to_add": "_wireMaterial.SetColor(\"_Color\", new Color(0f, 1f, 0f, 1f));"
        },
        {
          "id": "2.2",
          "description": "In OnEnable(), after creating _wireMaterial, also set ZTest so wireframe respects depth: _wireMaterial.SetInt(\"_ZTest\", (int)UnityEngine.Rendering.CompareFunction.LessEqual);",
          "target_method": "OnEnable",
          "line_range": [80, 88],
          "code_to_add": "_wireMaterial.SetInt(\"_ZTest\", (int)UnityEngine.Rendering.CompareFunction.LessEqual);"
        },
        {
          "id": "2.3",
          "description": "In the wireframe rendering block (lines 220-228), move GL.wireframe = true to immediately before _previewRenderUtility.camera.Render(), and keep GL.wireframe = false immediately after. This minimizes the global state scope. Ensure DrawMesh is called before GL.wireframe = true since DrawMesh only queues the draw call.",
          "target_method": "OnGUI or the preview rendering method",
          "line_range": [220, 231],
          "revised_code": "_previewRenderUtility.DrawMesh(_previewMesh, Matrix4x4.identity, _wireMaterial != null ? _wireMaterial : _previewMaterial, 0);\nGL.wireframe = true;\n_previewRenderUtility.camera.Render();\nGL.wireframe = false;"
        }
      ]
    }
  ],
  "validation": {
    "test_steps": [
      "Open PCG Geometry Spreadsheet window, verify rows display correctly without overlap and scrolling works",
      "Open PCG Node Preview window, switch to Shaded mode — verify 3D model renders normally",
      "Switch to Wire mode — verify green wireframe lines are visible on dark background",
      "Switch to ShadedWire mode — verify shaded model is visible with green wireframe overlay on top"
    ]
  }
}
```