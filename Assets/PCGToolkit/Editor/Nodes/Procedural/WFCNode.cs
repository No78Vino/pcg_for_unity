using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Procedural
{
    /// <summary>
    /// 波函数坍缩（Wave Function Collapse）程序化生成
    /// </summary>
    public class WFCNode : PCGNodeBase
    {
        public override string Name => "WFC";
        public override string DisplayName => "WFC (Wave Function Collapse)";
        public override string Description => "使用波函数坍缩算法进行程序化内容生成";
        public override PCGNodeCategory Category => PCGNodeCategory.Procedural;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("gridSizeX", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size X", "网格 X 方向大小", 10),
            new PCGParamSchema("gridSizeY", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size Y", "网格 Y 方向大小", 10),
            new PCGParamSchema("gridSizeZ", PCGPortDirection.Input, PCGPortType.Int,
                "Grid Size Z", "网格 Z 方向大小（2D 时为 1）", 1),
            new PCGParamSchema("tileCount", PCGPortDirection.Input, PCGPortType.Int,
                "Tile Count", "瓦片种类数量", 4),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("tileSize", PCGPortDirection.Input, PCGPortType.Float,
                "Tile Size", "瓦片尺寸", 1.0f),
            new PCGParamSchema("maxAttempts", PCGPortDirection.Input, PCGPortType.Int,
                "Max Attempts", "最大尝试次数", 10),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "生成的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            int gridX = GetParamInt(parameters, "gridSizeX", 10);
            int gridY = GetParamInt(parameters, "gridSizeY", 10);
            int gridZ = GetParamInt(parameters, "gridSizeZ", 1);
            int tileCount = GetParamInt(parameters, "tileCount", 4);
            int seed = GetParamInt(parameters, "seed", 0);
            float tileSize = GetParamFloat(parameters, "tileSize", 1.0f);
            int maxAttempts = GetParamInt(parameters, "maxAttempts", 10);

            var rng = new System.Random(seed);

            // 简化的 WFC 实现
            // 1. 定义瓦片（每个瓦片有颜色/类型）
            var tiles = new List<int>();
            for (int i = 0; i < tileCount; i++) tiles.Add(i);

            // 2. 定义邻接规则（简化：相邻瓦片类型差 <= 1）
            // 完整实现需要从输入数据或配置读取

            // 3. 初始化波函数（每个格子可以是任意瓦片）
            int totalCells = gridX * gridY * gridZ;
            var superposition = new List<HashSet<int>>();
            for (int i = 0; i < totalCells; i++)
            {
                superposition.Add(new HashSet<int>(tiles));
            }

            // 4. 观察与传播
            var collapsed = new int[totalCells];
            for (int i = 0; i < totalCells; i++) collapsed[i] = -1;

            bool success = false;
            for (int attempt = 0; attempt < maxAttempts && !success; attempt++)
            {
                // 重置
                for (int i = 0; i < totalCells; i++)
                {
                    superposition[i] = new HashSet<int>(tiles);
                    collapsed[i] = -1;
                }

                success = RunWFC(rng, superposition, collapsed, gridX, gridY, gridZ, tileCount);
            }

            // 5. 生成几何体
            var geo = new PCGGeometry();
            var points = new List<Vector3>();
            var primitives = new List<int[]>();

            for (int z = 0; z < gridZ; z++)
            {
                for (int y = 0; y < gridY; y++)
                {
                    for (int x = 0; x < gridX; x++)
                    {
                        int idx = x + y * gridX + z * gridX * gridY;
                        int tileType = collapsed[idx];

                        if (tileType < 0) continue;

                        // 为每个瓦片生成一个立方体
                        Vector3 basePos = new Vector3(x, z, y) * tileSize;
                        int baseIdx = points.Count;

                        // 8 个顶点
                        points.Add(basePos);
                        points.Add(basePos + new Vector3(tileSize, 0, 0));
                        points.Add(basePos + new Vector3(tileSize, 0, tileSize));
                        points.Add(basePos + new Vector3(0, 0, tileSize));
                        points.Add(basePos + new Vector3(0, tileSize, 0));
                        points.Add(basePos + new Vector3(tileSize, tileSize, 0));
                        points.Add(basePos + new Vector3(tileSize, tileSize, tileSize));
                        points.Add(basePos + new Vector3(0, tileSize, tileSize));

                        // 6 个面（每个面 2 个三角形）
                        // 底面
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 2, baseIdx + 1 });
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 3, baseIdx + 2 });
                        // 顶面
                        primitives.Add(new int[] { baseIdx + 4, baseIdx + 5, baseIdx + 6 });
                        primitives.Add(new int[] { baseIdx + 4, baseIdx + 6, baseIdx + 7 });
                        // 前面
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 1, baseIdx + 5 });
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 5, baseIdx + 4 });
                        // 后面
                        primitives.Add(new int[] { baseIdx + 2, baseIdx + 3, baseIdx + 7 });
                        primitives.Add(new int[] { baseIdx + 2, baseIdx + 7, baseIdx + 6 });
                        // 左面
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 4, baseIdx + 7 });
                        primitives.Add(new int[] { baseIdx + 0, baseIdx + 7, baseIdx + 3 });
                        // 右面
                        primitives.Add(new int[] { baseIdx + 1, baseIdx + 2, baseIdx + 6 });
                        primitives.Add(new int[] { baseIdx + 1, baseIdx + 6, baseIdx + 5 });

                        // 为每种瓦片类型创建 Prim Group
                        string tileGroupName = $"tile_{tileType}";
                        if (!geo.PrimGroups.ContainsKey(tileGroupName))
                            geo.PrimGroups[tileGroupName] = new HashSet<int>();
                        for (int pi = primitives.Count - 12; pi < primitives.Count; pi++)
                            geo.PrimGroups[tileGroupName].Add(pi);
                    }
                }
            }

            geo.Points = points;
            geo.Primitives = primitives;

            ctx.Log($"WFC: grid=({gridX}, {gridY}, {gridZ}), tiles={tileCount}, success={success}, output={points.Count}pts");
            return SingleOutput("geometry", geo);
        }

        private bool RunWFC(System.Random rng, List<HashSet<int>> superposition, int[] collapsed, int gx, int gy, int gz, int tileCount)
        {
            int totalCells = gx * gy * gz;

            for (int step = 0; step < totalCells; step++)
            {
                // 找到熵最小的格子
                int minIdx = -1;
                int minEntropy = int.MaxValue;

                for (int i = 0; i < totalCells; i++)
                {
                    if (collapsed[i] >= 0) continue;

                    int entropy = superposition[i].Count;
                    if (entropy < minEntropy)
                    {
                        minEntropy = entropy;
                        minIdx = i;
                    }
                }

                if (minIdx < 0) break; // 全部坍缩完成

                if (minEntropy == 0) return false; // 矛盾，失败

                // 随机选择一个可能的瓦片
                var options = new List<int>(superposition[minIdx]);
                int choice = options[rng.Next(options.Count)];
                collapsed[minIdx] = choice;
                superposition[minIdx].Clear();
                superposition[minIdx].Add(choice);

                // 传播约束
                var queue = new Queue<int>();
                queue.Enqueue(minIdx);

                while (queue.Count > 0)
                {
                    int current = queue.Dequeue();
                    int cx = current % gx;
                    int cy = (current / gx) % gy;
                    int cz = current / (gx * gy);

                    // 检查邻居
                    var neighbors = new[] {
                        (cx - 1, cy, cz), (cx + 1, cy, cz),
                        (cx, cy - 1, cz), (cx, cy + 1, cz),
                        (cx, cy, cz - 1), (cx, cy, cz + 1)
                    };

                    foreach (var (nx, ny, nz) in neighbors)
                    {
                        if (nx < 0 || nx >= gx || ny < 0 || ny >= gy || nz < 0 || nz >= gz) continue;

                        int nIdx = nx + ny * gx + nz * gx * gy;
                        if (collapsed[nIdx] >= 0) continue;

                        // 简化规则：相邻瓦片类型差 <= 1
                        var validNeighbors = new HashSet<int>();
                        foreach (int t in superposition[current])
                        {
                            foreach (int nt in superposition[nIdx])
                            {
                                if (Mathf.Abs(t - nt) <= 1)
                                    validNeighbors.Add(nt);
                            }
                        }

                        // 更新邻居的可能性
                        var newPossibilities = new HashSet<int>();
                        foreach (int t in superposition[nIdx])
                        {
                            if (validNeighbors.Contains(t))
                                newPossibilities.Add(t);
                        }

                        if (newPossibilities.Count < superposition[nIdx].Count)
                        {
                            superposition[nIdx] = newPossibilities;
                            queue.Enqueue(nIdx);
                        }

                        if (newPossibilities.Count == 0)
                            return false;
                    }
                }
            }

            return true;
        }
    }
}