using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using PCGToolkit.Core;

namespace PCGToolkit.Tools
{
    [Overlay(typeof(SceneView), "PCG Selection", true)]
    public class PCGSelectionOverlay : Overlay, ICreateHorizontalToolbar
    {
        public override VisualElement CreatePanelContent()
        {
            var root = new VisualElement();
            root.style.flexDirection = FlexDirection.Column;
            root.style.paddingLeft = 4;
            root.style.paddingRight = 4;
            root.style.paddingTop = 4;
            root.style.paddingBottom = 4;

            // Mode buttons
            var modeRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };

            var faceBtn = new Button(() => PCGSelectionState.SetMode(PCGSelectMode.Face)) { text = "Face" };
            var edgeBtn = new Button(() => PCGSelectionState.SetMode(PCGSelectMode.Edge)) { text = "Edge" };
            var vertBtn = new Button(() => PCGSelectionState.SetMode(PCGSelectMode.Vertex)) { text = "Vertex" };

            faceBtn.style.flexGrow = 1;
            edgeBtn.style.flexGrow = 1;
            vertBtn.style.flexGrow = 1;

            modeRow.Add(faceBtn);
            modeRow.Add(edgeBtn);
            modeRow.Add(vertBtn);
            root.Add(modeRow);

            // Stats label
            var statsLabel = new Label("Selected: 0");
            statsLabel.style.fontSize = 10;
            statsLabel.style.marginBottom = 4;
            root.Add(statsLabel);

            // Action buttons
            var actionRow = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var clearBtn = new Button(() =>
            {
                PCGSelectionState.Clear();
                SceneView.RepaintAll();
            }) { text = "Clear" };
            clearBtn.style.flexGrow = 1;

            var applyBtn = new Button(() =>
            {
                PCGSelectionState.NotifyChanged();
                Debug.Log($"[PCGSelection] Applied selection to graph: {PCGSelectionState.SelectionCount} elements ({PCGSelectionState.CurrentMode})");
            }) { text = "Apply to Graph" };
            applyBtn.style.flexGrow = 1;

            actionRow.Add(clearBtn);
            actionRow.Add(applyBtn);
            root.Add(actionRow);

            // Advanced Selection section
            var advancedFoldout = new Foldout { text = "Advanced Selection", value = false };
            advancedFoldout.style.marginTop = 4;

            var growShrinkRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            var growBtn = new Button(() =>
            {
                var tool = UnityEditor.EditorTools.ToolManager.activeTool as PCGSelectionTool;
                tool?.GrowSelection();
            }) { text = "Grow", tooltip = "Expand selection (Ctrl+Numpad+)" };
            growBtn.style.flexGrow = 1;
            var shrinkBtn = new Button(() =>
            {
                var tool = UnityEditor.EditorTools.ToolManager.activeTool as PCGSelectionTool;
                tool?.ShrinkSelection();
            }) { text = "Shrink", tooltip = "Shrink selection (Ctrl+Numpad-)" };
            shrinkBtn.style.flexGrow = 1;
            growShrinkRow.Add(growBtn);
            growShrinkRow.Add(shrinkBtn);
            advancedFoldout.Add(growShrinkRow);

            var selectUpBtn = new Button(() =>
            {
                var tool = UnityEditor.EditorTools.ToolManager.activeTool as PCGSelectionTool;
                tool?.SelectByNormal(Vector3.up, 0.7f);
            }) { text = "Select Up Faces", tooltip = "Select faces with normals pointing upward" };
            selectUpBtn.style.marginBottom = 2;
            advancedFoldout.Add(selectUpBtn);

            var selectMatBtn = new Button(() =>
            {
                var tool = UnityEditor.EditorTools.ToolManager.activeTool as PCGSelectionTool;
                if (tool != null && PCGSelectionState.SelectedPrimIndices.Count > 0)
                {
                    int primIndex = -1;
                    foreach (int idx in PCGSelectionState.SelectedPrimIndices) { primIndex = idx; break; }
                    if (primIndex >= 0) tool.SelectByMaterialId(primIndex);
                }
            }) { text = "Select by Material", tooltip = "Select all faces with the same material as the current selection" };
            selectMatBtn.style.marginBottom = 2;
            advancedFoldout.Add(selectMatBtn);

            root.Add(advancedFoldout);

            // Update loop
            root.schedule.Execute(() =>
            {
                statsLabel.text = $"Mode: {PCGSelectionState.CurrentMode} | Selected: {PCGSelectionState.SelectionCount}";

                // Update button visuals
                UpdateToggleStyle(faceBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Face);
                UpdateToggleStyle(edgeBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Edge);
                UpdateToggleStyle(vertBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Vertex);

                bool toolActive = UnityEditor.EditorTools.ToolManager.activeTool is PCGSelectionTool;
                growBtn.SetEnabled(toolActive);
                shrinkBtn.SetEnabled(toolActive);
                selectUpBtn.SetEnabled(toolActive);
                selectMatBtn.SetEnabled(toolActive);
            }).Every(200);

            return root;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            var toolbar = new OverlayToolbar();

            var faceToggle = new EditorToolbarToggle
            {
                text = "Face",
                tooltip = "Face selection mode",
                value = PCGSelectionState.CurrentMode == PCGSelectMode.Face
            };
            faceToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) PCGSelectionState.SetMode(PCGSelectMode.Face);
            });

            var edgeToggle = new EditorToolbarToggle
            {
                text = "Edge",
                tooltip = "Edge selection mode",
                value = PCGSelectionState.CurrentMode == PCGSelectMode.Edge
            };
            edgeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) PCGSelectionState.SetMode(PCGSelectMode.Edge);
            });

            var vertToggle = new EditorToolbarToggle
            {
                text = "Vertex",
                tooltip = "Vertex selection mode",
                value = PCGSelectionState.CurrentMode == PCGSelectMode.Vertex
            };
            vertToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue) PCGSelectionState.SetMode(PCGSelectMode.Vertex);
            });

            var clearBtn = new EditorToolbarButton
            {
                text = "Clear",
                tooltip = "Clear selection"
            };
            clearBtn.clicked += () =>
            {
                PCGSelectionState.Clear();
                SceneView.RepaintAll();
            };

            toolbar.Add(faceToggle);
            toolbar.Add(edgeToggle);
            toolbar.Add(vertToggle);
            toolbar.Add(clearBtn);

            toolbar.schedule.Execute(() =>
            {
                faceToggle.SetValueWithoutNotify(PCGSelectionState.CurrentMode == PCGSelectMode.Face);
                edgeToggle.SetValueWithoutNotify(PCGSelectionState.CurrentMode == PCGSelectMode.Edge);
                vertToggle.SetValueWithoutNotify(PCGSelectionState.CurrentMode == PCGSelectMode.Vertex);
            }).Every(200);

            return toolbar;
        }

        private static void UpdateToggleStyle(Button btn, bool active)
        {
            if (active)
                btn.style.backgroundColor = new StyleColor(new Color(0.2f, 0.5f, 1.0f, 0.5f));
            else
                btn.style.backgroundColor = StyleKeyword.Null;
        }
    }
}
