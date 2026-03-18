using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// PCG 节点编辑器主窗口
    /// </summary>
    public class PCGGraphEditorWindow : EditorWindow
    {
        private PCGGraphView graphView;
        private PCGGraphData currentGraph;

        [MenuItem("PCG Toolkit/Node Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<PCGGraphEditorWindow>();
            window.titleContent = new GUIContent("PCG Node Editor");
            window.minSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            // TODO: 初始化编辑器窗口
            ConstructGraphView();
            GenerateToolbar();
            Debug.Log("PCGGraphEditorWindow: OnEnable (TODO)");
        }

        private void OnDisable()
        {
            // TODO: 清理资源
            if (graphView != null)
            {
                rootVisualElement.Remove(graphView);
            }
        }

        private void ConstructGraphView()
        {
            // TODO: 创建 GraphView 并添加到窗口
            graphView = new PCGGraphView();
            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);
        }

        private void GenerateToolbar()
        {
            // TODO: 创建工具栏（新建/保存/加载/执行按钮）
            var toolbar = new Toolbar();

            var newButton = new Button(() => NewGraph()) { text = "New" };
            toolbar.Add(newButton);

            var saveButton = new Button(() => SaveGraph()) { text = "Save" };
            toolbar.Add(saveButton);

            var loadButton = new Button(() => LoadGraph()) { text = "Load" };
            toolbar.Add(loadButton);

            var executeButton = new Button(() => ExecuteGraph()) { text = "Execute" };
            toolbar.Add(executeButton);

            rootVisualElement.Add(toolbar);
        }

        private void NewGraph()
        {
            // TODO: 创建新的空图
            Debug.Log("PCGGraphEditorWindow: NewGraph (TODO)");
        }

        private void SaveGraph()
        {
            // TODO: 保存当前图为 ScriptableObject
            Debug.Log("PCGGraphEditorWindow: SaveGraph (TODO)");
        }

        private void LoadGraph()
        {
            // TODO: 从 ScriptableObject 加载图
            Debug.Log("PCGGraphEditorWindow: LoadGraph (TODO)");
        }

        private void ExecuteGraph()
        {
            // TODO: 执行当前图
            Debug.Log("PCGGraphEditorWindow: ExecuteGraph (TODO)");
        }
    }
}
