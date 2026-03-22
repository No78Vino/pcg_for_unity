using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// HDA 风格运行时组件：将 PCGGraphData 资产绑定到场景 GameObject，
    /// 可在 Inspector 中覆盖暴露参数并触发图执行。
    /// 图执行逻辑通过反射委托给 Editor-only 的 PCGGraphRunnerBridge。
    /// </summary>
    [AddComponentMenu("PCG Toolkit/PCG Graph Runner")]
    public class PCGGraphRunner : MonoBehaviour
    {
        [Header("Graph Asset")]
        public PCGGraphData GraphAsset;

        [Header("Exposed Parameters")]
        public List<PCGExposedParam> ExposedParams = new List<PCGExposedParam>();

        [Header("Output")]
        [Tooltip("执行后将结果网格放置到此 GameObject（留空则创建子物体）")]
        public GameObject OutputTarget;
        [Tooltip("是否在 Start() 时自动执行")]
        public bool RunOnStart = false;
        [Tooltip("是否将输出 Mesh 实例化为子 GameObject")]
        public bool InstantiateOutput = true;

        private void Start()
        {
            if (RunOnStart) Run();
        }

        /// <summary>
        /// 通过反射调用 Editor-only 的 PCGGraphRunnerBridge.Run()，
        /// 避免 Runtime 程序集直接引用 Editor 程序集类型。
        /// </summary>
        public void Run()
        {
            var bridgeType = System.Type.GetType(
                "PCGToolkit.Graph.PCGGraphRunnerBridge, Assembly-CSharp-Editor");
            if (bridgeType == null)
            {
                Debug.LogWarning("[PCGGraphRunner] PCGGraphRunnerBridge not found. " +
                                 "Graph execution is only available in the Unity Editor.");
                return;
            }

            var method = bridgeType.GetMethod("Run",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method == null)
            {
                Debug.LogError("[PCGGraphRunner] PCGGraphRunnerBridge.Run method not found.");
                return;
            }

            method.Invoke(null, new object[] { this });
        }
    }
}
