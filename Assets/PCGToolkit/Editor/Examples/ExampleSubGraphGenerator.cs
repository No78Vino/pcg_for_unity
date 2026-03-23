using UnityEditor;
using UnityEngine;
using PCGToolkit.Graph;

namespace PCGToolkit.Examples
{
    public static class ExampleSubGraphGenerator
    {
        [MenuItem("PCG Toolkit/Generate Example SubGraphs")]
        public static void GenerateAll()
        {
            string dir = "Assets/PCGToolkit/Examples/SubGraphs";
            if (!AssetDatabase.IsValidFolder("Assets/PCGToolkit/Examples"))
                AssetDatabase.CreateFolder("Assets/PCGToolkit", "Examples");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/PCGToolkit/Examples", "SubGraphs");

            GenerateParametricTable(dir);
            GenerateTerrainScatter(dir);
            GenerateBeveledWall(dir);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ExampleSubGraphGenerator: 3 example SubGraphs created.");
        }

        private static void GenerateParametricTable(string dir)
        {
            var graph = ScriptableObject.CreateInstance<PCGGraphData>();
            graph.GraphName = "ParametricTable";

            var grid = graph.AddNode("Grid", new Vector2(0, 0));
            grid.SetParameter("rows", 4);
            grid.SetParameter("columns", 4);
            grid.SetParameter("sizeX", 2.0f);
            grid.SetParameter("sizeY", 2.0f);

            var extrude = graph.AddNode("Extrude", new Vector2(250, 0));
            extrude.SetParameter("amount", 0.1f);

            var uvProject = graph.AddNode("UVProject", new Vector2(500, 0));
            uvProject.SetParameter("mode", "Box");

            var savePrefab = graph.AddNode("SavePrefab", new Vector2(750, 0));
            savePrefab.SetParameter("path", "Assets/Generated/Table.prefab");
            savePrefab.SetParameter("enabled", false);

            graph.AddEdge(grid.NodeId, "geometry", extrude.NodeId, "input");
            graph.AddEdge(extrude.NodeId, "geometry", uvProject.NodeId, "input");
            graph.AddEdge(uvProject.NodeId, "geometry", savePrefab.NodeId, "input");

            AssetDatabase.CreateAsset(graph, $"{dir}/ParametricTable.asset");
        }

        private static void GenerateTerrainScatter(string dir)
        {
            var graph = ScriptableObject.CreateInstance<PCGGraphData>();
            graph.GraphName = "TerrainScatter";

            var grid = graph.AddNode("Grid", new Vector2(0, 0));
            grid.SetParameter("rows", 20);
            grid.SetParameter("columns", 20);
            grid.SetParameter("sizeX", 10.0f);
            grid.SetParameter("sizeY", 10.0f);

            var mountain = graph.AddNode("Mountain", new Vector2(250, 0));
            mountain.SetParameter("amplitude", 2.0f);
            mountain.SetParameter("frequency", 0.3f);

            var scatter = graph.AddNode("Scatter", new Vector2(500, 0));
            scatter.SetParameter("count", 50);
            scatter.SetParameter("seed", 42);

            var box = graph.AddNode("Box", new Vector2(250, 200));
            box.SetParameter("sizeX", 0.3f);
            box.SetParameter("sizeY", 0.5f);
            box.SetParameter("sizeZ", 0.3f);

            var copyToPoints = graph.AddNode("CopyToPoints", new Vector2(750, 0));

            var saveScene = graph.AddNode("SaveScene", new Vector2(1000, 0));
            saveScene.SetParameter("enabled", false);

            graph.AddEdge(grid.NodeId, "geometry", mountain.NodeId, "input");
            graph.AddEdge(mountain.NodeId, "geometry", scatter.NodeId, "input");
            graph.AddEdge(scatter.NodeId, "geometry", copyToPoints.NodeId, "targetPoints");
            graph.AddEdge(box.NodeId, "geometry", copyToPoints.NodeId, "sourceGeo");
            graph.AddEdge(copyToPoints.NodeId, "geometry", saveScene.NodeId, "input");

            AssetDatabase.CreateAsset(graph, $"{dir}/TerrainScatter.asset");
        }

        private static void GenerateBeveledWall(string dir)
        {
            var graph = ScriptableObject.CreateInstance<PCGGraphData>();
            graph.GraphName = "BeveledWall";

            var box = graph.AddNode("Box", new Vector2(0, 0));
            box.SetParameter("sizeX", 3.0f);
            box.SetParameter("sizeY", 2.0f);
            box.SetParameter("sizeZ", 0.3f);

            var bevel = graph.AddNode("PolyBevel", new Vector2(250, 0));
            bevel.SetParameter("amount", 0.05f);

            var uvProject = graph.AddNode("UVProject", new Vector2(500, 0));
            uvProject.SetParameter("mode", "Box");

            var exportMesh = graph.AddNode("ExportMesh", new Vector2(750, 0));
            exportMesh.SetParameter("enabled", false);

            graph.AddEdge(box.NodeId, "geometry", bevel.NodeId, "input");
            graph.AddEdge(bevel.NodeId, "geometry", uvProject.NodeId, "input");
            graph.AddEdge(uvProject.NodeId, "geometry", exportMesh.NodeId, "input");

            AssetDatabase.CreateAsset(graph, $"{dir}/BeveledWall.asset");
        }
    }
}
