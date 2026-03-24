using UnityEditor;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Examples
{
    public static class DaggerExampleScene
    {
        private const string GraphAssetPath = "Assets/PCGToolkit/Examples/ProceduralDagger.asset";

        [MenuItem("PCG Toolkit/Examples/Setup Dagger Scene")]
        public static void SetupScene()
        {
            var graphAsset = AssetDatabase.LoadAssetAtPath<PCGGraphData>(GraphAssetPath);
            if (graphAsset == null)
            {
                EditorUtility.DisplayDialog("Missing Graph Asset",
                    $"ProceduralDagger.asset not found at:\n{GraphAssetPath}\n\nPlease run 'PCG Toolkit > Examples > Create Dagger Graph' first.",
                    "OK");
                return;
            }

            var go = new GameObject("Dagger Generator");
            var runner = go.AddComponent<PCGGraphRunner>();
            runner.GraphAsset = graphAsset;
            runner.InstantiateOutput = true;

            Undo.RegisterCreatedObjectUndo(go, "Create Dagger Generator");
            Selection.activeGameObject = go;

            Debug.Log("[DaggerExampleScene] Dagger Generator created. Click 'Sync Exposed Params' then 'Run Graph' in the Inspector.");
        }
    }
}
