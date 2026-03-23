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

            // Update loop
            root.schedule.Execute(() =>
            {
                statsLabel.text = $"Mode: {PCGSelectionState.CurrentMode} | Selected: {PCGSelectionState.SelectionCount}";

                // Update button visuals
                UpdateToggleStyle(faceBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Face);
                UpdateToggleStyle(edgeBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Edge);
                UpdateToggleStyle(vertBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Vertex);
            }).Every(200);

            return root;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            var toolbar = new OverlayToolbar();
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
