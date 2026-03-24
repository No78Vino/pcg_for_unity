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

            root.Add(PCGSelectionUIHelper.CreateModeToggleRow());
            root.Add(PCGSelectionUIHelper.CreateStatsLabel());

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
            var advElements = PCGSelectionUIHelper.CreateAdvancedSelectionButtons();
            advancedFoldout.Add(advElements.Root);
            root.Add(advancedFoldout);

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
    }
}
