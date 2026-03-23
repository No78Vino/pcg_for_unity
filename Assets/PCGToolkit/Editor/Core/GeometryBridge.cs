using System.Collections.Generic;
using UnityEngine;
using g3;

namespace PCGToolkit.Core
{
    public static class GeometryBridge
    {
        public static DMesh3 ToDMesh3(PCGGeometry geo)
        {
            if (geo == null || geo.Points.Count == 0)
                return new DMesh3();

            bool hasNormals = geo.PointAttribs.HasAttribute("N");
            bool hasUVs = geo.PointAttribs.HasAttribute("uv") || geo.VertexAttribs.HasAttribute("uv");
            bool hasColors = geo.PointAttribs.HasAttribute("Cd");

            var mesh = new DMesh3(hasNormals, hasColors, hasUVs, true);

            PCGAttribute normalAttr = hasNormals ? geo.PointAttribs.GetAttribute("N") : null;
            PCGAttribute uvAttr = geo.PointAttribs.HasAttribute("uv")
                ? geo.PointAttribs.GetAttribute("uv")
                : (geo.VertexAttribs.HasAttribute("uv") ? geo.VertexAttribs.GetAttribute("uv") : null);
            PCGAttribute colorAttr = hasColors ? geo.PointAttribs.GetAttribute("Cd") : null;

            for (int i = 0; i < geo.Points.Count; i++)
            {
                var p = geo.Points[i];
                var info = new NewVertexInfo(new Vector3d(p.x, p.y, p.z));

                if (normalAttr != null && i < normalAttr.Values.Count)
                {
                    var n = (Vector3)normalAttr.Values[i];
                    info.bHaveN = true;
                    info.n = new Vector3f(n.x, n.y, n.z);
                }

                if (uvAttr != null && i < uvAttr.Values.Count)
                {
                    object uvVal = uvAttr.Values[i];
                    if (uvVal is Vector2 uv2)
                    {
                        info.bHaveUV = true;
                        info.uv = new Vector2f(uv2.x, uv2.y);
                    }
                    else if (uvVal is Vector3 uv3)
                    {
                        info.bHaveUV = true;
                        info.uv = new Vector2f(uv3.x, uv3.y);
                    }
                }

                if (colorAttr != null && i < colorAttr.Values.Count)
                {
                    var cd = (Vector3)colorAttr.Values[i];
                    info.bHaveC = true;
                    info.c = new Vector3f(cd.x, cd.y, cd.z);
                }

                mesh.AppendVertex(info);
            }

            for (int pi = 0; pi < geo.Primitives.Count; pi++)
            {
                var prim = geo.Primitives[pi];
                if (prim.Length < 3) continue;

                int groupId = 0;
                foreach (var kvp in geo.PrimGroups)
                {
                    if (kvp.Value.Contains(pi))
                    {
                        groupId = kvp.Key.GetHashCode() & 0x7FFFFFFF;
                        break;
                    }
                }

                if (prim.Length == 3)
                {
                    mesh.AppendTriangle(prim[0], prim[1], prim[2], groupId);
                }
                else if (prim.Length == 4)
                {
                    mesh.AppendTriangle(prim[0], prim[1], prim[2], groupId);
                    mesh.AppendTriangle(prim[0], prim[2], prim[3], groupId);
                }
                else
                {
                    for (int j = 1; j < prim.Length - 1; j++)
                    {
                        mesh.AppendTriangle(prim[0], prim[j], prim[j + 1], groupId);
                    }
                }
            }

            return mesh;
        }

        public static PCGGeometry FromDMesh3(DMesh3 mesh)
        {
            var geo = new PCGGeometry();
            if (mesh == null || mesh.VertexCount == 0)
                return geo;

            var compactMesh = new DMesh3(mesh, true);

            var vertexMap = new Dictionary<int, int>();
            int newIdx = 0;

            foreach (int vid in compactMesh.VertexIndices())
            {
                var v = compactMesh.GetVertex(vid);
                geo.Points.Add(new Vector3((float)v.x, (float)v.y, (float)v.z));
                vertexMap[vid] = newIdx;
                newIdx++;
            }

            if (compactMesh.HasVertexNormals)
            {
                var normalAttr = geo.PointAttribs.CreateAttribute("N", AttribType.Vector3, Vector3.up);
                foreach (int vid in compactMesh.VertexIndices())
                {
                    var n = compactMesh.GetVertexNormal(vid);
                    normalAttr.Values.Add(new Vector3(n.x, n.y, n.z));
                }
            }

            if (compactMesh.HasVertexUVs)
            {
                var uvAttr = geo.PointAttribs.CreateAttribute("uv", AttribType.Vector2, Vector2.zero);
                foreach (int vid in compactMesh.VertexIndices())
                {
                    var uv = compactMesh.GetVertexUV(vid);
                    uvAttr.Values.Add(new Vector2(uv.x, uv.y));
                }
            }

            if (compactMesh.HasVertexColors)
            {
                var colorAttr = geo.PointAttribs.CreateAttribute("Cd", AttribType.Vector3, Vector3.one);
                foreach (int vid in compactMesh.VertexIndices())
                {
                    var c = compactMesh.GetVertexColor(vid);
                    colorAttr.Values.Add(new Vector3(c.x, c.y, c.z));
                }
            }

            var groupMap = new Dictionary<int, string>();
            foreach (int tid in compactMesh.TriangleIndices())
            {
                var tri = compactMesh.GetTriangle(tid);
                geo.Primitives.Add(new int[]
                {
                    vertexMap.ContainsKey(tri.a) ? vertexMap[tri.a] : tri.a,
                    vertexMap.ContainsKey(tri.b) ? vertexMap[tri.b] : tri.b,
                    vertexMap.ContainsKey(tri.c) ? vertexMap[tri.c] : tri.c
                });

                if (compactMesh.HasTriangleGroups)
                {
                    int gid = compactMesh.GetTriangleGroup(tid);
                    if (gid != 0)
                    {
                        if (!groupMap.ContainsKey(gid))
                            groupMap[gid] = $"group_{gid}";

                        string groupName = groupMap[gid];
                        if (!geo.PrimGroups.ContainsKey(groupName))
                            geo.PrimGroups[groupName] = new HashSet<int>();
                        geo.PrimGroups[groupName].Add(geo.Primitives.Count - 1);
                    }
                }
            }

            return geo;
        }
    }
}
