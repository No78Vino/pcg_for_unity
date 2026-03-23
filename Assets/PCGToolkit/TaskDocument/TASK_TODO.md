以下是完整的实施计划 JSON。直接复制给你的 AI Agent 即可。

```json
{
  "project": "No78Vino/pcg_for_unity",
  "title": "PCG Toolkit 本地缓存机制实现",
  "description": "为 PCG for Unity 实现完整的三层缓存机制（内存/磁盘/资产），支持 content-addressable 缓存、跨 session 持久化、自动清理、资源生命周期管理。对标 Houdini 的 File Cache / Cache / Stash SOP。",
  "tasks": [
    {
      "id": "T1",
      "title": "创建 PCGCacheEntry 数据结构",
      "type": "create",
      "file": "Assets/PCGToolkit/Editor/Core/PCGCacheEntry.cs",
      "namespace": "PCGToolkit.Core",
      "description": "缓存条目元数据类 + 枚举定义",
      "details": {
        "enums": [
          {
            "name": "CachePersistence",
            "values": ["Memory", "Disk", "Asset"],
            "description": "缓存持久化级别：Memory=仅内存, Disk=序列化到Library/, Asset=保存为Unity资产到Assets/"
          },
          {
            "name": "CacheAssetType",
            "values": ["Geometry", "Mesh", "Texture2D", "Material"],
            "description": "缓存资产类型"
          }
        ],
        "classes": [
          {
            "name": "PCGCacheEntry",
            "serializable": true,
            "fields": [
              { "name": "CacheKey", "type": "string", "description": "content-addressable 哈希键" },
              { "name": "NodeType", "type": "string", "description": "产生此缓存的节点类型" },
              { "name": "GraphId", "type": "string", "description": "所属图的标识" },
              { "name": "NodeId", "type": "string", "description": "所属节点ID" },
              { "name": "AssetType", "type": "CacheAssetType", "description": "缓存资产类型" },
              { "name": "Persistence", "type": "CachePersistence", "description": "持久化级别" },
              { "name": "CreatedAtTicks", "type": "long", "description": "创建时间 DateTime.UtcNow.Ticks" },
              { "name": "LastAccessedAtTicks", "type": "long", "description": "最后访问时间" },
              { "name": "SizeBytes", "type": "long", "description": "缓存数据大小（字节）" },
              { "name": "DiskFilePath", "type": "string", "description": "L2磁盘缓存文件路径（相对于项目根目录）" },
              { "name": "AssetPath", "type": "string", "description": "L3资产缓存的AssetDatabase路径" }
            ]
          },
          {
            "name": "CacheStatistics",
            "fields": [
              { "name": "MemoryEntryCount", "type": "int" },
              { "name": "DiskEntryCount", "type": "int" },
              { "name": "AssetEntryCount", "type": "int" },
              { "name": "TotalMemoryBytes", "type": "long" },
              { "name": "TotalDiskBytes", "type": "long" },
              { "name": "HitCount", "type": "long" },
              { "name": "MissCount", "type": "long" },
              { "name": "HitRate", "type": "float", "computed": "HitCount / (HitCount + MissCount)" }
            ]
          }
        ]
      }
    },
    {
      "id": "T2",
      "title": "创建 PCGGeometrySerializer 二进制序列化器",
      "type": "create",
      "file": "Assets/PCGToolkit/Editor/Core/PCGGeometrySerializer.cs",
      "namespace": "PCGToolkit.Core",
      "description": "将 PCGGeometry 高效序列化/反序列化为二进制格式，用于 L2 磁盘缓存。参考 geometry3Sharp 的 gSerialization.Store(DMesh3, BinaryWriter) 模式（位于 Assets/PCGToolkit/Editor/ThirdParty/geometry3Sharp/io/gSerialization.cs 第393行）。",
      "details": {
        "class": "PCGGeometrySerializer",
        "static": true,
        "constants": [
          { "name": "Version", "type": "int", "value": 1, "description": "序列化格式版本号" },
          { "name": "MagicBytes", "type": "byte[]", "value": "[0x50, 0x43, 0x47, 0x43]", "description": "文件头 'PCGC'" },
          { "name": "FileExtension", "type": "string", "value": ".pcgcache" }
        ],
        "methods": [
          {
            "name": "Serialize",
            "signature": "public static void Serialize(PCGGeometry geo, BinaryWriter writer)",
            "description": "序列化 PCGGeometry 到 BinaryWriter",
            "serialization_order": [
              "1. 写入 MagicBytes (4 bytes)",
              "2. 写入 Version (int32)",
              "3. 写入 Points: count + 每个 Vector3 的 x,y,z (float)",
              "4. 写入 Primitives: count + 每个 prim 的 length + indices (int)",
              "5. 写入 Edges: count + 每个 edge 的 2 个 int",
              "6. 写入 PointAttribs: 调用 SerializeAttributeStore()",
              "7. 写入 VertexAttribs: 调用 SerializeAttributeStore()",
              "8. 写入 PrimAttribs: 调用 SerializeAttributeStore()",
              "9. 写入 DetailAttribs: 调用 SerializeAttributeStore()",
              "10. 写入 PointGroups: 调用 SerializeGroups()",
              "11. 写入 PrimGroups: 调用 SerializeGroups()"
            ]
          },
          {
            "name": "Deserialize",
            "signature": "public static PCGGeometry Deserialize(BinaryReader reader)",
            "description": "从 BinaryReader 反序列化 PCGGeometry，验证 MagicBytes 和 Version"
          },
          {
            "name": "SerializeToFile",
            "signature": "public static long SerializeToFile(PCGGeometry geo, string filePath)",
            "description": "序列化到文件，返回文件大小（字节）。自动创建目录。"
          },
          {
            "name": "DeserializeFromFile",
            "signature": "public static PCGGeometry DeserializeFromFile(string filePath)",
            "description": "从文件反序列化，文件不存在返回 null"
          },
          {
            "name": "SerializeAttributeStore",
            "signature": "private static void SerializeAttributeStore(AttributeStore store, BinaryWriter writer)",
            "description": "序列化 AttributeStore：写入属性数量，每个属性写入 name(string) + type(AttribType as int) + values count + values。值的序列化根据 AttribType 分别处理：Float→float, Int→int, Vector2→2 floats, Vector3→3 floats, Vector4→4 floats, Color→4 floats, String→string"
          },
          {
            "name": "DeserializeAttributeStore",
            "signature": "private static AttributeStore DeserializeAttributeStore(BinaryReader reader)",
            "description": "反序列化 AttributeStore"
          },
          {
            "name": "SerializeGroups",
            "signature": "private static void SerializeGroups(Dictionary<string, HashSet<int>> groups, BinaryWriter writer)",
            "description": "序列化分组：写入组数量，每组写入 name(string) + count + indices(int[])"
          },
          {
            "name": "DeserializeGroups",
            "signature": "private static Dictionary<string, HashSet<int>> DeserializeGroups(BinaryReader reader)",
            "description": "反序列化分组"
          },
          {
            "name": "ComputeHash",
            "signature": "public static string ComputeHash(PCGGeometry geo)",
            "description": "计算 PCGGeometry 的快速哈希（用于缓存键）。使用 SHA256，但只哈希关键数据：Points.Count + Primitives.Count + 前 min(64, Points.Count) 个顶点坐标 + 所有属性名。不需要完整哈希所有数据，trade-off 速度 vs 精度。"
          }
        ],
        "references": [
          "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs - 数据结构定义",
          "Assets/PCGToolkit/Editor/Core/AttributeStore.cs - AttribType 枚举和 AttributeStore 类",
          "Assets/PCGToolkit/Editor/ThirdParty/geometry3Sharp/io/gSerialization.cs:393-456 - DMesh3 序列化参考"
        ]
      }
    },
    {
      "id": "T3",
      "title": "创建 PCGCacheManager 核心管理器",
      "type": "create",
      "file": "Assets/PCGToolkit/Editor/Core/PCGCacheManager.cs",
      "namespace": "PCGToolkit.Core",
      "description": "三层缓存的统一管理器，提供缓存读写、生命周期管理、统计、Editor菜单。使用 [InitializeOnLoad] 在 Editor 启动时初始化。",
      "details": {
        "class": "PCGCacheManager",
        "static": true,
        "attribute": "[InitializeOnLoad]",
        "constants": [
          { "name": "DiskCacheRoot", "value": "Library/PCGToolkit/Cache", "description": "L2 磁盘缓存根目录（不进版本控制，Library重建时丢失）" },
          { "name": "AssetCacheRoot", "value": "Assets/PCGToolkit/.cache", "description": "L3 资产缓存根目录（需加入 .gitignore）" },
          { "name": "ManifestFileName", "value": "manifest.json", "description": "缓存清单文件名" },
          { "name": "DefaultMaxDiskCacheMB", "value": 512, "description": "默认磁盘缓存上限 MB" },
          { "name": "DefaultMaxAgeDays", "value": 7, "description": "默认缓存过期天数" }
        ],
        "state_fields": [
          { "name": "_memoryCache", "type": "Dictionary<string, MemoryCacheEntry>", "description": "L1 内存缓存，key=cacheKey" },
          { "name": "_manifest", "type": "Dictionary<string, PCGCacheEntry>", "description": "缓存清单（从 manifest.json 加载）" },
          { "name": "_meshCache", "type": "Dictionary<string, Mesh>", "description": "内存中的 Mesh 对象缓存（用于预览，避免重复转换）" },
          { "name": "_hitCount", "type": "long" },
          { "name": "_missCount", "type": "long" },
          { "name": "_initialized", "type": "bool" }
        ],
        "inner_class": {
          "name": "MemoryCacheEntry",
          "fields": [
            { "name": "Geometry", "type": "PCGGeometry" },
            { "name": "CreatedAt", "type": "DateTime" },
            { "name": "LastAccessed", "type": "DateTime" },
            { "name": "SizeEstimate", "type": "long", "description": "估算大小 = Points.Count * 12 + Primitives sum * 4 + attribs" }
          ]
        },
        "methods": [
          {
            "name": "static PCGCacheManager()",
            "description": "静态构造函数：调用 Initialize()，注册 EditorApplication.quitting 事件保存 manifest"
          },
          {
            "name": "Initialize",
            "signature": "private static void Initialize()",
            "description": "确保目录存在（DiskCacheRoot, AssetCacheRoot），加载 manifest.json，初始化内存缓存字典"
          },
          {
            "name": "ComputeCacheKey",
            "signature": "public static string ComputeCacheKey(string nodeType, Dictionary<string, object> parameters, Dictionary<string, PCGGeometry> inputGeometries)",
            "description": "生成 content-addressable 缓存键。算法：将 nodeType + 排序后的参数键值对序列化为字符串 + 每个输入 PCGGeometry 的 PCGGeometrySerializer.ComputeHash()，拼接后取 SHA256 的前16位十六进制字符串。"
          },
          {
            "name": "TryGetGeometry",
            "signature": "public static bool TryGetGeometry(string cacheKey, out PCGGeometry geo)",
            "description": "查询缓存。先查 L1 内存缓存，命中则更新 LastAccessed 并返回 Clone()。未命中则查 L2 磁盘缓存（通过 manifest 查找文件路径，用 PCGGeometrySerializer.DeserializeFromFile 加载），加载成功则同时写入 L1。更新 _hitCount/_missCount。"
          },
          {
            "name": "PutGeometry",
            "signature": "public static void PutGeometry(string cacheKey, PCGGeometry geo, CachePersistence persistence = CachePersistence.Memory, string nodeType = null, string graphId = null, string nodeId = null)",
            "description": "写入缓存。始终写入 L1 内存缓存。如果 persistence >= Disk，同时用 PCGGeometrySerializer.SerializeToFile 写入 L2 磁盘缓存，文件路径为 DiskCacheRoot/{cacheKey}.pcgcache。更新 manifest 并保存。"
          },
          {
            "name": "GetOrCreateMesh",
            "signature": "public static Mesh GetOrCreateMesh(string cacheKey, PCGGeometry geo)",
            "description": "从 _meshCache 获取 Mesh，不存在则调用 PCGGeometryToMesh.Convert(geo) 创建并缓存。用于预览系统，避免重复转换。"
          },
          {
            "name": "InvalidateMesh",
            "signature": "public static void InvalidateMesh(string cacheKey)",
            "description": "从 _meshCache 移除指定 Mesh（DestroyImmediate），当对应的 Geometry 缓存更新时调用"
          },
          {
            "name": "CacheUnityAsset",
            "signature": "public static string CacheUnityAsset(UnityEngine.Object asset, string name, string extension)",
            "description": "将 Unity 资产（Mesh, Material, Texture2D）保存到 AssetCacheRoot/{name}_{hash}.{extension}，返回 AssetDatabase 路径。如果同名资产已存在则覆盖。调用 AssetDatabase.CreateAsset() 或 AssetDatabase.SaveAssets()。"
          },
          {
            "name": "ClearMemoryCache",
            "signature": "public static void ClearMemoryCache()",
            "description": "清空 _memoryCache 和 _meshCache（对 Mesh 调用 DestroyImmediate），重置统计计数"
          },
          {
            "name": "ClearDiskCache",
            "signature": "public static void ClearDiskCache()",
            "description": "删除 DiskCacheRoot 目录下所有 .pcgcache 文件和 manifest.json，重建空目录"
          },
          {
            "name": "ClearAssetCache",
            "signature": "public static void ClearAssetCache()",
            "description": "删除 AssetCacheRoot 目录（AssetDatabase.DeleteAsset），重建空目录，调用 AssetDatabase.Refresh()"
          },
          {
            "name": "ClearAll",
            "signature": "public static void ClearAll()",
            "description": "调用 ClearMemoryCache + ClearDiskCache + ClearAssetCache"
          },
          {
            "name": "GetStatistics",
            "signature": "public static CacheStatistics GetStatistics()",
            "description": "返回当前缓存统计信息"
          },
          {
            "name": "PurgeExpired",
            "signature": "public static int PurgeExpired(int maxAgeDays = -1)",
            "description": "清理超过 maxAgeDays 天未访问的磁盘缓存条目（默认使用 DefaultMaxAgeDays），返回清理数量"
          },
          {
            "name": "PurgeLRU",
            "signature": "public static int PurgeLRU(long maxSizeBytes = -1)",
            "description": "当磁盘缓存总大小超过 maxSizeBytes 时，按 LRU（LastAccessedAtTicks 最小的优先）删除，直到总大小低于阈值。默认使用 DefaultMaxDiskCacheMB * 1024 * 1024。返回清理数量。"
          },
          {
            "name": "SaveManifest",
            "signature": "private static void SaveManifest()",
            "description": "将 _manifest 序列化为 JSON 写入 DiskCacheRoot/manifest.json。使用 JsonUtility 或手动拼接 JSON。"
          },
          {
            "name": "LoadManifest",
            "signature": "private static void LoadManifest()",
            "description": "从 DiskCacheRoot/manifest.json 加载 _manifest。文件不存在则初始化空字典。"
          },
          {
            "name": "EstimateGeometrySize",
            "signature": "private static long EstimateGeometrySize(PCGGeometry geo)",
            "description": "估算 PCGGeometry 的内存占用：Points.Count * 12 + sum(Primitives[i].Length) * 4 + 属性值估算"
          }
        ],
        "editor_menu_items": [
          {
            "path": "PCG Toolkit/Cache/Clear Memory Cache",
            "method": "ClearMemoryCache",
            "description": "清空内存缓存"
          },
          {
            "path": "PCG Toolkit/Cache/Clear Disk Cache",
            "method": "ClearDiskCache",
            "description": "清空磁盘缓存"
          },
          {
            "path": "PCG Toolkit/Cache/Clear Asset Cache",
            "method": "ClearAssetCache",
            "description": "清空资产缓存目录"
          },
          {
            "path": "PCG Toolkit/Cache/Clear All Caches",
            "method": "ClearAll",
            "description": "清空所有缓存"
          },
          {
            "path": "PCG Toolkit/Cache/Purge Expired",
            "method": "PurgeExpired",
            "description": "清理过期缓存"
          },
          {
            "path": "PCG Toolkit/Cache/Show Statistics",
            "description": "弹出 EditorUtility.DisplayDialog 显示缓存统计信息（条目数、总大小、命中率）"
          }
        ],
        "references": [
          "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs - PCGGeometry 数据结构",
          "Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs:161-204 - Convert() 方法",
          "Assets/PCGToolkit/Editor/Core/PCGGeometryToMesh.cs:277-330 - FromMesh() 方法"
        ]
      }
    },
    {
      "id": "T4",
      "title": "修改 PCGContext 集成缓存管理器",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Core/PCGContext.cs",
      "description": "在 PCGContext 中添加缓存控制开关和节点缓存键跟踪",
      "changes": [
        {
          "action": "add_field",
          "field": "public bool UseDiskCache { get; set; } = true;",
          "description": "是否启用磁盘缓存（允许用户禁用）",
          "after_line": 26
        },
        {
          "action": "add_field",
          "field": "public Dictionary<string, string> NodeCacheKeys = new Dictionary<string, string>();",
          "description": "记录每个节点的缓存键（nodeId → cacheKey），用于增量执行时判断是否需要重新计算",
          "after_line": 27
        },
        {
          "action": "modify_method",
          "method": "ClearCache",
          "description": "ClearCache 只清除内存级数据（NodeOutputCache、Logs、Errors、NodeCacheKeys），不触碰 PCGCacheManager 的磁盘/资产缓存。新增 ClearAll() 方法调用 PCGCacheManager.ClearAll()。",
          "new_body": "public void ClearCache()\n{\n    NodeOutputCache.Clear();\n    NodeCacheKeys.Clear();\n    Logs.Clear();\n    Errors.Clear();\n}\n\npublic void ClearAllCaches()\n{\n    ClearCache();\n    PCGCacheManager.ClearAll();\n}"
        }
      ]
    },
    {
      "id": "T5",
      "title": "修改 PCGGraphExecutor 集成缓存查询/写入",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphExecutor.cs",
      "description": "在 ExecuteNode() 方法中加入缓存查询和写入逻辑",
      "changes": [
        {
          "action": "add_using",
          "value": "using System.Security.Cryptography;"
        },
        {
          "action": "modify_method",
          "method": "ExecuteNode (line 154-232)",
          "description": "在收集完 inputGeometries 和 parameters 之后（约第204行之后），执行节点之前（约第206行之前），插入缓存查询逻辑。在节点执行成功后（约第213-217行），插入缓存写入逻辑。",
          "pseudocode": [
            "// --- 在 context.CurrentNodeId = nodeData.NodeId; 之前插入 ---",
            "// 计算缓存键",
            "string cacheKey = PCGCacheManager.ComputeCacheKey(nodeData.NodeType, parameters, inputGeometries);",
            "context.NodeCacheKeys[nodeData.NodeId] = cacheKey;",
            "",
            "// 检查缓存（仅当 context.UseDiskCache 为 true 时）",
            "if (context.UseDiskCache && PCGCacheManager.TryGetGeometry(cacheKey, out var cachedGeo))",
            "{",
            "    var cachedResult = new Dictionary<string, PCGGeometry> { { \"geometry\", cachedGeo } };",
            "    _nodeOutputs[nodeData.NodeId] = cachedResult;",
            "    foreach (var kvp in cachedResult)",
            "        context.CacheOutput($\"{nodeData.NodeId}.{kvp.Key}\", kvp.Value);",
            "    return; // 跳过执行",
            "}",
            "",
            "// --- 在 result != null 的 if 块内，_nodeOutputs 赋值之后插入 ---",
            "// 写入缓存",
            "if (context.UseDiskCache)",
            "{",
            "    foreach (var kvp in result)",
            "    {",
            "        if (kvp.Value != null && kvp.Value.Points.Count > 0)",
            "            PCGCacheManager.PutGeometry(cacheKey, kvp.Value, CachePersistence.Disk,",
            "                nodeData.NodeType, graphData?.GraphName, nodeData.NodeId);",
            "    }",
            "}"
          ]
        },
        {
          "action": "modify_method",
          "method": "Execute (line 26-79)",
          "description": "在 Execute() 方法开头，不再调用 context.ClearCache()（这会清除所有内存缓存）。改为只清除 _nodeOutputs 和 Logs/Errors，保留 PCGCacheManager 的缓存。",
          "pseudocode": [
            "public void Execute(bool continueOnError = false)",
            "{",
            "    _nodeOutputs.Clear();",
            "    context.NodeOutputCache.Clear();",
            "    context.NodeCacheKeys.Clear();",
            "    context.Logs.Clear();",
            "    context.Errors.Clear();",
            "    context.ContinueOnError = continueOnError;",
            "    // ... 其余不变"
          ]
        }
      ]
    },
    {
      "id": "T6",
      "title": "修改 PCGAsyncGraphExecutor 集成缓存查询/写入",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
      "description": "在 ExecuteNodeInternal() 方法中加入与 PCGGraphExecutor 相同的缓存逻辑",
      "changes": [
        {
          "action": "modify_method",
          "method": "ExecuteNodeInternal (line 292-388)",
          "description": "在收集完 inputGeometries 和 parameters 之后（约第355行之后），执行节点之前（约第358行之前），插入缓存查询逻辑。缓存命中时直接返回 NodeExecutionResult（ElapsedMs=0, Success=true）。执行成功后写入缓存。",
          "pseudocode": [
            "// --- 在 _context.CurrentNodeId = nodeData.NodeId; 之前插入 ---",
            "string cacheKey = PCGCacheManager.ComputeCacheKey(nodeData.NodeType, parameters, inputGeometries);",
            "",
            "if (_context.UseDiskCache && PCGCacheManager.TryGetGeometry(cacheKey, out var cachedGeo))",
            "{",
            "    result.ElapsedMs = 0;",
            "    result.Success = true;",
            "    result.Outputs = new Dictionary<string, PCGGeometry> { { \"geometry\", cachedGeo } };",
            "    _nodeOutputs[nodeData.NodeId] = result.Outputs;",
            "    foreach (var kvp in result.Outputs)",
            "        _context.CacheOutput($\"{nodeData.NodeId}.{kvp.Key}\", kvp.Value);",
            "    return result;",
            "}",
            "",
            "// --- 在 outputs != null 的 if 块内插入 ---",
            "if (_context.UseDiskCache)",
            "{",
            "    foreach (var kvp in outputs)",
            "    {",
            "        if (kvp.Value != null && kvp.Value.Points.Count > 0)",
            "            PCGCacheManager.PutGeometry(cacheKey, kvp.Value, CachePersistence.Disk,",
            "                nodeData.NodeType, null, nodeData.NodeId);",
            "    }",
            "}"
          ]
        }
      ]
    },
    继续从 T7 截断处输出：

```json
    {
      "id": "T7",
      "title": "修改 PCGScenePreview 修复资源泄漏并集成缓存",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGScenePreview.cs",
      "description": "修复 InjectToScene 中 Mesh 对象泄漏问题，通过 PCGCacheManager 管理 Mesh 生命周期",
      "changes": [
        {
          "action": "add_field",
          "field": "private static List<Mesh> _injectedMeshes = new List<Mesh>();",
          "description": "跟踪注入场景的 Mesh 对象，用于清理",
          "after_line": 23
        },
        {
          "action": "modify_method",
          "method": "InjectToScene (line 50-71)",
          "description": "在清理旧对象时，同时销毁旧的 Mesh 对象。创建新 Mesh 时通过 PCGCacheManager.GetOrCreateMesh 获取。",
          "new_body": "public static void InjectToScene(PCGGeometry geo, string label = \"PCG_Preview\")\n{\n    // 清理旧对象和旧 Mesh\n    foreach (var m in _injectedMeshes)\n        if (m != null) Object.DestroyImmediate(m);\n    _injectedMeshes.Clear();\n    foreach (var old in _injectedObjects)\n        if (old != null) Object.DestroyImmediate(old);\n    _injectedObjects.Clear();\n\n    if (geo == null || geo.Points.Count == 0) return;\n\n    string cacheKey = \"scene_preview_\" + PCGGeometrySerializer.ComputeHash(geo);\n    var mesh = PCGCacheManager.GetOrCreateMesh(cacheKey, geo);\n    _injectedMeshes.Add(mesh);\n\n    var go = new GameObject(label);\n    go.hideFlags = HideFlags.DontSave;\n    go.AddComponent<MeshFilter>().sharedMesh = mesh;\n    go.AddComponent<MeshRenderer>().sharedMaterial =\n        AssetDatabase.GetBuiltinExtraResource<Material>(\"Default-Material.mat\");\n\n    _injectedObjects.Add(go);\n    Selection.activeGameObject = go;\n    SceneView.FrameLastActiveSceneView();\n    Debug.Log($\"[PCGScenePreview] Injected '{label}' to scene (verts:{mesh.vertexCount} tris:{mesh.triangles.Length / 3})\");\n}"
        },
        {
          "action": "modify_method",
          "method": "Hide (line 38-48)",
          "description": "在 Hide() 中也清理 _injectedMeshes",
          "add_lines": "foreach (var m in _injectedMeshes)\n    if (m != null) Object.DestroyImmediate(m);\n_injectedMeshes.Clear();"
        }
      ]
    },
    {
      "id": "T8",
      "title": "修改 PCGNodePreviewWindow 集成缓存",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
      "description": "预览 Mesh 通过 PCGCacheManager 管理，避免重复转换和泄漏",
      "changes": [
        {
          "action": "modify_method",
          "method": "SetPreviewData (line 28-44)",
          "description": "将直接调用 PCGGeometryToMesh.Convert() 改为通过 PCGCacheManager.GetOrCreateMesh() 获取缓存的 Mesh。不再手动 DestroyImmediate 旧 Mesh（由 CacheManager 管理生命周期）。",
          "new_body": "public void SetPreviewData(string nodeId, string displayName, PCGGeometry geometry, double executionTimeMs)\n{\n    _nodeId = nodeId;\n    _nodeDisplayName = displayName;\n    _geometry = geometry;\n    _executionTimeMs = executionTimeMs;\n\n    // 旧的 _previewMesh 不再手动销毁，由 CacheManager 管理\n    if (geometry != null && geometry.Points.Count > 0)\n    {\n        string cacheKey = \"preview_\" + nodeId + \"_\" + PCGGeometrySerializer.ComputeHash(geometry);\n        _previewMesh = PCGCacheManager.GetOrCreateMesh(cacheKey, geometry);\n    }\n    else\n    {\n        _previewMesh = null;\n    }\n\n    Repaint();\n}"
        }
      ]
    },
    {
      "id": "T9",
      "title": "修改 PCGGraphRunnerBridge 集成缓存",
      "type": "modify",
      "file": "Assets/PCGToolkit/Editor/Graph/PCGGraphRunnerBridge.cs",
      "description": "ApplyOutputToScene 中的 Mesh 通过 CacheManager 管理",
      "changes": [
        {
          "action": "modify_method",
          "method": "ApplyOutputToScene (line 61-78)",
          "description": "将直接调用 PCGGeometryToMesh.Convert(geo) 改为通过 PCGCacheManager.GetOrCreateMesh() 获取",
          "pseudocode": [
            "private static void ApplyOutputToScene(PCGGraphRunner runner, PCGGeometry geo)",
            "{",
            "    string cacheKey = \"runner_\" + runner.GetInstanceID() + \"_\" + PCGGeometrySerializer.ComputeHash(geo);",
            "    var mesh = PCGCacheManager.GetOrCreateMesh(cacheKey, geo);",
            "    // ... 其余逻辑不变，使用 mesh 赋值给 MeshFilter",
            "}"
          ]
        }
      ]
    },
    {
      "id": "T10",
      "title": "修改 Output 节点使用 AssetCache 管理中间产物",
      "type": "modify",
      "files": [
        "Assets/PCGToolkit/Editor/Nodes/Output/SavePrefabNode.cs",
        "Assets/PCGToolkit/Editor/Nodes/Output/SaveSceneNode.cs",
        "Assets/PCGToolkit/Editor/Nodes/Output/ExportMeshNode.cs",
        "Assets/PCGToolkit/Editor/Nodes/Output/AssemblePrefabNode.cs"
      ],
      "description": "Output 节点在创建中间 Mesh 资产时，通过 PCGCacheManager.CacheUnityAsset() 管理，而非直接写入最终路径旁边。这样中间产物可以被统一清理。",
      "changes": [
        {
          "action": "modify_pattern",
          "pattern": "AssetDatabase.CreateAsset(mesh, meshAssetPath);",
          "replacement_description": "将 Mesh 资产先通过 PCGCacheManager.CacheUnityAsset(mesh, meshName, \"asset\") 缓存到 .cache/ 目录，然后在最终保存时从缓存路径复制或引用。如果用户指定了明确的输出路径，则仍然写入用户路径，但同时在 CacheManager 中注册该资产。",
          "affected_files": [
            {
              "file": "SavePrefabNode.cs",
              "line": 134,
              "description": "meshAssetPath 的 Mesh 资产创建"
            },
            {
              "file": "SaveSceneNode.cs",
              "line": 138,
              "description": "meshAssetPath 的 Mesh 资产创建"
            },
            {
              "file": "ExportMeshNode.cs",
              "description": "如果有 AssetDatabase.CreateAsset 调用"
            },
            {
              "file": "AssemblePrefabNode.cs",
              "description": "如果有 AssetDatabase.CreateAsset 调用"
            }
          ]
        }
      ]
    },
    {
      "id": "T11",
      "title": "新增 CacheNode 节点（对标 Houdini File Cache SOP）",
      "type": "create",
      "file": "Assets/PCGToolkit/Editor/Nodes/Utility/CacheNode.cs",
      "namespace": "PCGToolkit.Nodes",
      "description": "显式缓存节点，用户可以在节点图中手动插入缓存点。对标 Houdini 的 File Cache / Cache SOP。",
      "details": {
        "class": "CacheNode",
        "extends": "PCGNodeBase",
        "properties": {
          "Name": "Cache",
          "DisplayName": "Cache",
          "Description": "缓存几何体到磁盘，避免上游重复计算",
          "Category": "PCGNodeCategory.Utility"
        },
        "inputs": [
          { "name": "input", "type": "Geometry", "required": true, "description": "输入几何体" },
          { "name": "mode", "type": "String", "default": "auto", "description": "缓存模式：auto（自动判断）/ always_write（总是写入）/ always_read（总是从缓存读取）/ bypass（直通不缓存）" },
          { "name": "cacheName", "type": "String", "default": "", "description": "自定义缓存名称（留空则自动生成）" }
        ],
        "outputs": [
          { "name": "geometry", "type": "Geometry" }
        ],
        "execute_logic": [
          "1. 如果 mode == 'bypass'，直接返回输入几何体",
          "2. 计算缓存键：如果 cacheName 非空则用 cacheName，否则用 ComputeCacheKey",
          "3. 如果 mode == 'always_read'，尝试从缓存读取，失败则报错",
          "4. 如果 mode == 'always_write'，执行上游并写入缓存",
          "5. 如果 mode == 'auto'，先查缓存，命中则返回缓存，未命中则写入缓存",
          "6. 写入时使用 CachePersistence.Disk",
          "7. ctx.Log 输出缓存命中/未命中信息"
        ]
      }
    },
    {
      "id": "T12",
      "title": "添加 .gitignore 规则",
      "type": "modify",
      "file": ".gitignore",
      "description": "在项目根目录的 .gitignore 中添加缓存目录排除规则",
      "changes": [
        {
          "action": "append",
          "content": "\n# PCG Toolkit cache\nAssets/PCGToolkit/.cache/\nAssets/PCGToolkit/.cache.meta\n"
        }
      ]
    },
    {
      "id": "T13",
      "title": "更新 NODE_TODO.md 和 HandBook.md 文档",
      "type": "modify",
      "files": [
        "Assets/PCGToolkit/NODE_TODO.md",
        "Assets/PCGToolkit/HandBook.md"
      ],
      "description": "更新文档记录缓存机制",
      "changes": [
        {
          "file": "Assets/PCGToolkit/NODE_TODO.md",
          "action": "update_utility_count",
          "description": "Utility 节点数从 20 更新为 21（新增 CacheNode）"
        },
        {
          "file": "Assets/PCGToolkit/HandBook.md",
          "action": "add_section",
          "description": "新增章节「缓存机制」，说明三层缓存架构、CacheNode 用法、Editor 菜单操作、.gitignore 配置"
        }
      ]
    },
    {
      "id": "T14",
      "title": "编写缓存机制测试",
      "type": "create",
      "file": "Assets/PCGToolkit/Tests/CacheTests.cs",
      "description": "缓存机制的单元测试和集成测试",
      "details": {
        "test_class": "CacheTests",
        "test_methods": [
          {
            "name": "PCGGeometrySerializer_RoundTrip",
            "description": "创建一个包含 Points、Primitives、PointAttribs(N, uv, Cd)、PrimGroups 的 PCGGeometry，序列化到 MemoryStream 再反序列化，验证所有数据一致"
          },
          {
            "name": "PCGGeometrySerializer_EmptyGeometry",
            "description": "空 PCGGeometry 的序列化/反序列化不报错"
          },
          {
            "name": "PCGGeometrySerializer_FileRoundTrip",
            "description": "序列化到临时文件再反序列化，验证数据一致，测试后清理临时文件"
          },
          {
            "name": "ComputeHash_SameInput_SameHash",
            "description": "相同的 PCGGeometry 产生相同的哈希"
          },
          {
            "name": "ComputeHash_DifferentInput_DifferentHash",
            "description": "不同的 PCGGeometry 产生不同的哈希"
          },
          {
            "name": "ComputeCacheKey_SameNodeSameParams_SameKey",
            "description": "相同节点类型+相同参数+相同输入 → 相同缓存键"
          },
          {
            "name": "ComputeCacheKey_DifferentParams_DifferentKey",
            "description": "参数不同 → 缓存键不同"
          },
          {
            "name": "CacheManager_PutAndGet_MemoryCache",
            "description": "PutGeometry(Memory) 后 TryGetGeometry 返回 true 且数据正确"
          },
          {
            "name": "CacheManager_PutAndGet_DiskCache",
            "description": "PutGeometry(Disk) 后清空内存缓存，TryGetGeometry 仍返回 true（从磁盘加载）"
          },
          {
            "name": "CacheManager_ClearMemory_DiskSurvives",
            "description": "ClearMemoryCache 后磁盘缓存仍可读取"
          },
          {
            "name": "CacheManager_ClearDisk_MemorySurvives",
            "description": "ClearDiskCache 后内存缓存仍可读取"
          },
          {
            "name": "CacheManager_ClearAll",
            "description": "ClearAll 后所有缓存均不可读取"
          },
          {
            "name": "CacheManager_GetOrCreateMesh_Cached",
            "description": "第二次调用 GetOrCreateMesh 返回同一个 Mesh 实例（引用相等）"
          },
          {
            "name": "CacheManager_Statistics",
            "description": "验证 HitCount/MissCount 正确递增"
          },
          {
            "name": "CacheNode_AutoMode_CacheHit",
            "description": "CacheNode auto 模式：第一次执行写入缓存，第二次执行命中缓存"
          },
          {
            "name": "CacheNode_BypassMode",
            "description": "CacheNode bypass 模式：直通不缓存"
          }
        ]
      }
    }
  ],
  "execution_order": [
    { "phase": 1, "tasks": ["T1", "T2"], "description": "基础数据结构和序列化器（无外部依赖）" },
    { "phase": 2, "tasks": ["T3"], "description": "核心缓存管理器（依赖 T1, T2）" },
    { "phase": 3, "tasks": ["T4", "T5", "T6"], "description": "集成到执行引擎（依赖 T3）" },
    { "phase": 4, "tasks": ["T7", "T8", "T9"], "description": "集成到预览系统（依赖 T3）" },
    { "phase": 5, "tasks": ["T10", "T11"], "description": "Output 节点改造 + CacheNode（依赖 T3）" },
    { "phase": 6, "tasks": ["T12", "T13", "T14"], "description": "文档、gitignore、测试（依赖全部）" }
  ],
  "validation_criteria": [
    "所有现有测试仍然通过（缓存机制不应破坏现有功能）",
    "CacheTests 中所有 16 个测试方法通过",
    "执行同一个图两次，第二次所有节点应命中缓存（通过 ctx.Log 确认）",
    "修改某个节点参数后重新执行，该节点及其下游应缓存未命中，上游应命中",
    "ClearAll 后重新执行，所有节点应缓存未命中",
    "Domain Reload 后（模拟：ClearMemoryCache），磁盘缓存仍可命中",
    "PCGScenePreview.InjectToScene 不再泄漏 Mesh 对象",
    "Editor 菜单 PCG Toolkit/Cache/* 所有项可正常工作",
    "Assets/PCGToolkit/.cache/ 目录被 .gitignore 排除"
  ]
}
```