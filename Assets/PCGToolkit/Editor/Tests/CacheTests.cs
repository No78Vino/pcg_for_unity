using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PCGToolkit.Core;
using PCGToolkit.Nodes.Utility;
using UnityEngine;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class CacheTests : NodeTestBase
    {
        private string _tempDir;

        [SetUp]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "PCGCacheTests_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            PCGCacheManager.ClearMemoryCache();
        }

        [TearDown]
        public void Teardown()
        {
            PCGCacheManager.ClearMemoryCache();
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        private PCGGeometry CreateTestGeometry()
        {
            var geo = new PCGGeometry();
            geo.Points.Add(new Vector3(0, 0, 0));
            geo.Points.Add(new Vector3(1, 0, 0));
            geo.Points.Add(new Vector3(0, 1, 0));
            geo.Points.Add(new Vector3(1, 1, 0));
            geo.Primitives.Add(new int[] { 0, 1, 2 });
            geo.Primitives.Add(new int[] { 1, 3, 2 });

            var nAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3);
            nAttr.Values.Add(new Vector3(0, 0, 1));
            nAttr.Values.Add(new Vector3(0, 0, 1));
            nAttr.Values.Add(new Vector3(0, 0, 1));
            nAttr.Values.Add(new Vector3(0, 0, 1));

            var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2);
            uvAttr.Values.Add(new Vector2(0, 0));
            uvAttr.Values.Add(new Vector2(1, 0));
            uvAttr.Values.Add(new Vector2(0, 1));
            uvAttr.Values.Add(new Vector2(1, 1));

            var cdAttr = geo.PointAttribs.CreateAttribute("Cd", AttribType.Color);
            cdAttr.Values.Add(Color.red);
            cdAttr.Values.Add(Color.green);
            cdAttr.Values.Add(Color.blue);
            cdAttr.Values.Add(Color.white);

            geo.PrimGroups["top"] = new HashSet<int> { 0 };
            geo.PrimGroups["bottom"] = new HashSet<int> { 1 };

            return geo;
        }

        [Test]
        public void PCGGeometrySerializer_RoundTrip()
        {
            var original = CreateTestGeometry();

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                    PCGGeometrySerializer.Serialize(original, writer);

                ms.Position = 0;
                using (var reader = new BinaryReader(ms))
                {
                    var result = PCGGeometrySerializer.Deserialize(reader);

                    Assert.AreEqual(original.Points.Count, result.Points.Count);
                    Assert.AreEqual(original.Primitives.Count, result.Primitives.Count);

                    for (int i = 0; i < original.Points.Count; i++)
                        Assert.AreEqual(original.Points[i], result.Points[i]);

                    for (int i = 0; i < original.Primitives.Count; i++)
                        Assert.AreEqual(original.Primitives[i], result.Primitives[i]);

                    var origN = original.PointAttribs.GetAttribute("N");
                    var resultN = result.PointAttribs.GetAttribute("N");
                    Assert.IsNotNull(resultN);
                    Assert.AreEqual(origN.Values.Count, resultN.Values.Count);

                    var origUv = original.PointAttribs.GetAttribute("uv");
                    var resultUv = result.PointAttribs.GetAttribute("uv");
                    Assert.IsNotNull(resultUv);
                    Assert.AreEqual(origUv.Values.Count, resultUv.Values.Count);

                    Assert.AreEqual(original.PrimGroups.Count, result.PrimGroups.Count);
                    Assert.IsTrue(result.PrimGroups.ContainsKey("top"));
                    Assert.IsTrue(result.PrimGroups.ContainsKey("bottom"));
                }
            }
        }

        [Test]
        public void PCGGeometrySerializer_EmptyGeometry()
        {
            var empty = new PCGGeometry();

            using (var ms = new MemoryStream())
            {
                using (var writer = new BinaryWriter(ms, System.Text.Encoding.UTF8, true))
                    PCGGeometrySerializer.Serialize(empty, writer);

                ms.Position = 0;
                using (var reader = new BinaryReader(ms))
                {
                    var result = PCGGeometrySerializer.Deserialize(reader);
                    Assert.AreEqual(0, result.Points.Count);
                    Assert.AreEqual(0, result.Primitives.Count);
                }
            }
        }

        [Test]
        public void PCGGeometrySerializer_FileRoundTrip()
        {
            var original = CreateTestGeometry();
            string filePath = Path.Combine(_tempDir, "test.pcgcache");

            long size = PCGGeometrySerializer.SerializeToFile(original, filePath);
            Assert.Greater(size, 0);
            Assert.IsTrue(File.Exists(filePath));

            var result = PCGGeometrySerializer.DeserializeFromFile(filePath);
            Assert.IsNotNull(result);
            Assert.AreEqual(original.Points.Count, result.Points.Count);
            Assert.AreEqual(original.Primitives.Count, result.Primitives.Count);
        }

        [Test]
        public void ComputeHash_SameInput_SameHash()
        {
            var geo1 = CreateTestGeometry();
            var geo2 = CreateTestGeometry();

            string hash1 = PCGGeometrySerializer.ComputeHash(geo1);
            string hash2 = PCGGeometrySerializer.ComputeHash(geo2);

            Assert.AreEqual(hash1, hash2);
        }

        [Test]
        public void ComputeHash_DifferentInput_DifferentHash()
        {
            var geo1 = CreateTestGeometry();
            var geo2 = CreateTestGeometry();
            geo2.Points.Add(new Vector3(2, 2, 2));

            string hash1 = PCGGeometrySerializer.ComputeHash(geo1);
            string hash2 = PCGGeometrySerializer.ComputeHash(geo2);

            Assert.AreNotEqual(hash1, hash2);
        }

        [Test]
        public void ComputeCacheKey_SameNodeSameParams_SameKey()
        {
            var geo = CreateTestGeometry();
            var inputs = new Dictionary<string, PCGGeometry> { { "input", geo } };
            var params1 = new Dictionary<string, object> { { "scale", 1.0f } };
            var params2 = new Dictionary<string, object> { { "scale", 1.0f } };

            string key1 = PCGCacheManager.ComputeCacheKey("TestNode", params1, inputs);
            string key2 = PCGCacheManager.ComputeCacheKey("TestNode", params2, inputs);

            Assert.AreEqual(key1, key2);
        }

        [Test]
        public void ComputeCacheKey_DifferentParams_DifferentKey()
        {
            var geo = CreateTestGeometry();
            var inputs = new Dictionary<string, PCGGeometry> { { "input", geo } };
            var params1 = new Dictionary<string, object> { { "scale", 1.0f } };
            var params2 = new Dictionary<string, object> { { "scale", 2.0f } };

            string key1 = PCGCacheManager.ComputeCacheKey("TestNode", params1, inputs);
            string key2 = PCGCacheManager.ComputeCacheKey("TestNode", params2, inputs);

            Assert.AreNotEqual(key1, key2);
        }

        [Test]
        public void CacheManager_PutAndGet_MemoryCache()
        {
            var geo = CreateTestGeometry();
            string key = "test_mem_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            PCGCacheManager.PutGeometry(key, geo, CachePersistence.Memory);

            bool found = PCGCacheManager.TryGetGeometry(key, out var result);
            Assert.IsTrue(found);
            Assert.IsNotNull(result);
            Assert.AreEqual(geo.Points.Count, result.Points.Count);
        }

        [Test]
        public void CacheManager_PutAndGet_DiskCache()
        {
            var geo = CreateTestGeometry();
            string key = "test_disk_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            PCGCacheManager.PutGeometry(key, geo, CachePersistence.Disk);
            PCGCacheManager.ClearMemoryCache();

            bool found = PCGCacheManager.TryGetGeometry(key, out var result);
            Assert.IsTrue(found);
            Assert.IsNotNull(result);
            Assert.AreEqual(geo.Points.Count, result.Points.Count);

            // Cleanup disk
            string filePath = Path.Combine("Library/PCGToolkit/Cache", key + ".pcgcache");
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        [Test]
        public void CacheManager_ClearMemory_DiskSurvives()
        {
            var geo = CreateTestGeometry();
            string key = "test_survive_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            PCGCacheManager.PutGeometry(key, geo, CachePersistence.Disk);
            PCGCacheManager.ClearMemoryCache();

            bool found = PCGCacheManager.TryGetGeometry(key, out var result);
            Assert.IsTrue(found);

            // Cleanup
            string filePath = Path.Combine("Library/PCGToolkit/Cache", key + ".pcgcache");
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        [Test]
        public void CacheManager_ClearAll()
        {
            var geo = CreateTestGeometry();
            string key = "test_clearall_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            PCGCacheManager.PutGeometry(key, geo, CachePersistence.Disk);
            PCGCacheManager.ClearAll();

            bool found = PCGCacheManager.TryGetGeometry(key, out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void CacheManager_GetOrCreateMesh_Cached()
        {
            var geo = CreateTestGeometry();
            string key = "test_mesh_cache";

            var mesh1 = PCGCacheManager.GetOrCreateMesh(key, geo);
            var mesh2 = PCGCacheManager.GetOrCreateMesh(key, geo);

            Assert.AreSame(mesh1, mesh2);

            PCGCacheManager.InvalidateMesh(key);
        }

        [Test]
        public void CacheManager_Statistics()
        {
            PCGCacheManager.ClearAll();

            string key = "test_stats_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

            // Miss
            PCGCacheManager.TryGetGeometry(key, out _);

            // Put + Hit
            var geo = CreateTestGeometry();
            PCGCacheManager.PutGeometry(key, geo, CachePersistence.Memory);
            PCGCacheManager.TryGetGeometry(key, out _);

            var stats = PCGCacheManager.GetStatistics();
            Assert.GreaterOrEqual(stats.HitCount, 1);
            Assert.GreaterOrEqual(stats.MissCount, 1);
            Assert.Greater(stats.MemoryEntryCount, 0);
        }

        [Test]
        public void CacheNode_AutoMode_CacheHit()
        {
            var geo = CreateTestGeometry();
            var inputs = new Dictionary<string, PCGGeometry> { { "input", geo } };
            var parameters = new Dictionary<string, object> { { "mode", "auto" }, { "cacheName", "test_auto_node" } };

            var node = new CacheNode();
            var ctx = CreateContext();

            // First execution: cache miss, write
            var result1 = node.Execute(ctx, inputs, parameters);
            Assert.IsNotNull(result1["geometry"]);

            // Second execution: cache hit
            var result2 = node.Execute(ctx, inputs, parameters);
            Assert.IsNotNull(result2["geometry"]);
            Assert.AreEqual(geo.Points.Count, result2["geometry"].Points.Count);
        }

        [Test]
        public void CacheNode_BypassMode()
        {
            var geo = CreateTestGeometry();
            var inputs = new Dictionary<string, PCGGeometry> { { "input", geo } };
            var parameters = new Dictionary<string, object> { { "mode", "bypass" }, { "cacheName", "" } };

            var node = new CacheNode();
            var ctx = CreateContext();

            var result = node.Execute(ctx, inputs, parameters);
            Assert.IsNotNull(result["geometry"]);
            Assert.AreEqual(geo.Points.Count, result["geometry"].Points.Count);
        }
    }
}
