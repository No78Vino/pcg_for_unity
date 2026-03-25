using System;
using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// 属性同步辅助工具类，封装常用的属性/分组同步操作，供所有节点复用。
    /// </summary>
    public static class AttributeSyncHelper
    {
        /// <summary>
        /// 按索引映射重建PointAttribs。遍历source.PointAttribs的所有属性，
        /// 在dest中创建对应属性，按oldToNewIndexMap的值顺序（排序后）复制对应的源属性值。
        /// </summary>
        public static void RemapPointAttribs(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewIndexMap)
        {
            if (oldToNewIndexMap == null || oldToNewIndexMap.Count == 0)
                return;

            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var newAttr = dest.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                var sortedValues = new List<int>(oldToNewIndexMap.Keys);
                sortedValues.Sort();
                foreach (var oldIdx in sortedValues)
                {
                    if (oldToNewIndexMap.TryGetValue(oldIdx, out int newIdx))
                    {
                        if (oldIdx >= 0 && oldIdx < attr.Values.Count)
                            newAttr.Values.Add(attr.Values[oldIdx]);
                        else
                            newAttr.Values.Add(attr.DefaultValue);
                    }
                }
            }
        }

        /// <summary>
        /// 按索引映射重建PrimAttribs。同RemapPointAttribs逻辑。
        /// </summary>
        public static void RemapPrimAttribs(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewPrimMap)
        {
            if (oldToNewPrimMap == null || oldToNewPrimMap.Count == 0)
                return;

            foreach (var attr in source.PrimAttribs.GetAllAttributes())
            {
                var newAttr = dest.PrimAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                var sortedValues = new List<int>(oldToNewPrimMap.Keys);
                sortedValues.Sort();
                foreach (var oldIdx in sortedValues)
                {
                    if (oldToNewPrimMap.TryGetValue(oldIdx, out int newIdx))
                    {
                        if (oldIdx >= 0 && oldIdx < attr.Values.Count)
                            newAttr.Values.Add(attr.Values[oldIdx]);
                        else
                            newAttr.Values.Add(attr.DefaultValue);
                    }
                }
            }
        }

        /// <summary>
        /// 按索引映射重建PointGroups。遍历source.PointGroups，
        /// 将旧索引通过映射转换为新索引。
        /// </summary>
        public static void RemapPointGroups(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewIndexMap)
        {
            if (oldToNewIndexMap == null || oldToNewIndexMap.Count == 0)
                return;

            foreach (var kvp in source.PointGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (var oldIdx in kvp.Value)
                {
                    if (oldToNewIndexMap.TryGetValue(oldIdx, out int newIdx))
                    {
                        newGroup.Add(newIdx);
                    }
                }
                if (newGroup.Count > 0)
                    dest.PointGroups[kvp.Key] = newGroup;
            }
        }

        /// <summary>
        /// 按索引映射重建PrimGroups。同RemapPointGroups逻辑。
        /// </summary>
        public static void RemapPrimGroups(PCGGeometry source, PCGGeometry dest, Dictionary<int, int> oldToNewPrimMap)
        {
            if (oldToNewPrimMap == null || oldToNewPrimMap.Count == 0)
                return;

            foreach (var kvp in source.PrimGroups)
            {
                var newGroup = new HashSet<int>();
                foreach (var oldIdx in kvp.Value)
                {
                    if (oldToNewPrimMap.TryGetValue(oldIdx, out int newIdx))
                    {
                        newGroup.Add(newIdx);
                    }
                }
                if (newGroup.Count > 0)
                    dest.PrimGroups[kvp.Key] = newGroup;
            }
        }

        /// <summary>
        /// 为dest中新增的点从source中复制属性值。
        /// sourcePointIndices[i]表示dest中第(dest原有点数+i)个新点对应source中的哪个点。
        /// </summary>
        public static void CopyPointAttribsForNewPoints(PCGGeometry dest, PCGGeometry source, List<int> sourcePointIndices)
        {
            int originalPointCount = dest.Points.Count - sourcePointIndices.Count;
            if (sourcePointIndices == null || sourcePointIndices.Count == 0)
                return;

            foreach (var attr in source.PointAttribs.GetAllAttributes())
            {
                var destAttr = dest.PointAttribs.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = dest.PointAttribs.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    for (int i = 0; i < originalPointCount; i++)
                        destAttr.Values.Add(attr.DefaultValue);
                }

                foreach (var srcIdx in sourcePointIndices)
                {
                    if (srcIdx >= 0 && srcIdx < attr.Values.Count)
                        destAttr.Values.Add(attr.Values[srcIdx]);
                    else
                        destAttr.Values.Add(attr.DefaultValue);
                }
            }
        }

        /// <summary>
        /// 合并两个AttributeStore。对于dest中已有但src中没有的属性，补齐newElementCount个DefaultValue。
        /// 对于src中有但dest中没有的属性，先补齐existingElementCount个DefaultValue再AddRange。
        /// 对于两者都有的属性，先补齐到existingElementCount再AddRange。
        /// </summary>
        public static void MergeAttribStore(AttributeStore dest, AttributeStore src, int existingElementCount, int newElementCount)
        {
            if (src == null)
                return;

            var destNames = new HashSet<string>();
            foreach (var name in dest.GetAttributeNames())
                destNames.Add(name);

            foreach (var attr in src.GetAllAttributes())
            {
                var destAttr = dest.GetAttribute(attr.Name);
                if (destAttr == null)
                {
                    destAttr = dest.CreateAttribute(attr.Name, attr.Type, attr.DefaultValue);
                    for (int i = 0; i < existingElementCount; i++)
                        destAttr.Values.Add(attr.DefaultValue);
                }

                destAttr.Values.AddRange(attr.Values);
                destNames.Remove(attr.Name);
            }

            foreach (var name in destNames)
            {
                var destAttr = dest.GetAttribute(name);
                for (int i = 0; i < newElementCount; i++)
                    destAttr.Values.Add(destAttr.DefaultValue);
            }
        }

        /// <summary>
        /// 对两个属性值进行线性插值。
        /// Float: Lerp, Int: Round(Lerp), Vector2/3/4: Vector.Lerp, Color: Color.Lerp, String: 取valA。
        /// </summary>
        public static object InterpolateAttributeValue(object valA, object valB, float t, AttribType type)
        {
            switch (type)
            {
                case AttribType.Float:
                    float a = valA is double ? Convert.ToSingle(valA) : (float)valA;
                    float b = valB is double ? Convert.ToSingle(valB) : (float)valB;
                    return Mathf.Lerp(a, b, t);

                case AttribType.Int:
                    float aInt = valA is double ? Convert.ToSingle(valA) : (float)(int)valA;
                    float bInt = valB is double ? Convert.ToSingle(valB) : (float)(int)valB;
                    return Mathf.RoundToInt(Mathf.Lerp(aInt, bInt, t));

                case AttribType.Vector2:
                    return Vector2.Lerp((Vector2)valA, (Vector2)valB, t);

                case AttribType.Vector3:
                    return Vector3.Lerp((Vector3)valA, (Vector3)valB, t);

                case AttribType.Vector4:
                    return Vector4.Lerp((Vector4)valA, (Vector4)valB, t);

                case AttribType.Color:
                    return Color.Lerp((Color)valA, (Color)valB, t);

                case AttribType.String:
                default:
                    return valA;
            }
        }

        /// <summary>
        /// 对多个属性值取平均。用于细分等操作中面中心点的属性计算。
        /// </summary>
        public static object AverageAttributeValues(List<object> values, AttribType type)
        {
            if (values == null || values.Count == 0)
                return null;

            switch (type)
            {
                case AttribType.Float:
                    double sumF = 0;
                    foreach (var v in values)
                        sumF += v is double ? (double)v : (double)(float)v;
                    return (float)(sumF / values.Count);

                case AttribType.Int:
                    long sumI = 0;
                    foreach (var v in values)
                        sumI += v is long ? (long)v : (int)v;
                    return (int)(sumI / values.Count);

                case AttribType.Vector2:
                    Vector2 sumV2 = Vector2.zero;
                    foreach (var v in values)
                        sumV2 += (Vector2)v;
                    return sumV2 / values.Count;

                case AttribType.Vector3:
                    Vector3 sumV3 = Vector3.zero;
                    foreach (var v in values)
                        sumV3 += (Vector3)v;
                    return sumV3 / values.Count;

                case AttribType.Vector4:
                    Vector4 sumV4 = Vector4.zero;
                    foreach (var v in values)
                        sumV4 += (Vector4)v;
                    return sumV4 / values.Count;

                case AttribType.Color:
                    Color sumC = Color.black;
                    foreach (var v in values)
                        sumC += (Color)v;
                    return sumC / values.Count;

                case AttribType.String:
                default:
                    return values[0];
            }
        }

        /// <summary>
        /// 从源几何体复制PointAttribs到目标几何体（完全替换，不做映射）。
        /// 用于从源几何体继承属性的场景。
        /// </summary>
        public static void CopyPointAttribs(PCGGeometry dest, PCGGeometry source)
        {
            dest.PointAttribs = source.PointAttribs.Clone();
        }

        /// <summary>
        /// 从源几何体复制DetailAttribs到目标几何体。
        /// </summary>
        public static void CopyDetailAttribs(PCGGeometry dest, PCGGeometry source)
        {
            dest.DetailAttribs = source.DetailAttribs.Clone();
        }

        /// <summary>
        /// 补齐目标AttributeStore中不存在的属性到指定元素数量。
        /// 用于合并时补齐缺失属性的默认值。
        /// </summary>
        public static void PadMissingAttribs(AttributeStore dest, AttributeStore src, int elementCount)
        {
            var srcNames = new HashSet<string>();
            foreach (var name in src.GetAttributeNames())
                srcNames.Add(name);

            foreach (var destAttr in dest.GetAllAttributes())
            {
                if (!srcNames.Contains(destAttr.Name))
                {
                    for (int i = 0; i < elementCount; i++)
                        destAttr.Values.Add(destAttr.DefaultValue);
                }
            }
        }
    }
}
