using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;
using PCGToolkit.Graph;

namespace PCGToolkit.Editor
{
    /// <summary>
    /// PCGGraphRunner 自定义 Inspector：
    /// - 显示 Graph 资产引用
    /// - 从 Graph 读取 Exposed 参数并渲染覆盖字段
    /// - Run / Sync Params 按钮
    /// </summary>
    [CustomEditor(typeof(PCGGraphRunner))]
    public class PCGGraphRunnerEditor : UnityEditor.Editor
    {
        private PCGGraphRunner _runner;

        private void OnEnable()
        {
            _runner = (PCGGraphRunner)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("PCG Graph Runner", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Graph Asset
            EditorGUILayout.PropertyField(serializedObject.FindProperty("GraphAsset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("OutputTarget"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("RunOnStart"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("InstantiateOutput"));

            EditorGUILayout.Space(8);

            // Sync Params button
            if (GUILayout.Button("Sync Exposed Params from Graph", GUILayout.Height(22)))
                SyncExposedParams();

            EditorGUILayout.Space(4);

            // Exposed Params
            if (_runner.ExposedParams != null && _runner.ExposedParams.Count > 0)
            {
                EditorGUILayout.LabelField("Exposed Parameters", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                foreach (var ep in _runner.ExposedParams)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(ep.DisplayName, GUILayout.Width(140));
                    DrawExposedParamField(ep);
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "点击 'Sync Exposed Params from Graph' 从 Graph 资产加载暴露参数。\n" +
                    "在节点的 PCGParamSchema 中设置 Exposed=true 来暴露参数。",
                    MessageType.Info);
            }

            EditorGUILayout.Space(8);

            // Run button
            GUI.backgroundColor = new Color(0.3f, 0.6f, 0.3f);
            if (GUILayout.Button("Run Graph", GUILayout.Height(30)))
            {
                Undo.RecordObject(_runner, "PCGGraphRunner Run");
                _runner.Run();
                EditorUtility.SetDirty(_runner);
            }
            GUI.backgroundColor = Color.white;

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
                EditorUtility.SetDirty(_runner);
        }

        private void DrawExposedParamField(PCGExposedParam ep)
        {
            EditorGUI.BeginChangeCheck();
            switch (ep.ValueType)
            {
                case "float":
                    ep.FloatValue = EditorGUILayout.FloatField(ep.FloatValue);
                    break;
                case "int":
                    ep.IntValue = EditorGUILayout.IntField(ep.IntValue);
                    break;
                case "bool":
                    ep.BoolValue = EditorGUILayout.Toggle(ep.BoolValue);
                    break;
                case "string":
                    ep.StringValue = EditorGUILayout.TextField(ep.StringValue ?? "");
                    break;
                case "Vector3":
                    ep.Vector3Value = EditorGUILayout.Vector3Field("", ep.Vector3Value);
                    break;
                case "Color":
                    ep.ColorValue = EditorGUILayout.ColorField(ep.ColorValue);
                    break;
                default:
                    ep.StringValue = EditorGUILayout.TextField(ep.StringValue ?? "");
                    break;
            }
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_runner);
        }

        /// <summary>从 GraphAsset 读取所有 Exposed=true 的参数并同步到 ExposedParams 列表</summary>
        private void SyncExposedParams()
        {
            if (_runner.GraphAsset == null)
            {
                Debug.LogWarning("[PCGGraphRunnerEditor] GraphAsset is not assigned.");
                return;
            }

            Undo.RecordObject(_runner, "Sync Exposed Params");

            var newList = new List<PCGExposedParam>();
            foreach (var nodeData in _runner.GraphAsset.Nodes)
            {
                var nodeTemplate = PCGNodeRegistry.GetNode(nodeData.NodeType);
                if (nodeTemplate == null) continue;

                foreach (var schema in nodeTemplate.Inputs)
                {
                    // E5: 检查代码级（schema.Exposed）和图级（ExposedParameters）两种暴露方式
                    bool isExposedInCode = schema.Exposed;
                    bool isExposedInGraph = _runner.GraphAsset.ExposedParameters != null &&
                        _runner.GraphAsset.ExposedParameters.Exists(
                            e => e.NodeId == nodeData.NodeId && e.ParamName == schema.Name);
                    if (!isExposedInCode && !isExposedInGraph) continue;

                    // 查找已有同名参数（保留用户设置的值）
                    var existing = _runner.ExposedParams.Find(
                        e => e.NodeId == nodeData.NodeId && e.ParamName == schema.Name);

                    if (existing != null)
                    {
                        newList.Add(existing);
                    }
                    else
                    {
                        var ep = new PCGExposedParam
                        {
                            NodeId      = nodeData.NodeId,
                            ParamName   = schema.Name,
                            DisplayName = $"{nodeData.NodeType}/{schema.DisplayName}",
                        };
                        // 推断类型
                        string typeStr = schema.PortType switch
                        {
                            PCGPortType.Float   => "float",
                            PCGPortType.Int     => "int",
                            PCGPortType.Bool    => "bool",
                            PCGPortType.String  => "string",
                            PCGPortType.Vector3 => "Vector3",
                            PCGPortType.Color   => "Color",
                            _                   => "string",
                        };
                        // 尝试从 nodeData.Parameters 取当前值作为默认
                        var p = nodeData.Parameters.Find(x => x.Key == schema.Name);
                        ep.SetDefaultFromString(p?.ValueJson ?? "", typeStr);
                        newList.Add(ep);
                    }
                }
            }

            _runner.ExposedParams = newList;
            EditorUtility.SetDirty(_runner);
            Debug.Log($"[PCGGraphRunnerEditor] Synced {newList.Count} exposed params.");
        }
    }
}
