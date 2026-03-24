using UnityEngine;
using UnityEngine.UIElements;
using PCGToolkit.Core;

namespace PCGToolkit.Tools
{
    public static class PCGSelectionUIHelper
    {
        public struct AdvancedSelectionElements
        {
            public VisualElement Root;
            public Button GrowBtn;
            public Button ShrinkBtn;
            public Slider NormalThresholdSlider;
            public Button SelectUpBtn;
            public Button SelectMatBtn;
        }

        public static AdvancedSelectionElements CreateAdvancedSelectionButtons()
        {
            var root = new VisualElement();

            var growShrinkRow = new VisualElement { style = { flexDirection = FlexDirection.Row, marginBottom = 4 } };
            var growBtn = new Button(() =>
            {
                PCGSelectionTool.ActiveInstance?.GrowSelection();
            }) { text = "Grow", tooltip = "Expand selection (Ctrl+Numpad+)" };
            growBtn.style.flexGrow = 1;
            var shrinkBtn = new Button(() =>
            {
                PCGSelectionTool.ActiveInstance?.ShrinkSelection();
            }) { text = "Shrink", tooltip = "Shrink selection (Ctrl+Numpad-)" };
            shrinkBtn.style.flexGrow = 1;
            growShrinkRow.Add(growBtn);
            growShrinkRow.Add(shrinkBtn);
            root.Add(growShrinkRow);

            var normalThresholdSlider = new Slider("Threshold", 0f, 1f) { value = 0.7f };
            normalThresholdSlider.style.marginBottom = 2;
            root.Add(normalThresholdSlider);

            var selectUpBtn = new Button(() =>
            {
                PCGSelectionTool.ActiveInstance?.SelectByNormal(Vector3.up, normalThresholdSlider.value);
            }) { text = "Select Up Faces", tooltip = "Select faces with normals pointing upward" };
            selectUpBtn.style.marginBottom = 2;
            root.Add(selectUpBtn);

            var selectMatBtn = new Button(() =>
            {
                var tool = PCGSelectionTool.ActiveInstance;
                if (tool != null && PCGSelectionState.SelectedPrimIndices.Count > 0)
                {
                    int primIndex = -1;
                    foreach (int idx in PCGSelectionState.SelectedPrimIndices) { primIndex = idx; break; }
                    if (primIndex >= 0) tool.SelectByMaterialId(primIndex);
                }
            }) { text = "Select by Material", tooltip = "Select all faces with the same material as the current selection" };
            selectMatBtn.style.marginBottom = 2;
            root.Add(selectMatBtn);

            root.schedule.Execute(() =>
            {
                bool toolActive = PCGSelectionTool.ActiveInstance != null;
                growBtn.SetEnabled(toolActive);
                shrinkBtn.SetEnabled(toolActive);
                selectUpBtn.SetEnabled(toolActive);
                selectMatBtn.SetEnabled(toolActive);
            }).Every(200);

            return new AdvancedSelectionElements
            {
                Root = root,
                GrowBtn = growBtn,
                ShrinkBtn = shrinkBtn,
                NormalThresholdSlider = normalThresholdSlider,
                SelectUpBtn = selectUpBtn,
                SelectMatBtn = selectMatBtn
            };
        }

        public static VisualElement CreateModeToggleRow()
        {
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

            modeRow.schedule.Execute(() =>
            {
                UpdateToggleStyle(faceBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Face);
                UpdateToggleStyle(edgeBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Edge);
                UpdateToggleStyle(vertBtn, PCGSelectionState.CurrentMode == PCGSelectMode.Vertex);
            }).Every(200);

            return modeRow;
        }

        public static Label CreateStatsLabel()
        {
            var statsLabel = new Label("Selected: 0");
            statsLabel.style.fontSize = 10;
            statsLabel.style.marginBottom = 4;

            statsLabel.schedule.Execute(() =>
            {
                statsLabel.text = $"Mode: {PCGSelectionState.CurrentMode} | Selected: {PCGSelectionState.SelectionCount}";
            }).Every(200);

            return statsLabel;
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
