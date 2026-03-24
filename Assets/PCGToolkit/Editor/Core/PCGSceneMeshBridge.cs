using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PCGToolkit.Core
{
    /// <summary>
    /// Bridges PCGGeometry to a temporary scene Mesh+MeshCollider for interactive selection.
    /// Maintains bidirectional index mappings between Unity triangles and PCG Primitives.
    /// </summary>
    public class PCGSceneMeshBridge
    {
        public GameObject TempGameObject { get; private set; }
        public Mesh TempMesh { get; private set; }
        public PCGGeometry Geometry { get; private set; }

        /// <summary>Unity triangle index (triangleIndex from Raycast) -> PCG Primitive index</summary>
        public Dictionary<int, int> UnityTriToPcgPrim { get; private set; } = new Dictionary<int, int>();

        /// <summary>Unity vertex index -> PCG Point index (1:1 since vertices are directly copied)</summary>
        public Dictionary<int, int> UnityVertToPcgPoint { get; private set; } = new Dictionary<int, int>();

        /// <summary>PCG Primitive index -> list of Unity triangle indices</summary>
        public Dictionary<int, List<int>> PcgPrimToUnityTris { get; private set; } = new Dictionary<int, List<int>>();

        public bool IsValid => TempGameObject != null && TempMesh != null && Geometry != null;

        public void Instantiate(PCGGeometry geo)
        {
            Dispose();

            if (geo == null || geo.Points.Count == 0) return;

            Geometry = geo;
            UnityTriToPcgPrim.Clear();
            UnityVertToPcgPoint.Clear();
            PcgPrimToUnityTris.Clear();

            // Vertex mapping is 1:1
            for (int i = 0; i < geo.Points.Count; i++)
                UnityVertToPcgPoint[i] = i;

            // Build mesh with triangle-to-prim tracking
            TempMesh = new Mesh();
            TempMesh.name = "PCGSelectionMesh";
            TempMesh.vertices = geo.Points.ToArray();

            var triangles = new List<int>();
            int unityTriIndex = 0;

            for (int primIdx = 0; primIdx < geo.Primitives.Count; primIdx++)
            {
                var prim = geo.Primitives[primIdx];
                int trisBefore = triangles.Count / 3;

                if (prim.Length == 3)
                {
                    triangles.Add(prim[0]);
                    triangles.Add(prim[1]);
                    triangles.Add(prim[2]);
                }
                else if (prim.Length == 4)
                {
                    triangles.Add(prim[0]);
                    triangles.Add(prim[1]);
                    triangles.Add(prim[2]);
                    triangles.Add(prim[0]);
                    triangles.Add(prim[2]);
                    triangles.Add(prim[3]);
                }
                else if (prim.Length > 4)
                {
                    TriangulateNgon(geo.Points, prim, triangles);
                }

                int trisAfter = triangles.Count / 3;
                var triList = new List<int>();

                for (int t = trisBefore; t < trisAfter; t++)
                {
                    UnityTriToPcgPrim[t] = primIdx;
                    triList.Add(t);
                }

                PcgPrimToUnityTris[primIdx] = triList;
            }

            TempMesh.triangles = triangles.ToArray();

            // Apply normals
            var normalAttr = geo.PointAttribs.GetAttribute("N");
            if (normalAttr != null && normalAttr.Values.Count == geo.Points.Count)
            {
                var normals = new Vector3[geo.Points.Count];
                for (int i = 0; i < geo.Points.Count; i++)
                    normals[i] = normalAttr.Values[i] is Vector3 n ? n : Vector3.up;
                TempMesh.normals = normals;
            }
            else
            {
                TempMesh.RecalculateNormals();
            }

            TempMesh.RecalculateBounds();

            // Create temp GameObject
            TempGameObject = new GameObject("__PCGSelection__");
            TempGameObject.hideFlags = HideFlags.HideAndDontSave;

            var mf = TempGameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = TempMesh;

            var mr = TempGameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            mr.enabled = false; // We don't need to render it, just need collider for raycast

            var mc = TempGameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = TempMesh;

            // Build edges if needed
            geo.BuildEdges();
        }

        public void Dispose()
        {
            if (TempGameObject != null)
                Object.DestroyImmediate(TempGameObject);
            if (TempMesh != null)
                Object.DestroyImmediate(TempMesh);

            TempGameObject = null;
            TempMesh = null;
            Geometry = null;
            UnityTriToPcgPrim.Clear();
            UnityVertToPcgPoint.Clear();
            PcgPrimToUnityTris.Clear();
        }

        /// <summary>
        /// Get the center position of a PCG Primitive in world space.
        /// </summary>
        public Vector3 GetPrimCenter(int primIdx)
        {
            if (Geometry == null || primIdx < 0 || primIdx >= Geometry.Primitives.Count)
                return Vector3.zero;

            var prim = Geometry.Primitives[primIdx];
            Vector3 center = Vector3.zero;
            foreach (int vi in prim)
                center += Geometry.Points[vi];
            return center / prim.Length;
        }

        /// <summary>
        /// Get all vertex positions for a PCG Primitive.
        /// </summary>
        public Vector3[] GetPrimVertices(int primIdx)
        {
            if (Geometry == null || primIdx < 0 || primIdx >= Geometry.Primitives.Count)
                return new Vector3[0];

            var prim = Geometry.Primitives[primIdx];
            var verts = new Vector3[prim.Length];
            for (int i = 0; i < prim.Length; i++)
                verts[i] = Geometry.Points[prim[i]];
            return verts;
        }

        /// <summary>
        /// Find the closest vertex to a world position.
        /// First checks the hit triangle, then expands to all triangles sharing its vertices
        /// to avoid missing closer vertices on adjacent faces.
        /// Returns the PCG Point index.
        /// </summary>
        public int FindClosestVertex(int unityTriIndex, Vector3 worldPos)
        {
            if (TempMesh == null) return -1;

            var tris = TempMesh.triangles;
            int baseIdx = unityTriIndex * 3;
            if (baseIdx + 2 >= tris.Length) return -1;

            var verts = TempMesh.vertices;

            // Collect candidate PCG point indices from the hit triangle and its neighbors
            var candidatePcgPoints = new HashSet<int>();
            var seedVerts = new HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                int vi = tris[baseIdx + i];
                if (vi < verts.Length)
                    seedVerts.Add(vi);
            }

            // Expand: find all triangles sharing any of the seed vertices
            for (int t = 0; t < tris.Length; t += 3)
            {
                bool shares = false;
                for (int i = 0; i < 3; i++)
                {
                    if (seedVerts.Contains(tris[t + i]))
                    {
                        shares = true;
                        break;
                    }
                }
                if (shares)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int vi = tris[t + i];
                        if (vi < verts.Length && UnityVertToPcgPoint.TryGetValue(vi, out int pcgIdx))
                            candidatePcgPoints.Add(pcgIdx);
                    }
                }
            }

            float minDist = float.MaxValue;
            int closestPcgIdx = -1;
            foreach (int pcgIdx in candidatePcgPoints)
            {
                if (pcgIdx < 0 || pcgIdx >= Geometry.Points.Count) continue;
                float dist = Vector3.Distance(Geometry.Points[pcgIdx], worldPos);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestPcgIdx = pcgIdx;
                }
            }

            return closestPcgIdx;
        }

        /// <summary>
        /// Find the closest edge to a world position.
        /// Expands search to all triangles sharing vertices with the hit triangle
        /// to avoid missing edges on adjacent faces.
        /// Returns the PCG Edge index, or -1 if not found.
        /// </summary>
        public int FindClosestEdge(int unityTriIndex, Vector3 worldPos)
        {
            if (TempMesh == null || Geometry == null || Geometry.Edges.Count == 0) return -1;

            var tris = TempMesh.triangles;
            int baseIdx = unityTriIndex * 3;
            if (baseIdx + 2 >= tris.Length) return -1;

            var verts = TempMesh.vertices;

            // Collect candidate PCG point indices from hit triangle + neighbors
            var seedVerts = new HashSet<int>();
            for (int i = 0; i < 3; i++)
            {
                int vi = tris[baseIdx + i];
                if (vi < verts.Length)
                    seedVerts.Add(vi);
            }

            var candidatePcgPoints = new HashSet<int>();
            for (int t = 0; t < tris.Length; t += 3)
            {
                bool shares = false;
                for (int i = 0; i < 3; i++)
                {
                    if (seedVerts.Contains(tris[t + i]))
                    {
                        shares = true;
                        break;
                    }
                }
                if (shares)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        int vi = tris[t + i];
                        if (vi < verts.Length && UnityVertToPcgPoint.TryGetValue(vi, out int pcgIdx))
                            candidatePcgPoints.Add(pcgIdx);
                    }
                }
            }

            // Find the closest PCG edge whose both endpoints are in the candidate set
            float minDist = float.MaxValue;
            int closestEdgeIdx = -1;

            for (int e = 0; e < Geometry.Edges.Count; e++)
            {
                int ea = Geometry.Edges[e][0];
                int eb = Geometry.Edges[e][1];
                if (!candidatePcgPoints.Contains(ea) || !candidatePcgPoints.Contains(eb))
                    continue;

                float dist = DistanceToSegment(worldPos, Geometry.Points[ea], Geometry.Points[eb]);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestEdgeIdx = e;
                }
            }

            return closestEdgeIdx;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / ab.sqrMagnitude);
            Vector3 closest = a + t * ab;
            return Vector3.Distance(point, closest);
        }

        // Duplicate of the ear-clipping triangulation from PCGGeometryToMesh
        private static void TriangulateNgon(List<Vector3> allPoints, int[] prim, List<int> triangles)
        {
            if (prim.Length < 3) return;

            Vector3 normal = Vector3.zero;
            for (int i = 0; i < prim.Length; i++)
            {
                Vector3 curr = allPoints[prim[i]];
                Vector3 next = allPoints[prim[(i + 1) % prim.Length]];
                normal.x += (curr.y - next.y) * (curr.z + next.z);
                normal.y += (curr.z - next.z) * (curr.x + next.x);
                normal.z += (curr.x - next.x) * (curr.y + next.y);
            }
            if (normal.sqrMagnitude > 0.0001f) normal.Normalize();
            else normal = Vector3.up;

            var indices = new List<int>(prim);
            int safety = indices.Count * indices.Count;

            while (indices.Count > 3 && safety-- > 0)
            {
                bool earFound = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int prev = (i - 1 + indices.Count) % indices.Count;
                    int next = (i + 1) % indices.Count;

                    Vector3 a = allPoints[indices[prev]];
                    Vector3 b = allPoints[indices[i]];
                    Vector3 c = allPoints[indices[next]];

                    Vector3 cross = Vector3.Cross(b - a, c - b);
                    if (Vector3.Dot(cross, normal) < 0) continue;

                    bool isEar = true;
                    for (int j = 0; j < indices.Count; j++)
                    {
                        if (j == prev || j == i || j == next) continue;
                        if (PointInTriangle(allPoints[indices[j]], a, b, c))
                        {
                            isEar = false;
                            break;
                        }
                    }

                    if (isEar)
                    {
                        triangles.Add(indices[prev]);
                        triangles.Add(indices[i]);
                        triangles.Add(indices[next]);
                        indices.RemoveAt(i);
                        earFound = true;
                        break;
                    }
                }

                if (!earFound)
                {
                    for (int i = 1; i < indices.Count - 1; i++)
                    {
                        triangles.Add(indices[0]);
                        triangles.Add(indices[i]);
                        triangles.Add(indices[i + 1]);
                    }
                    break;
                }
            }

            if (indices.Count == 3)
            {
                triangles.Add(indices[0]);
                triangles.Add(indices[1]);
                triangles.Add(indices[2]);
            }
        }

        private static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 v0 = c - a, v1 = b - a, v2 = p - a;
            float dot00 = Vector3.Dot(v0, v0);
            float dot01 = Vector3.Dot(v0, v1);
            float dot02 = Vector3.Dot(v0, v2);
            float dot11 = Vector3.Dot(v1, v1);
            float dot12 = Vector3.Dot(v1, v2);
            float denom = dot00 * dot11 - dot01 * dot01;
            if (Mathf.Abs(denom) < 1e-8f) return false;
            float inv = 1f / denom;
            float u = (dot11 * dot02 - dot01 * dot12) * inv;
            float v = (dot00 * dot12 - dot01 * dot02) * inv;
            return u >= 0 && v >= 0 && u + v <= 1;
        }
    }
}
