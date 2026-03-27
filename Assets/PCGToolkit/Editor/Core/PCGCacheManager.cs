using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PCGToolkit.Core
{
    [InitializeOnLoad]
    public static class PCGCacheManager
    {
        public const string DiskCacheRoot = "Library/PCGToolkit/Cache";
        public const string AssetCacheRoot = "Assets/PCGToolkit/.cache";
        public const string ManifestFileName = "manifest.json";
        public const int DefaultMaxDiskCacheMB = 512;
        public const int DefaultMaxAgeDays = 7;

        private static Dictionary<string, MemoryCacheEntry> _memoryCache = new Dictionary<string, MemoryCacheEntry>();
        private static Dictionary<string, PCGCacheEntry> _manifest = new Dictionary<string, PCGCacheEntry>();
        private static Dictionary<string, Mesh> _meshCache = new Dictionary<string, Mesh>();
        private static long _hitCount;
        private static long _missCount;
        private static bool _initialized;

        private class MemoryCacheEntry
        {
            public PCGGeometry Geometry;
            public DateTime CreatedAt;
            public DateTime LastAccessed;
            public long SizeEstimate;
        }

        static PCGCacheManager()
        {
            Initialize();
            EditorApplication.quitting += SaveManifest;
        }

        private static void Initialize()
        {
            if (_initialized) return;

            if (!Directory.Exists(DiskCacheRoot))
                Directory.CreateDirectory(DiskCacheRoot);

            LoadManifest();
            _initialized = true;
        }

        public static string ComputeCacheKey(string nodeType, Dictionary<string, object> parameters,
            Dictionary<string, PCGGeometry> inputGeometries)
        {
            using (var sha = SHA256.Create())
            {
                var sb = new StringBuilder();
                sb.Append(nodeType ?? "");

                if (parameters != null)
                {
                    foreach (var kvp in parameters.OrderBy(k => k.Key))
                    {
                        sb.Append(kvp.Key);
                        sb.Append("=");
                        sb.Append(kvp.Value?.ToString() ?? "null");
                    }
                }

                if (inputGeometries != null)
                {
                    foreach (var kvp in inputGeometries.OrderBy(k => k.Key))
                    {
                        sb.Append(kvp.Key);
                        sb.Append(":");
                        sb.Append(kvp.Value?.GetContentHash() ?? "null");
                    }
                }

                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
                var result = new StringBuilder(16);
                for (int i = 0; i < 8; i++)
                    result.Append(bytes[i].ToString("x2"));
                return result.ToString();
            }
        }

        /// <summary>
        /// 获取缓存几何体（Clone 版本，返回独立副本，调用方可安全修改）
        /// 如果不需要修改，使用 TryGetGeometryReadOnly 避免 Clone 开销
        /// </summary>
        public static bool TryGetGeometry(string cacheKey, out PCGGeometry geo)
        {
            EnsureInitialized();

            // L1: memory cache
            if (_memoryCache.TryGetValue(cacheKey, out var memEntry))
            {
                memEntry.LastAccessed = DateTime.UtcNow;
                geo = memEntry.Geometry.Clone();
                _hitCount++;
                return true;
            }

            // L2: disk cache
            if (_manifest.TryGetValue(cacheKey, out var entry) && !string.IsNullOrEmpty(entry.DiskFilePath))
            {
                var loaded = PCGGeometrySerializer.DeserializeFromFile(entry.DiskFilePath);
                if (loaded != null)
                {
                    entry.LastAccessedAtTicks = DateTime.UtcNow.Ticks;

                    // Promote to L1
                    _memoryCache[cacheKey] = new MemoryCacheEntry
                    {
                        Geometry = loaded,
                        CreatedAt = new DateTime(entry.CreatedAtTicks),
                        LastAccessed = DateTime.UtcNow,
                        SizeEstimate = EstimateGeometrySize(loaded)
                    };

                    geo = loaded.Clone();
                    _hitCount++;
                    return true;
                }
            }

            _missCount++;
            geo = null;
            return false;
        }

        /// <summary>
        /// 获取缓存几何体（只读版本，返回原始引用，不 Clone）
        /// 注意: 调用方如果需要修改返回的 geo，必须自行 Clone()
        /// </summary>
        public static bool TryGetGeometryReadOnly(string cacheKey, out PCGGeometry geo)
        {
            EnsureInitialized();

            // L1: memory cache — 返回原始引用，不 Clone
            if (_memoryCache.TryGetValue(cacheKey, out var memEntry))
            {
                memEntry.LastAccessed = DateTime.UtcNow;
                geo = memEntry.Geometry;
                _hitCount++;
                return true;
            }

            // L2: disk cache — 反序列化后存入 L1 并返回引用
            if (_manifest.TryGetValue(cacheKey, out var entry) && !string.IsNullOrEmpty(entry.DiskFilePath))
            {
                var loaded = PCGGeometrySerializer.DeserializeFromFile(entry.DiskFilePath);
                if (loaded != null)
                {
                    entry.LastAccessedAtTicks = DateTime.UtcNow.Ticks;
                    _memoryCache[cacheKey] = new MemoryCacheEntry
                    {
                        Geometry = loaded,
                        CreatedAt = new DateTime(entry.CreatedAtTicks),
                        LastAccessed = DateTime.UtcNow,
                        SizeEstimate = EstimateGeometrySize(loaded)
                    };
                    geo = loaded;
                    _hitCount++;
                    return true;
                }
            }

            _missCount++;
            geo = null;
            return false;
        }

        public static void PutGeometry(string cacheKey, PCGGeometry geo,
            CachePersistence persistence = CachePersistence.Memory,
            string nodeType = null, string graphId = null, string nodeId = null)
        {
            EnsureInitialized();
            if (geo == null) return;

            // Always write to L1
            _memoryCache[cacheKey] = new MemoryCacheEntry
            {
                Geometry = geo.Clone(),
                CreatedAt = DateTime.UtcNow,
                LastAccessed = DateTime.UtcNow,
                SizeEstimate = EstimateGeometrySize(geo)
            };

            if (persistence >= CachePersistence.Disk)
            {
                string filePath = Path.Combine(DiskCacheRoot, cacheKey + PCGGeometrySerializer.FileExtension);
                long fileSize = PCGGeometrySerializer.SerializeToFile(geo, filePath);

                var entry = new PCGCacheEntry
                {
                    CacheKey = cacheKey,
                    NodeType = nodeType,
                    GraphId = graphId,
                    NodeId = nodeId,
                    AssetType = CacheAssetType.Geometry,
                    Persistence = persistence,
                    CreatedAtTicks = DateTime.UtcNow.Ticks,
                    LastAccessedAtTicks = DateTime.UtcNow.Ticks,
                    SizeBytes = fileSize,
                    DiskFilePath = filePath
                };

                _manifest[cacheKey] = entry;
                SaveManifest();
            }
        }

        public static Mesh GetOrCreateMesh(string cacheKey, PCGGeometry geo)
        {
            if (_meshCache.TryGetValue(cacheKey, out var existing) && existing != null)
                return existing;

            var mesh = PCGGeometryToMesh.Convert(geo);
            _meshCache[cacheKey] = mesh;
            return mesh;
        }

        public static void InvalidateMesh(string cacheKey)
        {
            if (_meshCache.TryGetValue(cacheKey, out var mesh))
            {
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
                _meshCache.Remove(cacheKey);
            }
        }

        public static string CacheUnityAsset(UnityEngine.Object asset, string name, string extension)
        {
            EnsureInitialized();

            if (!Directory.Exists(AssetCacheRoot))
            {
                Directory.CreateDirectory(AssetCacheRoot);
                AssetDatabase.Refresh();
            }

            string hash = asset.GetHashCode().ToString("x8");
            string fileName = $"{name}_{hash}.{extension}";
            string assetPath = Path.Combine(AssetCacheRoot, fileName);

            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) != null)
                AssetDatabase.DeleteAsset(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            return assetPath;
        }

        public static void ClearMemoryCache()
        {
            foreach (var kvp in _meshCache)
                if (kvp.Value != null) UnityEngine.Object.DestroyImmediate(kvp.Value);
            _meshCache.Clear();
            _memoryCache.Clear();
            _hitCount = 0;
            _missCount = 0;
        }

        public static void ClearDiskCache()
        {
            if (Directory.Exists(DiskCacheRoot))
            {
                foreach (var file in Directory.GetFiles(DiskCacheRoot))
                    File.Delete(file);
            }
            _manifest.Clear();
        }

        public static void ClearAssetCache()
        {
            if (AssetDatabase.IsValidFolder(AssetCacheRoot))
            {
                AssetDatabase.DeleteAsset(AssetCacheRoot);
                AssetDatabase.Refresh();
            }
        }

        public static void ClearAll()
        {
            ClearMemoryCache();
            ClearDiskCache();
            ClearAssetCache();
        }

        public static CacheStatistics GetStatistics()
        {
            EnsureInitialized();

            long totalDiskBytes = 0;
            int diskCount = 0;
            int assetCount = 0;

            foreach (var kvp in _manifest)
            {
                if (kvp.Value.Persistence == CachePersistence.Disk)
                {
                    diskCount++;
                    totalDiskBytes += kvp.Value.SizeBytes;
                }
                else if (kvp.Value.Persistence == CachePersistence.Asset)
                {
                    assetCount++;
                }
            }

            long totalMemBytes = 0;
            foreach (var kvp in _memoryCache)
                totalMemBytes += kvp.Value.SizeEstimate;

            return new CacheStatistics
            {
                MemoryEntryCount = _memoryCache.Count,
                DiskEntryCount = diskCount,
                AssetEntryCount = assetCount,
                TotalMemoryBytes = totalMemBytes,
                TotalDiskBytes = totalDiskBytes,
                HitCount = _hitCount,
                MissCount = _missCount
            };
        }

        public static int PurgeExpired(int maxAgeDays = -1)
        {
            EnsureInitialized();
            if (maxAgeDays < 0) maxAgeDays = DefaultMaxAgeDays;

            var cutoff = DateTime.UtcNow.AddDays(-maxAgeDays).Ticks;
            var toRemove = new List<string>();

            foreach (var kvp in _manifest)
            {
                if (kvp.Value.LastAccessedAtTicks < cutoff)
                    toRemove.Add(kvp.Key);
            }

            foreach (var key in toRemove)
            {
                var entry = _manifest[key];
                if (!string.IsNullOrEmpty(entry.DiskFilePath) && File.Exists(entry.DiskFilePath))
                    File.Delete(entry.DiskFilePath);
                _manifest.Remove(key);
                _memoryCache.Remove(key);
            }

            if (toRemove.Count > 0) SaveManifest();
            return toRemove.Count;
        }

        public static int PurgeLRU(long maxSizeBytes = -1)
        {
            EnsureInitialized();
            if (maxSizeBytes < 0) maxSizeBytes = (long)DefaultMaxDiskCacheMB * 1024 * 1024;

            long totalSize = _manifest.Values.Sum(e => e.SizeBytes);
            if (totalSize <= maxSizeBytes) return 0;

            var sorted = _manifest.OrderBy(kvp => kvp.Value.LastAccessedAtTicks).ToList();
            int removed = 0;

            foreach (var kvp in sorted)
            {
                if (totalSize <= maxSizeBytes) break;

                totalSize -= kvp.Value.SizeBytes;
                if (!string.IsNullOrEmpty(kvp.Value.DiskFilePath) && File.Exists(kvp.Value.DiskFilePath))
                    File.Delete(kvp.Value.DiskFilePath);
                _manifest.Remove(kvp.Key);
                _memoryCache.Remove(kvp.Key);
                removed++;
            }

            if (removed > 0) SaveManifest();
            return removed;
        }

        private static void SaveManifest()
        {
            try
            {
                if (!Directory.Exists(DiskCacheRoot))
                    Directory.CreateDirectory(DiskCacheRoot);

                string path = Path.Combine(DiskCacheRoot, ManifestFileName);
                var wrapper = new ManifestWrapper { Entries = _manifest.Values.ToList() };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PCGCacheManager] Failed to save manifest: {e.Message}");
            }
        }

        private static void LoadManifest()
        {
            _manifest = new Dictionary<string, PCGCacheEntry>();
            string path = Path.Combine(DiskCacheRoot, ManifestFileName);

            if (!File.Exists(path)) return;

            try
            {
                string json = File.ReadAllText(path);
                var wrapper = JsonUtility.FromJson<ManifestWrapper>(json);
                if (wrapper?.Entries != null)
                {
                    foreach (var entry in wrapper.Entries)
                    {
                        if (!string.IsNullOrEmpty(entry.CacheKey))
                            _manifest[entry.CacheKey] = entry;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[PCGCacheManager] Failed to load manifest: {e.Message}");
            }
        }

        private static long EstimateGeometrySize(PCGGeometry geo)
        {
            if (geo == null) return 0;
            long size = geo.Points.Count * 12L; // 3 floats * 4 bytes
            foreach (var prim in geo.Primitives)
                size += prim.Length * 4L;
            // Rough attribute estimate
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
                size += attr.Values.Count * 16L;
            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
                size += attr.Values.Count * 16L;
            return size;
        }

        private static void EnsureInitialized()
        {
            if (!_initialized) Initialize();
        }

        // JSON serialization wrapper for manifest
        [Serializable]
        private class ManifestWrapper
        {
            public List<PCGCacheEntry> Entries = new List<PCGCacheEntry>();
        }

        // ---- Editor Menu Items ----

        [MenuItem("PCG Toolkit/Cache/Clear Memory Cache")]
        private static void MenuClearMemoryCache()
        {
            ClearMemoryCache();
            Debug.Log("[PCGCacheManager] Memory cache cleared.");
        }

        [MenuItem("PCG Toolkit/Cache/Clear Disk Cache")]
        private static void MenuClearDiskCache()
        {
            ClearDiskCache();
            Debug.Log("[PCGCacheManager] Disk cache cleared.");
        }

        [MenuItem("PCG Toolkit/Cache/Clear Asset Cache")]
        private static void MenuClearAssetCache()
        {
            ClearAssetCache();
            Debug.Log("[PCGCacheManager] Asset cache cleared.");
        }

        [MenuItem("PCG Toolkit/Cache/Clear All Caches")]
        private static void MenuClearAll()
        {
            ClearAll();
            Debug.Log("[PCGCacheManager] All caches cleared.");
        }

        [MenuItem("PCG Toolkit/Cache/Purge Expired")]
        private static void MenuPurgeExpired()
        {
            int count = PurgeExpired();
            Debug.Log($"[PCGCacheManager] Purged {count} expired entries.");
        }

        [MenuItem("PCG Toolkit/Cache/Show Statistics")]
        private static void MenuShowStatistics()
        {
            var stats = GetStatistics();
            string msg = $"Memory Entries: {stats.MemoryEntryCount}\n" +
                         $"Disk Entries: {stats.DiskEntryCount}\n" +
                         $"Asset Entries: {stats.AssetEntryCount}\n" +
                         $"Memory Size: {stats.TotalMemoryBytes / 1024f:F1} KB\n" +
                         $"Disk Size: {stats.TotalDiskBytes / 1024f:F1} KB\n" +
                         $"Hit Count: {stats.HitCount}\n" +
                         $"Miss Count: {stats.MissCount}\n" +
                         $"Hit Rate: {stats.HitRate:P1}";
            EditorUtility.DisplayDialog("PCG Cache Statistics", msg, "OK");
        }
    }
}
