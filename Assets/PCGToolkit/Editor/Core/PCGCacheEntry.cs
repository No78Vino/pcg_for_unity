using System;

namespace PCGToolkit.Core
{
    public enum CachePersistence
    {
        Memory,
        Disk,
        Asset
    }

    public enum CacheAssetType
    {
        Geometry,
        Mesh,
        Texture2D,
        Material
    }

    [Serializable]
    public class PCGCacheEntry
    {
        public string CacheKey;
        public string NodeType;
        public string GraphId;
        public string NodeId;
        public CacheAssetType AssetType;
        public CachePersistence Persistence;
        public long CreatedAtTicks;
        public long LastAccessedAtTicks;
        public long SizeBytes;
        public string DiskFilePath;
        public string AssetPath;
    }

    public class CacheStatistics
    {
        public int MemoryEntryCount;
        public int DiskEntryCount;
        public int AssetEntryCount;
        public long TotalMemoryBytes;
        public long TotalDiskBytes;
        public long HitCount;
        public long MissCount;
        public float HitRate => (HitCount + MissCount) > 0 ? (float)HitCount / (HitCount + MissCount) : 0f;
    }
}
