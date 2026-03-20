using System.Collections.Generic;
using PCGToolkit.Core;
using UnityEngine;

namespace PCGToolkit.Nodes.Deform
{
    /// <summary>
    /// 噪声变形（对标 Houdini Mountain SOP）
    /// </summary>
    public class MountainNode : PCGNodeBase
    {
        public override string Name => "Mountain";
        public override string DisplayName => "Mountain";
        public override string Description => "用噪声函数对几何体进行变形（产生山脉/地形效果）";
        public override PCGNodeCategory Category => PCGNodeCategory.Deform;

        public override PCGParamSchema[] Inputs => new[]
        {
            new PCGParamSchema("input", PCGPortDirection.Input, PCGPortType.Geometry,
                "Input", "输入几何体", null, required: true),
            new PCGParamSchema("height", PCGPortDirection.Input, PCGPortType.Float,
                "Height", "噪声高度/振幅", 1.0f),
            new PCGParamSchema("frequency", PCGPortDirection.Input, PCGPortType.Float,
                "Frequency", "噪声频率", 1.0f),
            new PCGParamSchema("octaves", PCGPortDirection.Input, PCGPortType.Int,
                "Octaves", "分形叠加层数", 4),
            new PCGParamSchema("lacunarity", PCGPortDirection.Input, PCGPortType.Float,
                "Lacunarity", "频率递增倍数", 2.0f),
            new PCGParamSchema("persistence", PCGPortDirection.Input, PCGPortType.Float,
                "Persistence", "振幅递减比例", 0.5f),
            new PCGParamSchema("seed", PCGPortDirection.Input, PCGPortType.Int,
                "Seed", "随机种子", 0),
            new PCGParamSchema("noiseType", PCGPortDirection.Input, PCGPortType.String,
                "Noise Type", "噪声类型（perlin/simplex/value）", "perlin"),
        };

        public override PCGParamSchema[] Outputs => new[]
        {
            new PCGParamSchema("geometry", PCGPortDirection.Output, PCGPortType.Geometry,
                "Geometry", "变形后的几何体"),
        };

        public override Dictionary<string, PCGGeometry> Execute(
            PCGContext ctx,
            Dictionary<string, PCGGeometry> inputGeometries,
            Dictionary<string, object> parameters)
        {
            var geo = GetInputGeometry(inputGeometries, "input").Clone();

            if (geo.Points.Count == 0)
            {
                ctx.LogWarning("Mountain: 输入几何体为空");
                return SingleOutput("geometry", geo);
            }

            float height = GetParamFloat(parameters, "height", 1.0f);
            float frequency = GetParamFloat(parameters, "frequency", 1.0f);
            int octaves = GetParamInt(parameters, "octaves", 4);
            float lacunarity = GetParamFloat(parameters, "lacunarity", 2.0f);
            float persistence = GetParamFloat(parameters, "persistence", 0.5f);
            int seed = GetParamInt(parameters, "seed", 0);
            string noiseType = GetParamString(parameters, "noiseType", "perlin").ToLower();

            // 设置随机种子
            UnityEngine.Random.InitState(seed);
            Vector3 offset = new Vector3(
                UnityEngine.Random.value * 1000f,
                UnityEngine.Random.value * 1000f,
                UnityEngine.Random.value * 1000f
            );

            // 预计算顶点法线
            Vector3[] vertexNormals = new Vector3[geo.Points.Count];

            // 优先从 PointAttribs 读取 "N"
            var normalAttr = geo.PointAttribs.GetAttribute("N");
            if (normalAttr != null && normalAttr.Values.Count == geo.Points.Count)
            {
                for (int i = 0; i < geo.Points.Count; i++)
                {
                    vertexNormals[i] = (normalAttr.Values[i] is Vector3 n) ? n : Vector3.up;
                }
            }
            else if (geo.Primitives.Count > 0)
            {
                // 从相邻面计算顶点法线（面积加权平均）
                for (int i = 0; i < geo.Points.Count; i++)
                    vertexNormals[i] = Vector3.zero;

                foreach (var prim in geo.Primitives)
                {
                    if (prim.Length < 3) continue;
                    Vector3 v0 = geo.Points[prim[0]];
                    Vector3 v1 = geo.Points[prim[1]];
                    Vector3 v2 = geo.Points[prim[2]];
                    Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0); // 未归一化 = 面积加权
                    
                    foreach (int idx in prim)
                    {
                        vertexNormals[idx] += faceNormal;
                    }
                }

                for (int i = 0; i < geo.Points.Count; i++)
                {
                    if (vertexNormals[i].sqrMagnitude > 0.0001f)
                        vertexNormals[i] = vertexNormals[i].normalized;
                    else
                        vertexNormals[i] = Vector3.up;
                }
            }
            else
            {
                // 无面数据，默认 Y 轴
                for (int i = 0; i < geo.Points.Count; i++)
                    vertexNormals[i] = Vector3.up;
            }

            // 对每个点应用噪声位移
            for (int i = 0; i < geo.Points.Count; i++)
            {
                Vector3 p = geo.Points[i];
                Vector3 samplePos = (p + offset) * frequency;

                // 分形噪声（fBm）
                float noiseValue = 0f;
                float amplitude = 1f;
                float maxAmplitude = 0f;
                float currentFreq = 1f;

                for (int o = 0; o < octaves; o++)
                {
                    float n = 0f;
                    Vector3 sampleAt = samplePos * currentFreq;

                    switch (noiseType)
                    {
                        case "simplex":
                            // Unity 没有内置 Simplex，用 Perlin 近似
                            n = Perlin3D(sampleAt);
                            break;
                        case "value":
                            // Value 噪声近似
                            n = ValueNoise3D(sampleAt);
                            break;
                        default: // perlin
                            n = Perlin3D(sampleAt);
                            break;
                    }

                    noiseValue += n * amplitude;
                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    currentFreq *= lacunarity;
                }

                noiseValue /= maxAmplitude;
                noiseValue = noiseValue * 2f - 1f; // 映射到 -1 ~ 1

                // 沿法线方向偏移
                geo.Points[i] = p + vertexNormals[i] * noiseValue * height;
            }

            ctx.Log($"Mountain: height={height}, frequency={frequency}, octaves={octaves}, noiseType={noiseType}");
            return SingleOutput("geometry", geo);
        }

        private float Perlin3D(Vector3 p)
        {
            // 3D Perlin 噪声近似（使用 Unity 的 2D Perlin）
            float xy = Mathf.PerlinNoise(p.x, p.y);
            float yz = Mathf.PerlinNoise(p.y, p.z);
            float xz = Mathf.PerlinNoise(p.x, p.z);
            return (xy + yz + xz) / 3f;
        }

        private float ValueNoise3D(Vector3 p)
        {
            // 简单 Value 噪声
            int xi = Mathf.FloorToInt(p.x);
            int yi = Mathf.FloorToInt(p.y);
            int zi = Mathf.FloorToInt(p.z);
            float xf = p.x - xi;
            float yf = p.y - yi;
            float zf = p.z - zi;

            // 平滑插值
            xf = xf * xf * (3f - 2f * xf);
            yf = yf * yf * (3f - 2f * yf);
            zf = zf * zf * (3f - 2f * zf);

            // 哈希函数
            float n000 = Hash(xi, yi, zi);
            float n100 = Hash(xi + 1, yi, zi);
            float n010 = Hash(xi, yi + 1, zi);
            float n110 = Hash(xi + 1, yi + 1, zi);
            float n001 = Hash(xi, yi, zi + 1);
            float n101 = Hash(xi + 1, yi, zi + 1);
            float n011 = Hash(xi, yi + 1, zi + 1);
            float n111 = Hash(xi + 1, yi + 1, zi + 1);

            float x00 = Mathf.Lerp(n000, n100, xf);
            float x10 = Mathf.Lerp(n010, n110, xf);
            float x01 = Mathf.Lerp(n001, n101, xf);
            float x11 = Mathf.Lerp(n011, n111, xf);

            float y0 = Mathf.Lerp(x00, x10, yf);
            float y1 = Mathf.Lerp(x01, x11, yf);

            return Mathf.Lerp(y0, y1, zf);
        }

        private float Hash(int x, int y, int z)
        {
            // 简单哈希函数
            int n = x + y * 57 + z * 113;
            n = (n << 13) ^ n;
            n = n * (n * n * 15731 + 789221) + 1376312589;
            return (n & 0x7fffffff) / 2147483648f;
        }
    }
}
