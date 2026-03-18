using UnityEditor;
using UnityEngine;

namespace PCGToolkit.Graph
{
    /// <summary>
    /// 节点图序列化/反序列化
    /// 支持 ScriptableObject 和 JSON 两种格式
    /// </summary>
    public static class PCGGraphSerializer
    {
        /// <summary>
        /// 保存图为 ScriptableObject 资产
        /// </summary>
        public static void SaveAsAsset(PCGGraphData graphData, string assetPath)
        {
            // TODO: 使用 AssetDatabase.CreateAsset 或更新现有资产
            Debug.Log($"PCGGraphSerializer: SaveAsAsset - {assetPath} (TODO)");
        }

        /// <summary>
        /// 从 ScriptableObject 资产加载图
        /// </summary>
        public static PCGGraphData LoadFromAsset(string assetPath)
        {
            // TODO: 使用 AssetDatabase.LoadAssetAtPath 加载
            Debug.Log($"PCGGraphSerializer: LoadFromAsset - {assetPath} (TODO)");
            return null;
        }

        /// <summary>
        /// 序列化为 JSON 字符串
        /// </summary>
        public static string ToJson(PCGGraphData graphData)
        {
            // TODO: 使用 JsonUtility 或自定义序列化
            Debug.Log("PCGGraphSerializer: ToJson (TODO)");
            return JsonUtility.ToJson(graphData, true);
        }

        /// <summary>
        /// 从 JSON 字符串反序列化
        /// </summary>
        public static PCGGraphData FromJson(string json)
        {
            // TODO: 使用 JsonUtility 或自定义反序列化
            Debug.Log("PCGGraphSerializer: FromJson (TODO)");
            var data = ScriptableObject.CreateInstance<PCGGraphData>();
            JsonUtility.FromJsonOverwrite(json, data);
            return data;
        }
    }
}
