以下是第11轮迭代的 JSON 任务方案文档：

```json
{
  "iteration": 11,
  "title": "Debug框架扩展 + 节点属性/分组传递Bug全面修复",
  "repository": "No78Vino/pcg_for_unity",
  "branch": "main",
  "description": "用户在调试中发现MergeNode等节点执行后geometry数据（属性、分组等）丢失。经全面代码审查，发现大量节点存在属性（Attribs）和分组（Groups）未正确传递/同步的系统性bug。本轮迭代分两个阶段：A阶段扩展Debug框架使每个节点可查看完整geometry数据并自动验证一致性；B阶段修复所有节点的属性/分组传递bug。",
  "core_data_model": {
    "file": "Assets/PCGToolkit/Editor/Core/PCGGeometry.cs",
    "fields": {
      "Points": "List<Vector3> — 顶点位置列表",
      "Primitives": "List<int[]> — 面（每个元素是该面的顶点索引数组）",
      "Edges": "List<int[]> — 边（按需构建）",
      "PointAttribs": "AttributeStore — 点属性（Values.Count 应 == Points.Count）",
      "VertexAttribs": "AttributeStore — 顶点属性（per-face-vertex）",
      "PrimAttribs": "AttributeStore — 面属性（Values.Count 应 == Primitives.Count）",
      "DetailAttribs": "AttributeStore — 全局属性（Values.Count 应 == 1）",
      "PointGroups": "Dictionary<string, HashSet<int>> — 点分组（索引应 < Points.Count）",
      "PrimGroups": "Dictionary<string, HashSet<int>> — 面分组（索引应 < Primitives.Count）"
    },
    "attribute_store_file": "Assets/PCGToolkit/Editor/Core/AttributeStore.cs",
    "attribute_store_api": {
      "CreateAttribute": "创建新属性，返回PCGAttribute",
      "GetAttribute": "按名获取属性，不存在返回null",
      "GetAllAttributes": "返回所有PCGAttribute",
      "GetAttributeNames": "返回所有属性名",
      "Clone": "深拷贝整个AttributeStore"
    },
    "pcg_attribute_structure": {
      "Name": "string",
      "Type": "AttribType (Float/Int/Vector2/Vector3/Vector4/Color/String)",
      "DefaultValue": "object",
      "Values": "List<object>"
    }
  },
  "phases": [
    {
      "phase": "A",
      "title": "扩展Debug框架",
      "priority": "high",
      "tasks": [
        {
          "task_id": "A1",
          "title": "新增GeometryValidator静态工具类",
          "priority": "high",
          "file_to_create": "Assets/PCGToolkit/Editor/Core/GeometryValidator.cs",
          "namespace": "PCGToolkit.Core",
          "description": "创建一个静态工具类，对PCGGeometry进行数据一致性验证，返回警告列表。此类将被Debug框架和执行引擎共同使用。",
          "implementation": {
            "class_name": "GeometryValidator",
            "method": "public static List<string> Validate(PCGGeometry geo)",
            "checks": [
              {
                "id": "check_point_attrib_count",
                "description": "检查每个PointAttrib的Values.Count是否等于Points.Count",
                "severity": "error",
                "format": "PointAttrib '{name}': Values.Count={actual} != Points.Count={expected}"
              },
              {
                "id": "check_prim_attrib_count",
                "description": "检查每个PrimAttrib的Values.Count是否等于Primitives.Count",
                "severity": "error",
                "format": "PrimAttrib '{name}': Values.Count={actual} != Primitives.Count={expected}"
              },
              {
                "id": "check_vertex_attrib_count",
                "description": "检查每个VertexAttrib的Values.Count是否等于所有面的顶点总数（sum of prim.Length）",
                "severity": "warning",
                "format": "VertexAttrib '{name}': Values.Count={actual} != TotalVertices={expected}"
              },
              {
                "id": "check_detail_attrib_count",
                "description": "检查每个DetailAttrib的Values.Count是否等于1",
                "severity": "warning",
                "format": "DetailAttrib '{name}': Values.Count={actual} != 1"
              },
              {
                "id": "check_point_group_indices",
                "description": "检查PointGroups中的每个索引是否 < Points.Count",
                "severity": "error",
                "format": "PointGroup '{name}': index {idx} >= Points.Count={count}"
              },
              {
                "id": "check_prim_group_indices",
                "description": "检查PrimGroups中的每个索引是否 < Primitives.Count",
                "severity": "error",
                "format": "PrimGroup '{name}': index {idx} >= Primitives.Count={count}"
              },
              {
                "id": "check_prim_vertex_indices",
                "description": "检查Primitives中每个面的每个顶点索引是否 < Points.Count 且 >= 0",
                "severity": "error",
                "format": "Primitive[{primIdx}]: vertex index {vertIdx} out of range [0, {pointCount})"
              },
              {
                "id": "check_edge_indices",
                "description": "检查Edges中每条边的两个索引是否 < Points.Count 且 >= 0",
                "severity": "warning",
                "format": "Edge[{edgeIdx}]: index {vertIdx} out of range [0, {pointCount})"
              }
            ]
          }
        },
        {
          "task_id": "A2",
          "title": "扩展PCGNodeInspectorWindow的Geometry Debug面板",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",
          "description": "在UpdateExecutionInfo方法中，将当前只显示属性名和分组名的简单文本，扩展为完整的Geometry Debug面板。",
          "current_code_location": {
            "method": "UpdateExecutionInfo",
            "line_range": [96, 112],
            "current_behavior": "只显示Points/Primitives/Edges数量、属性名列表、分组名列表"
          },
          "changes": [
            {
              "description": "将_geometryStatsLabel替换为一个可折叠的Foldout结构，包含以下子面板",
              "sub_panels": [
                {
                  "name": "Topology",
                  "content": "Points.Count, Primitives.Count, Edges.Count"
                },
                {
                  "name": "Point Attributes",
                  "content": "遍历PointAttribs.GetAllAttributes()，每个属性显示：Name, Type, Values.Count, DefaultValue。用红色标记Values.Count != Points.Count的情况。可展开查看前20条Values的具体值（ToString()）"
                },
                {
                  "name": "Vertex Attributes",
                  "content": "同上，遍历VertexAttribs"
                },
                {
                  "name": "Primitive Attributes",
                  "content": "同上，遍历PrimAttribs，红色标记Values.Count != Primitives.Count"
                },
                {
                  "name": "Detail Attributes",
                  "content": "同上，遍历DetailAttribs，红色标记Values.Count != 1"
                },
                {
                  "name": "Point Groups",
                  "content": "每个组名 + 包含的索引数量，可展开查看索引列表（前50个）"
                },
                {
                  "name": "Prim Groups",
                  "content": "同上"
                },
                {
                  "name": "Validation",
                  "content": "调用GeometryValidator.Validate()，显示所有警告/错误，用红色/黄色标记"
                }
              ]
            },
            {
              "description": "需要将geometry对象缓存为成员变量_lastGeometry，以便在OnGUI中构建UI时使用"
            }
          ]
        },
        {
          "task_id": "A3",
          "title": "在执行引擎中集成自动验证",
          "priority": "medium",
          "file": "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs",
          "description": "在节点执行完成后自动调用GeometryValidator验证输出，将警告信息附加到NodeExecutionResult中。",
          "changes": [
            {
              "location": "NodeExecutionResult类（约第35-58行）",
              "change": "新增字段 public List<string> Warnings = new List<string>();"
            },
            {
              "location": "ExecuteNodeInternal方法（约第376-403行），在outputs != null分支内",
              "change": "遍历outputs中的每个PCGGeometry，调用GeometryValidator.Validate()，将结果存入result.Warnings。如果有warnings，通过Debug.LogWarning输出，格式为：[PCGValidator] Node {nodeType} ({nodeId}): {warning}"
            }
          ]
        },
        {
          "task_id": "A4",
          "title": "扩展PCGNodePreviewWindow的数据面板",
          "priority": "low",
          "file": "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",
          "description": "在3D预览窗口下方增加一个可折叠的数据面板tab，显示与A2相同的属性/分组详情。",
          "current_code_location": {
            "line_range": [143, 151],
            "current_behavior": "只显示Points和Prims数量的miniLabel"
          },
          "changes": [
            {
              "description": "在现有的Points/Prims标签下方增加一个Foldout，展示完整的属性和分组信息，复用A2中的UI构建逻辑（可提取为共享的静态方法）"
            }
          ]
        }
      ]
    },
    {
      "phase": "B",
      "title": "修复节点属性/分组传递Bug",
      "priority": "high",
      "general_principle": "任何修改拓扑（增删改点或面）的节点，都必须同步更新所有相关的AttributeStore（PointAttribs/VertexAttribs/PrimAttribs/DetailAttribs）和Groups（PointGroups/PrimGroups）。创建新PCGGeometry时，如果是从源geometry派生的，必须传递所有属性和分组。",
      "tasks": [
        {
          "task_id": "B1",
          "title": "修复MergeNode — 属性合并不完整",
          "priority": "critical",
          "file": "Assets/PCGToolkit/Editor/Nodes/Create/MergeNode.cs",
          "bugs": [
            {
              "id": "B1-1",
              "severity": "critical",
              "description": "VertexAttribs完全未合并。如果上游节点将UV写入VertexAttribs，merge后UV丢失。",
              "location": "Execute方法第64-66行，只合并了PointAttribs和PrimAttribs",
              "fix": "在第66行后增加VertexAttribs的合并。VertexAttribs的elementCount应为该geo所有面的顶点总数（sum of prim.Length），existingCount应为result中已有的所有面的顶点总数。"
            },
            {
              "id": "B1-2",
              "severity": "critical",
              "description": "DetailAttribs完全未合并。",
              "location": "Execute方法第64-66行",
              "fix": "合并DetailAttribs：取第一个非空输入的DetailAttribs值。如果多个输入都有同名Detail属性，保留第一个的值。"
            },
            {
              "id": "B1-3",
              "severity": "high",
              "description": "MergeAttributes方法中，dest中已有但src中没有的属性不会被补齐DefaultValue。例如：geo A有属性Cd（3个值），geo B没有Cd。合并后Cd只有3个值，但Points有6个。",
              "location": "MergeAttributes方法第92-107行",
              "fix": "在MergeAttributes方法末尾（第106行后），遍历dest中所有属性，对于src中不存在的属性，补齐elementCount个DefaultValue。具体做法：在foreach循环结束后，再遍历dest.GetAllAttributes()，检查哪些属性不在src中（可用HashSet记录src中处理过的属性名），对这些属性补齐。"
            }
          ]
        },
        {
          "task_id": "B2",
          "title": "修复ExtrudeNode — 属性全部丢失",
          "priority": "critical",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/ExtrudeNode.cs",
          "bugs": [
            {
              "id": "B2-1",
              "severity": "critical",
              "description": "ExecuteNormal和ExecuteIndividual都创建了新的PCGGeometry result，只复制了Points和Primitives，完全没有处理PointAttribs、PrimAttribs、VertexAttribs、DetailAttribs、PointGroups、PrimGroups。挤出后所有属性和分组全部丢失。",
              "location": "ExecuteNormal第93行创建new PCGGeometry()，ExecuteIndividual第182行同样",
              "fix_steps": [
                "1. 在复制原始顶点后（第96-99行），同步复制原始点的PointAttribs：遍历geo.PointAttribs.GetAllAttributes()，为每个属性创建对应的result属性，复制前geo.Points.Count个值",
                "2. 为新生成的挤出点（每层新顶点），从对应的原始点复制属性值（因为挤出点是从原始点派生的，属性值应与原始点相同）",
                "3. 复制DetailAttribs：result.DetailAttribs = geo.DetailAttribs.Clone()",
                "4. 为未挤出的面传递PrimAttribs和PrimGroups",
                "5. 为挤出生成的侧面和顶面，PrimAttribs补DefaultValue",
                "6. ExecuteIndividual中同理处理"
              ]
            }
          ]
        },
        {
          "task_id": "B3",
          "title": "修复SubdivideNode — 属性全部丢失 + 未Clone输入",
          "priority": "critical",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/SubdivideNode.cs",
          "bugs": [
            {
              "id": "B3-1",
              "severity": "high",
              "description": "Execute方法第38行 var geo = GetInputGeometry(inputGeometries, 'input') 没有调用.Clone()，可能修改上游数据（虽然SubdivideLinear返回新对象，但如果iterations==1且有其他节点引用同一上游输出，存在共享引用风险）。",
              "location": "Execute方法第38行",
              "fix": "改为 var geo = GetInputGeometry(inputGeometries, 'input').Clone();"
            },
            {
              "id": "B3-2",
              "severity": "critical",
              "description": "SubdivideLinear和SubdivideCatmullClark都创建新的PCGGeometry，只处理了拓扑（Points和Primitives），完全没有传递或插值任何属性和分组。细分后法线、UV、颜色等全部丢失。",
              "location": "SubdivideLinear第52-117行，SubdivideCatmullClark第119-298行",
              "fix_steps": [
                "1. SubdivideLinear中：复制原始点的PointAttribs值（前geo.Points.Count个点保持原值）",
                "2. 为边中点插值属性：取两端点属性值的平均（对于float/int/Vector类型做线性插值，对于string类型取第一个端点的值）",
                "3. 为面中心点插值属性：取面所有点属性值的平均",
                "4. PrimAttribs：原面的子面继承原面的属性值",
                "5. PrimGroups：原面的子面继承原面的分组",
                "6. DetailAttribs：直接Clone",
                "7. SubdivideCatmullClark中同理，但顶点位置已经被Catmull-Clark规则修改，属性插值也应遵循相同的权重规则",
                "8. 建议创建一个辅助方法 InterpolateAttribute(PCGAttribute attr, int idxA, int idxB, float t) 用于属性插值"
              ]
            }
          ]
        },
        {
          "task_id": "B4",
          "title": "修复FuseNode — 属性和分组未同步",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/FuseNode.cs",
          "bugs": [
            {
              "id": "B4-1",
              "severity": "critical",
              "description": "合并重叠顶点后，PointAttribs的Values数量仍然是原始点数，但Points已经被缩减为newPoints。属性值与点不对应。",
              "location": "Execute方法第75-94行，更新了面索引和边索引，但没有同步PointAttribs",
              "fix_steps": [
                "1. 在geo.Points = newPoints之前，重建PointAttribs：遍历所有属性，创建新的Values列表，只保留被保留的点（即oldToNew中作为主点的那些点）的属性值",
                "2. 具体做法：对于每个属性attr，创建新的Values列表newValues。遍历0到geo.Points.Count-1，如果remap[i]==i（即该点是主点），则newValues.Add(attr.Values[i])",
                "3. 同步PointGroups：遍历每个PointGroup，使用oldToNew映射更新索引",
                "4. PrimAttribs和PrimGroups不需要改变（面没有被删除，只是索引被更新了）"
              ]
            }
          ]
        },
        {
          "task_id": "B5",
          "title": "修复ClipNode — 属性和分组未同步 + HashSet遍历顺序不确定",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/ClipNode.cs",
          "bugs": [
            {
              "id": "B5-1",
              "severity": "high",
              "description": "usedPoints是HashSet<int>，遍历顺序不确定，导致indexMap的映射顺序不稳定。不同运行可能产生不同的点顺序。",
              "location": "Execute方法第86-92行",
              "fix": "将 foreach (int idx in usedPoints) 改为 foreach (int idx in usedPoints.OrderBy(x => x))，或改用SortedSet<int>。确保点的顺序是确定性的。"
            },
            {
              "id": "B5-2",
              "severity": "critical",
              "description": "重建Points和Primitives后，完全没有同步PointAttribs、PrimAttribs、VertexAttribs、DetailAttribs、PointGroups、PrimGroups。裁切后所有属性和分组丢失。",
              "location": "Execute方法第103-104行之后",
              "fix_steps": [
                "1. 同步PointAttribs：遍历geo.PointAttribs.GetAllAttributes()，按indexMap重建Values（只保留被保留点的属性值）",
                "2. 同步PrimAttribs：需要记录哪些面被保留了（建立旧面索引到新面索引的映射），只保留被保留面的属性值",
                "3. 同步PointGroups：使用indexMap更新索引，移除不在indexMap中的索引",
                "4. 同步PrimGroups：使用面索引映射更新",
                "5. DetailAttribs保持不变",
                "6. 清空Edges（拓扑已变化，旧边无效）"
              ]
            }
          ]
        },
        {
          "task_id": "B6",
          "title": "修复BlastNode — 属性未同步",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/BlastNode.cs",
          "bugs": [
            {
              "id": "B6-1",
              "severity": "critical",
              "description": "primitive模式：删除面后，PrimAttribs未同步。被删除面的属性值仍然留在PrimAttribs中，导致Values.Count与Primitives.Count不匹配。",
              "location": "Execute方法第77-106行，只更新了PrimGroups，没有更新PrimAttribs",
              "fix": "在第86行（geo.Primitives = newPrims）之后，重建PrimAttribs：遍历所有PrimAttrib，创建新的Values列表，只保留未被删除面的属性值。"
            },
            {
              "id": "B6-2",
              "severity": "critical",
              "description": "point模式：删除点后，PointAttribs和PointGroups未同步。",
              "location": "Execute方法第108-146行",
              "fix_steps": [
                "1. 在geo.Points = newPoints之后，重建PointAttribs：遍历所有PointAttrib，按indexMap重建Values",
                "2. 重建PointGroups：使用indexMap更新索引，移除被删除点的索引",
                "3. 重建PrimGroups：面被过滤后索引变化了，需要建立旧面索引到新面索引的映射并更新PrimGroups",
                "4. 重建PrimAttribs：同理只保留被保留面的属性值"
              ]
            }
          ]
        },
        {
          "task_id": "B7",
          "title": "修复InsetNode — 属性全部丢失",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/InsetNode.cs",
          "bugs": [
            {
              "id": "B7-1",
              "severity": "critical",
              "description": "创建新的PCGGeometry result，只复制了Points，没有处理任何属性和分组。",
              "location": "Execute方法第52-54行",
              "fix_steps": [
                "1. 复制原始点的PointAttribs：result.PointAttribs = geo.PointAttribs.Clone()",
                "2. 为内缩新点（innerVerts），从对应原始点复制属性值：遍历所有PointAttrib，为每个新点添加对应原始点prim[i]的属性值",
                "3. 复制DetailAttribs：result.DetailAttribs = geo.DetailAttribs.Clone()",
                "4. 为未内缩的面传递PrimAttribs",
                "5. 为内缩生成的侧面和内面，PrimAttribs补DefaultValue或从原面继承",
                "6. 传递PrimGroups：未内缩的面保持原分组"
              ]
            }
          ]
        },
        {
          "task_id": "B8",
          "title": "修复FacetNode — 属性全部丢失",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/FacetNode.cs",
          "bugs": [
            {
              "id": "B8-1",
              "severity": "critical",
              "description": "MakeUnique创建新PCGGeometry，为每个面的每个顶点创建独立副本，但完全没有传递属性和分组。",
              "location": "MakeUnique方法第70-86行",
              "fix_steps": [
                "1. 为每个新点复制对应原始点prim[i]的PointAttribs值",
                "2. PrimAttribs直接Clone（面的数量和顺序不变）",
                "3. PrimGroups直接复制（面索引不变）",
                "4. DetailAttribs直接Clone"
              ]
            },
            {
              "id": "B8-2",
              "severity": "critical",
              "description": "Consolidate创建新PCGGeometry，合并重叠顶点，但完全没有传递属性和分组。",
              "location": "Consolidate方法第88-131行",
              "fix_steps": [
                "1. 为合并后的点保留主点的PointAttribs值（使用remap数组确定主点）",
                "2. PrimAttribs：跳过退化面后需要重建",
                "3. PrimGroups：跳过退化面后需要重建索引映射",
                "4. DetailAttribs直接Clone"
              ]
            }
          ]
        },
        {
          "task_id": "B9",
          "title": "修复SortNode — PointAttribs/PrimAttribs未按新顺序重排",
          "priority": "high",
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/SortNode.cs",
          "bugs": [
            {
              "id": "B9-1",
              "severity": "critical",
              "description": "RemapPoints中 result.PointAttribs = geo.PointAttribs.Clone() 直接复制了属性，但属性Values的顺序仍然是旧的，没有按newToOld重排。排序后属性值与点不对应。",
              "location": "RemapPoints方法第152行",
              "fix": "不要直接Clone。遍历geo.PointAttribs.GetAllAttributes()，为每个属性创建新属性，按newToOld顺序重建Values：foreach (int oldIdx in newToOld) { newAttr.Values.Add(attr.Values[oldIdx]); }"
            },
            {
              "id": "B9-2",
              "severity": "critical",
              "description": "RemapPrimitives中 result.PrimAttribs = geo.PrimAttribs.Clone() 同样没有按新顺序重排。",
              "location": "RemapPrimitives方法第188行",
              "fix": "同B9-1，按newToOld顺序重建PrimAttribs的Values。"
            }
          ]
        },
        {
          "task_id": "B10",
          "title": "修复CopyToPointsNode — 源几何体属性未传递",
          "priority": "medium",
          "file": "Assets/PCGToolkit/Editor/Nodes/Distribute/CopyToPointsNode.cs",
          "bugs": [
            {
              "id": "B10-1",
              "severity": "high",
              "description": "只传递了@copynum和transferAttributes指定的目标点属性，但源几何体自身的PointAttribs（如法线N、UV uv等）完全没有复制到结果中。",
              "location": "Execute方法第128-175行",
              "fix_steps": [
                "1. 在复制变换后的顶点循环中（第129-133行），同时复制源几何体的PointAttribs",
                "2. 对于法线N属性，需要经过rotation变换：rotatedN = rotation * originalN",
                "3. 对于其他属性（如uv、Cd等），直接复制值",
                "4. 同时复制源几何体的PrimAttribs（每个副本的面属性与源相同）",  
                "5. 复制源几何体的PrimGroups（每个副本的面分组需要加上primOffset）",  
                "6. 复制源几何体的PointGroups（每个副本的点分组需要加上vertexOffset）",  
                "7. DetailAttribs取源几何体的值"  
              ]  
            }  
          ]  
        },  
        {  
          "task_id": "B11",  
          "title": "修复MirrorNode — PrimAttribs和PrimGroups未合并",  
          "priority": "high",  
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/MirrorNode.cs",  
          "bugs": [  
            {  
              "id": "B11-1",  
              "severity": "high",  
              "description": "在keepOriginal=true合并时，只合并了PointAttribs，完全没有处理PrimAttribs和PrimGroups。镜像侧的面属性和面分组全部丢失。PointGroups也未合并。",  
              "location": "Execute方法第97-123行",  
              "fix_steps": [  
                "1. 在PointAttribs合并逻辑（第109-121行）之后，增加PrimAttribs合并：遍历mirrored.PrimAttribs.GetAllAttributes()，对于每个属性，在result中找到或创建同名属性，先补齐原始面数量的DefaultValue（如果result中没有该属性），再AddRange镜像侧的值",  
                "2. 合并PointGroups：遍历mirrored.PointGroups，对于每个组，在result中找到或创建同名组，将镜像侧的索引加上offset后添加",  
                "3. 合并PrimGroups：遍历mirrored.PrimGroups，对于每个组，在result中找到或创建同名组，将镜像侧的索引加上primOffset（primOffset = result.Primitives.Count - mirrored.Primitives.Count，在添加镜像面之后计算）",  
                "4. 在keepOriginal=false时，也需要传递镜像几何体的属性和分组"  
              ]  
            }  
          ]  
        },  
        {  
          "task_id": "B12",  
          "title": "修复NormalNode — cusp angle逻辑无效",  
          "priority": "medium",  
          "file": "Assets/PCGToolkit/Editor/Nodes/Geometry/NormalNode.cs",  
          "bugs": [  
            {  
              "id": "B12-1",  
              "severity": "medium",  
              "description": "在vertex模式中，withinCusp变量被计算但从未被使用来影响法线累加。无论cusp angle如何设置，所有相邻面的法线都会被累加，cusp angle参数实际上无效。",  
              "location": "ComputeVertexNormals方法第98-115行",  
              "fix": "将第114行的法线累加 avgNormal += faceNormals[faceIdx] * area; 包裹在 if (withinCusp) 条件中。即：只有当该面与所有其他相邻面的夹角都在cusp angle范围内时，才将其法线累加到顶点法线中。修改后的逻辑：if (withinCusp) { float area = weightByArea ? CalculateFaceArea(geo, faceIdx) : 1f; avgNormal += faceNormals[faceIdx] * area; }"  
            }  
          ]  
        },  
        {  
          "task_id": "B13",  
          "title": "修复ForEachNode.MergeResults — PrimAttribs未合并",  
          "priority": "high",  
          "file": "Assets/PCGToolkit/Editor/Nodes/Utility/ForEachNode.cs",  
          "bugs": [  
            {  
              "id": "B13-1",  
              "severity": "critical",  
              "description": "MergeResults方法合并了PointAttribs、PointGroups、PrimGroups，但完全没有合并PrimAttribs。ForEach循环后所有面属性丢失。",  
              "location": "MergeResults方法第226-277行",  
              "fix": "在PointAttribs合并逻辑（第247-255行）之后，增加PrimAttribs合并逻辑。代码结构与PointAttribs合并完全对称：遍历piece.PrimAttribs.GetAllAttributes()，对于每个属性，在result中找到或创建同名属性。如果是新创建的属性，先补齐primOffset个DefaultValue（primOffset = result.Primitives.Count - piece.Primitives.Count，即添加当前piece的面之后、减去当前piece面数）。然后AddRange当前piece的属性值。",  
              "fix_code_template": "// 合并面属性（在PointAttribs合并之后、PointGroups合并之前插入）\nint primOffsetForAttribs = result.Primitives.Count - piece.Primitives.Count;\nforeach (var attr in piece.PrimAttribs.GetAllAttributes())\n{\n    var destAttr = result.PrimAttribs.GetAttribute(attr.Name);\n    if (destAttr == null)\n    {\n        destAttr = result.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);\n        for (int j = 0; j < primOffsetForAttribs; j++)\n            destAttr.Values.Add(destAttr.DefaultValue);\n    }\n    destAttr.Values.AddRange(attr.Values);\n}"  
            },  
            {  
              "id": "B13-2",  
              "severity": "medium",  
              "description": "MergeResults方法中，PointAttribs合并时，如果前面的piece有属性A但后面的piece没有，属性A不会为后面piece的点补齐DefaultValue。这与MergeNode的B1-3 bug相同。",  
              "location": "MergeResults方法第247-255行",  
              "fix": "在每个piece处理完毕后（pointOffset += piece.Points.Count之前），遍历result.PointAttribs中所有属性，对于当前piece中不存在的属性，补齐piece.Points.Count个DefaultValue。PrimAttribs同理。"  
            }  
          ]  
        },  
        {  
          "task_id": "B14",  
          "title": "修复SplitNode.ExtractPrims — PrimAttribs和PrimGroups未传递",  
          "priority": "high",  
          "file": "Assets/PCGToolkit/Editor/Nodes/Utility/SplitNode.cs",  
          "bugs": [  
            {  
              "id": "B14-1",  
              "severity": "critical",  
              "description": "ExtractPrims方法只传递了PointAttribs，完全没有传递PrimAttribs、PrimGroups、PointGroups、DetailAttribs。Split后面属性和所有分组丢失。",  
              "location": "ExtractPrims方法第69-116行",  
              "fix_steps": [  
                "1. 在PointAttribs复制逻辑（第102-111行）之后，增加PrimAttribs复制：建立旧面索引到新面索引的映射（在复制面的循环中记录），遍历source.PrimAttribs.GetAllAttributes()，为每个属性创建新属性，按旧面索引顺序复制对应的值",  
                "2. 增加PointGroups复制：遍历source.PointGroups，对于每个组，使用indexMap将旧点索引映射为新点索引，只保留在indexMap中存在的索引",  
                "3. 增加PrimGroups复制：遍历source.PrimGroups，对于每个组，使用面索引映射将旧面索引映射为新面索引，只保留在primIndices中存在的索引",  
                "4. 复制DetailAttribs：result.DetailAttribs = source.DetailAttribs.Clone()"  
              ]  
            }  
          ]  
        },  
        {  
          "task_id": "B15",  
          "title": "修复ScatterNode — 三角形选择不按面积加权",  
          "priority": "low",  
          "file": "Assets/PCGToolkit/Editor/Nodes/Distribute/ScatterNode.cs",  
          "bugs": [  
            {  
              "id": "B15-1",  
              "severity": "low",  
              "description": "SamplePointOnPrim中 int selectedTri = rng.Next(triCount) 是均匀随机选择子三角形，但多边形扇形三角化后各子三角形面积可能不同。大三角形和小三角形被等概率选中，导致散布点分布不均匀。",  
              "location": "SamplePointOnPrim方法第165-170行",  
              "fix_steps": [  
                "1. 计算每个子三角形的面积：area[i] = 0.5f * Vector3.Cross(v1-v0, v2-v0).magnitude",  
                "2. 构建累积分布函数（CDF）：cdf[i] = sum(area[0..i]) / totalArea",  
                "3. 生成随机数r in [0,1)，用二分查找在CDF中找到对应的子三角形索引",  
                "4. 使用该子三角形进行采样"  
              ]  
            }  
          ]  
        },  
        {  
          "task_id": "B16",  
          "title": "创建属性同步辅助工具类",  
          "priority": "high",  
          "file_to_create": "Assets/PCGToolkit/Editor/Core/AttributeSyncHelper.cs",  
          "namespace": "PCGToolkit.Core",  
          "description": "创建一个静态辅助类，封装常用的属性同步操作，供所有节点复用，避免每个节点重复实现相同的逻辑。",  
          "methods": [  
            {  
              "signature": "public static void RemapPointAttribs(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewIndexMap)",  
              "description": "按索引映射重建PointAttribs。遍历source.PointAttribs的所有属性，在dest中创建对应属性，按oldToNewIndexMap的值顺序（排序后）复制对应的源属性值。"  
            },  
            {  
              "signature": "public static void RemapPrimAttribs(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewPrimMap)",  
              "description": "按索引映射重建PrimAttribs。同上。"  
            },  
            {  
              "signature": "public static void RemapPointGroups(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewIndexMap)",  
              "description": "按索引映射重建PointGroups。遍历source.PointGroups，对于每个组，将旧索引通过映射转换为新索引。"  
            },  
            {  
              "signature": "public static void RemapPrimGroups(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewPrimMap)",  
              "description": "按索引映射重建PrimGroups。同上。"  
            },  
            {  
              "signature": "public static void CopyPointAttribsForNewPoints(PCGGeometry dest, PCGGeometry source, List<int> sourcePointIndices)",  
              "description": "为dest中新增的点从source中复制属性值。sourcePointIndices[i]表示dest中第(dest原有点数+i)个新点对应source中的哪个点。"  
            },  
            {  
              "signature": "public static void MergeAttribStore(AttributeStore dest, AttributeStore src, int existingElementCount, int newElementCount)",  
              "description": "将src中的属性合并到dest中。对于dest中已有但src中没有的属性，补齐newElementCount个DefaultValue。对于src中有但dest中没有的属性，先补齐existingElementCount个DefaultValue再AddRange。对于两者都有的属性，先补齐到existingElementCount再AddRange。"  
            },  
            {  
              "signature": "public static object InterpolateAttributeValue(object valA, object valB, float t, AttribType type)",  
              "description": "对两个属性值进行线性插值。Float: Lerp, Int: Round(Lerp), Vector2/3/4: Vector.Lerp, Color: Color.Lerp, String: 取valA。"  
            },  
            {  
              "signature": "public static object AverageAttributeValues(List<object> values, AttribType type)",  
              "description": "对多个属性值取平均。用于细分等操作中面中心点的属性计算。"  
            }  
          ]  
        }  
      ]  
    }  
  ],  
  "execution_order": [  
    {  
      "step": 1,  
      "tasks": ["B16"],  
      "reason": "先创建辅助工具类，后续所有节点修复都会用到"  
    },  
    {  
      "step": 2,  
      "tasks": ["A1"],  
      "reason": "创建GeometryValidator，后续Debug框架和执行引擎都会用到"  
    },  
    {  
      "step": 3,  
      "tasks": ["B1", "B13"],  
      "reason": "修复MergeNode和ForEachNode.MergeResults，这是用户当前最急需的"  
    },  
    {  
      "step": 4,  
      "tasks": ["B4", "B5", "B6"],  
      "reason": "修复FuseNode、ClipNode、BlastNode — 这些节点直接修改拓扑，属性丢失影响最大"  
    },  
    {  
      "step": 5,  
      "tasks": ["B2", "B3", "B7", "B8"],  
      "reason": "修复ExtrudeNode、SubdivideNode、InsetNode、FacetNode — 创建新几何体的节点"  
    },  
    {  
      "step": 6,  
      "tasks": ["B9", "B10", "B11", "B14"],  
      "reason": "修复SortNode、CopyToPointsNode、MirrorNode、SplitNode"  
    },  
    {  
      "step": 7,  
      "tasks": ["B12", "B15"],  
      "reason": "修复NormalNode cusp angle逻辑和ScatterNode面积加权 — 功能性bug但不影响数据完整性"  
    },  
    {  
      "step": 8,  
      "tasks": ["A2", "A3", "A4"],  
      "reason": "完成Debug框架扩展 — 此时所有节点已修复，可以用Debug框架验证"  
    }  
  ],  
  "testing_strategy": {  
    "description": "每个节点修复后，使用GeometryValidator验证输出的数据一致性",  
    "test_cases": [  
      {  
        "name": "MergeNode属性完整性",  
        "setup": "创建两个Box，分别设置不同的PointAttribs（如Cd颜色），然后Merge",  
        "expected": "Merge后Points.Count == 两个Box点数之和，每个PointAttrib的Values.Count == Points.Count，属性值正确对应"  
      },  
      {  
        "name": "MergeNode不对称属性",  
        "setup": "Box A有属性Cd，Box B没有。Merge后检查",  
        "expected": "Cd属性的Values.Count == 总点数，Box B的点使用DefaultValue填充"  
      },  
      {  
        "name": "ExtrudeNode属性传递",  
        "setup": "创建Box，设置PointAttribs Cd，然后Extrude",  
        "expected": "Extrude后所有PointAttrib的Values.Count == Points.Count"  
      },  
      {  
        "name": "SubdivideNode属性插值",  
        "setup": "创建Grid，设置PointAttribs Cd（四角不同颜色），Subdivide 1次",  
        "expected": "细分后新点的Cd值为相邻点的插值，所有PointAttrib的Values.Count == Points.Count"  
      },  
      {  
        "name": "FuseNode属性同步",  
        "setup": "创建两个共享边的面，Fuse",  
        "expected": "Fuse后Points.Count减少，所有PointAttrib的Values.Count == Points.Count"  
      },  
      {  
        "name": "SortNode属性重排",  
        "setup": "创建Grid，设置PointAttribs Cd，按X轴排序",  
        "expected": "排序后Cd值与对应点的位置一致"  
      },  
      {  
        "name": "ForEachNode PrimAttribs",  
        "setup": "创建多面体，设置PrimAttribs material，ForEach处理",  
        "expected": "ForEach后PrimAttribs的Values.Count == Primitives.Count"  
      },  
      {  
        "name": "全链路测试",  
        "setup": "Box → Subdivide → Extrude → Merge(with another Box) → Fuse → Sort",  
        "expected": "每个节点输出都通过GeometryValidator验证，无任何警告"  
      }  
    ]  
  },  
  "files_to_create": [  
    "Assets/PCGToolkit/Editor/Core/GeometryValidator.cs",  
    "Assets/PCGToolkit/Editor/Core/AttributeSyncHelper.cs"  
  ],  
  "files_to_modify": [  
    "Assets/PCGToolkit/Editor/Nodes/Create/MergeNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/ExtrudeNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/SubdivideNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/FuseNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/ClipNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/BlastNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/InsetNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/FacetNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/SortNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/MirrorNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Geometry/NormalNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Distribute/CopyToPointsNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Distribute/ScatterNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Utility/ForEachNode.cs",  
    "Assets/PCGToolkit/Editor/Nodes/Utility/SplitNode.cs",  
    "Assets/PCGToolkit/Editor/Graph/PCGNodeInspectorWindow.cs",  
    "Assets/PCGToolkit/Editor/Graph/PCGNodePreviewWindow.cs",  
    "Assets/PCGToolkit/Editor/Graph/PCGAsyncGraphExecutor.cs"  
  ],  
  "summary_stats": {  
    "total_tasks": 20,  
    "critical_bugs": 12,  
    "high_bugs": 7,  
    "medium_bugs": 3,  
    "low_bugs": 1,  
    "files_to_create": 2,  
    "files_to_modify": 18  
  }  
}
```