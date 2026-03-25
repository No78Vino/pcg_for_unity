using System.Collections.Generic;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// PCGGeometry数据一致性验证工具类。
    /// 对PCGGeometry进行数据一致性验证，返回警告/错误列表。
    /// </summary>
    public static class GeometryValidator
    {
        /// <summary>
        /// 验证结果严重级别
        /// </summary>
        public enum Severity
        {
            Warning,
            Error
        }

        /// <summary>
        /// 单条验证消息
        /// </summary>
        public class ValidationMessage
        {
            public string Id;
            public Severity Severity;
            public string Message;

            public ValidationMessage(string id, Severity severity, string message)
            {
                Id = id;
                Severity = severity;
                Message = message;
            }
        }

        /// <summary>
        /// 对PCGGeometry进行数据一致性验证
        /// </summary>
        public static List<ValidationMessage> Validate(PCGGeometry geo)
        {
            var messages = new List<ValidationMessage>();

            if (geo == null)
            {
                messages.Add(new ValidationMessage("null_geometry", Severity.Error, "PCGGeometry is null"));
                return messages;
            }

            int pointCount = geo.Points.Count;
            int primCount = geo.Primitives.Count;
            int totalVertices = 0;
            foreach (var prim in geo.Primitives)
                totalVertices += prim.Length;

            // check_point_attrib_count
            foreach (var attr in geo.PointAttribs.GetAllAttributes())
            {
                if (attr.Values.Count != pointCount)
                {
                    messages.Add(new ValidationMessage(
                        "check_point_attrib_count",
                        Severity.Error,
                        $"PointAttrib '{attr.Name}': Values.Count={attr.Values.Count} != Points.Count={pointCount}"
                    ));
                }
            }

            // check_prim_attrib_count
            foreach (var attr in geo.PrimAttribs.GetAllAttributes())
            {
                if (attr.Values.Count != primCount)
                {
                    messages.Add(new ValidationMessage(
                        "check_prim_attrib_count",
                        Severity.Error,
                        $"PrimAttrib '{attr.Name}': Values.Count={attr.Values.Count} != Primitives.Count={primCount}"
                    ));
                }
            }

            // check_vertex_attrib_count
            foreach (var attr in geo.VertexAttribs.GetAllAttributes())
            {
                if (attr.Values.Count != totalVertices)
                {
                    messages.Add(new ValidationMessage(
                        "check_vertex_attrib_count",
                        Severity.Warning,
                        $"VertexAttrib '{attr.Name}': Values.Count={attr.Values.Count} != TotalVertices={totalVertices}"
                    ));
                }
            }

            // check_detail_attrib_count
            foreach (var attr in geo.DetailAttribs.GetAllAttributes())
            {
                if (attr.Values.Count != 1)
                {
                    messages.Add(new ValidationMessage(
                        "check_detail_attrib_count",
                        Severity.Warning,
                        $"DetailAttrib '{attr.Name}': Values.Count={attr.Values.Count} != 1"
                    ));
                }
            }

            // check_point_group_indices
            foreach (var kvp in geo.PointGroups)
            {
                foreach (var idx in kvp.Value)
                {
                    if (idx < 0 || idx >= pointCount)
                    {
                        messages.Add(new ValidationMessage(
                            "check_point_group_indices",
                            Severity.Error,
                            $"PointGroup '{kvp.Key}': index {idx} >= Points.Count={pointCount}"
                        ));
                        break;
                    }
                }
            }

            // check_prim_group_indices
            foreach (var kvp in geo.PrimGroups)
            {
                foreach (var idx in kvp.Value)
                {
                    if (idx < 0 || idx >= primCount)
                    {
                        messages.Add(new ValidationMessage(
                            "check_prim_group_indices",
                            Severity.Error,
                            $"PrimGroup '{kvp.Key}': index {idx} >= Primitives.Count={primCount}"
                        ));
                        break;
                    }
                }
            }

            // check_prim_vertex_indices
            for (int primIdx = 0; primIdx < geo.Primitives.Count; primIdx++)
            {
                var prim = geo.Primitives[primIdx];
                for (int i = 0; i < prim.Length; i++)
                {
                    int vertIdx = prim[i];
                    if (vertIdx < 0 || vertIdx >= pointCount)
                    {
                        messages.Add(new ValidationMessage(
                            "check_prim_vertex_indices",
                            Severity.Error,
                            $"Primitive[{primIdx}]: vertex index {vertIdx} out of range [0, {pointCount})"
                        ));
                        break;
                    }
                }
            }

            // check_edge_indices
            foreach (var edge in geo.Edges)
            {
                if (edge == null || edge.Length < 2)
                {
                    messages.Add(new ValidationMessage(
                        "check_edge_indices",
                        Severity.Warning,
                        $"Edge has invalid format (less than 2 vertices)"
                    ));
                    continue;
                }
                int a = edge[0];
                int b = edge[1];
                if (a < 0 || a >= pointCount || b < 0 || b >= pointCount)
                {
                    messages.Add(new ValidationMessage(
                        "check_edge_indices",
                        Severity.Warning,
                        $"Edge[0:{edge[0]},1:{edge[1]}]: index out of range [0, {pointCount})"
                    ));
                }
            }

            return messages;
        }

        /// <summary>
        /// 快速检查几何体是否有效（无错误，只有警告）
        /// </summary>
        public static bool IsValid(PCGGeometry geo)
        {
            var messages = Validate(geo);
            foreach (var msg in messages)
            {
                if (msg.Severity == Severity.Error)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 获取所有错误消息
        /// </summary>
        public static List<string> GetErrors(PCGGeometry geo)
        {
            var messages = Validate(geo);
            var errors = new List<string>();
            foreach (var msg in messages)
            {
                if (msg.Severity == Severity.Error)
                    errors.Add(msg.Message);
            }
            return errors;
        }

        /// <summary>
        /// 获取所有警告消息
        /// </summary>
        public static List<string> GetWarnings(PCGGeometry geo)
        {
            var messages = Validate(geo);
            var warnings = new List<string>();
            foreach (var msg in messages)
            {
                if (msg.Severity == Severity.Warning)
                    warnings.Add(msg.Message);
            }
            return warnings;
        }
    }
}
