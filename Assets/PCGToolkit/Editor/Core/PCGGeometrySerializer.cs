using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace PCGToolkit.Core
{
    public static class PCGGeometrySerializer
    {
        public const int Version = 1;
        public static readonly byte[] MagicBytes = { 0x50, 0x43, 0x47, 0x43 }; // 'PCGC'
        public const string FileExtension = ".pcgcache";

        public static void Serialize(PCGGeometry geo, BinaryWriter writer)
        {
            writer.Write(MagicBytes);
            writer.Write(Version);

            // Points
            writer.Write(geo.Points.Count);
            foreach (var p in geo.Points)
            {
                writer.Write(p.x);
                writer.Write(p.y);
                writer.Write(p.z);
            }

            // Primitives
            writer.Write(geo.Primitives.Count);
            foreach (var prim in geo.Primitives)
            {
                writer.Write(prim.Length);
                foreach (var idx in prim)
                    writer.Write(idx);
            }

            // Edges
            writer.Write(geo.Edges.Count);
            foreach (var edge in geo.Edges)
            {
                writer.Write(edge[0]);
                writer.Write(edge[1]);
            }

            // Attribute stores
            SerializeAttributeStore(geo.PointAttribs, writer);
            SerializeAttributeStore(geo.VertexAttribs, writer);
            SerializeAttributeStore(geo.PrimAttribs, writer);
            SerializeAttributeStore(geo.DetailAttribs, writer);

            // Groups
            SerializeGroups(geo.PointGroups, writer);
            SerializeGroups(geo.PrimGroups, writer);
        }

        public static PCGGeometry Deserialize(BinaryReader reader)
        {
            // Validate magic bytes
            var magic = reader.ReadBytes(4);
            if (magic.Length != 4 || magic[0] != MagicBytes[0] || magic[1] != MagicBytes[1] ||
                magic[2] != MagicBytes[2] || magic[3] != MagicBytes[3])
                throw new InvalidDataException("Invalid PCGCache file: magic bytes mismatch");

            int version = reader.ReadInt32();
            if (version != Version)
                throw new InvalidDataException($"Unsupported PCGCache version: {version}");

            var geo = new PCGGeometry();

            // Points
            int pointCount = reader.ReadInt32();
            for (int i = 0; i < pointCount; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                geo.Points.Add(new Vector3(x, y, z));
            }

            // Primitives
            int primCount = reader.ReadInt32();
            for (int i = 0; i < primCount; i++)
            {
                int len = reader.ReadInt32();
                var prim = new int[len];
                for (int j = 0; j < len; j++)
                    prim[j] = reader.ReadInt32();
                geo.Primitives.Add(prim);
            }

            // Edges
            int edgeCount = reader.ReadInt32();
            for (int i = 0; i < edgeCount; i++)
            {
                int a = reader.ReadInt32();
                int b = reader.ReadInt32();
                geo.Edges.Add(new int[] { a, b });
            }

            // Attribute stores
            geo.PointAttribs = DeserializeAttributeStore(reader);
            geo.VertexAttribs = DeserializeAttributeStore(reader);
            geo.PrimAttribs = DeserializeAttributeStore(reader);
            geo.DetailAttribs = DeserializeAttributeStore(reader);

            // Groups
            geo.PointGroups = DeserializeGroups(reader);
            geo.PrimGroups = DeserializeGroups(reader);

            return geo;
        }

        public static long SerializeToFile(PCGGeometry geo, string filePath)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            using (var writer = new BinaryWriter(fs))
            {
                Serialize(geo, writer);
                return fs.Length;
            }
        }

        public static PCGGeometry DeserializeFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return null;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(fs))
            {
                return Deserialize(reader);
            }
        }

        private static void SerializeAttributeStore(AttributeStore store, BinaryWriter writer)
        {
            var names = store.GetAttributeNames().ToList();
            writer.Write(names.Count);

            foreach (var name in names)
            {
                var attr = store.GetAttribute(name);
                writer.Write(attr.Name);
                writer.Write((int)attr.Type);
                writer.Write(attr.Values.Count);

                foreach (var val in attr.Values)
                {
                    SerializeValue(attr.Type, val, writer);
                }
            }
        }

        private static AttributeStore DeserializeAttributeStore(BinaryReader reader)
        {
            var store = new AttributeStore();
            int attrCount = reader.ReadInt32();

            for (int i = 0; i < attrCount; i++)
            {
                string name = reader.ReadString();
                AttribType type = (AttribType)reader.ReadInt32();
                int valueCount = reader.ReadInt32();

                var attr = store.CreateAttribute(name, type);
                for (int j = 0; j < valueCount; j++)
                {
                    attr.Values.Add(DeserializeValue(type, reader));
                }
            }

            return store;
        }

        private static void SerializeValue(AttribType type, object val, BinaryWriter writer)
        {
            switch (type)
            {
                case AttribType.Float:
                    writer.Write(val is float f ? f : val is double d ? (float)d : 0f);
                    break;
                case AttribType.Int:
                    writer.Write(val is int i ? i : 0);
                    break;
                case AttribType.Vector2:
                    var v2 = val is Vector2 vec2 ? vec2 : Vector2.zero;
                    writer.Write(v2.x);
                    writer.Write(v2.y);
                    break;
                case AttribType.Vector3:
                    var v3 = val is Vector3 vec3 ? vec3 : Vector3.zero;
                    writer.Write(v3.x);
                    writer.Write(v3.y);
                    writer.Write(v3.z);
                    break;
                case AttribType.Vector4:
                    var v4 = val is Vector4 vec4 ? vec4 : Vector4.zero;
                    writer.Write(v4.x);
                    writer.Write(v4.y);
                    writer.Write(v4.z);
                    writer.Write(v4.w);
                    break;
                case AttribType.Color:
                    var c = val is Color col ? col : Color.white;
                    writer.Write(c.r);
                    writer.Write(c.g);
                    writer.Write(c.b);
                    writer.Write(c.a);
                    break;
                case AttribType.String:
                    writer.Write(val is string s ? s : "");
                    break;
            }
        }

        private static object DeserializeValue(AttribType type, BinaryReader reader)
        {
            switch (type)
            {
                case AttribType.Float:
                    return reader.ReadSingle();
                case AttribType.Int:
                    return reader.ReadInt32();
                case AttribType.Vector2:
                    return new Vector2(reader.ReadSingle(), reader.ReadSingle());
                case AttribType.Vector3:
                    return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case AttribType.Vector4:
                    return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case AttribType.Color:
                    return new Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                case AttribType.String:
                    return reader.ReadString();
                default:
                    return null;
            }
        }

        private static void SerializeGroups(Dictionary<string, HashSet<int>> groups, BinaryWriter writer)
        {
            writer.Write(groups.Count);
            foreach (var kvp in groups)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value.Count);
                foreach (var idx in kvp.Value)
                    writer.Write(idx);
            }
        }

        private static Dictionary<string, HashSet<int>> DeserializeGroups(BinaryReader reader)
        {
            var groups = new Dictionary<string, HashSet<int>>();
            int groupCount = reader.ReadInt32();

            for (int i = 0; i < groupCount; i++)
            {
                string name = reader.ReadString();
                int count = reader.ReadInt32();
                var set = new HashSet<int>();
                for (int j = 0; j < count; j++)
                    set.Add(reader.ReadInt32());
                groups[name] = set;
            }

            return groups;
        }

        public static string ComputeHash(PCGGeometry geo)
        {
            if (geo == null) return "null";

            using (var sha = SHA256.Create())
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(geo.Points.Count);
                    writer.Write(geo.Primitives.Count);

                    // Hash first min(64, Points.Count) vertices
                    int sampleCount = Math.Min(64, geo.Points.Count);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        writer.Write(geo.Points[i].x);
                        writer.Write(geo.Points[i].y);
                        writer.Write(geo.Points[i].z);
                    }

                    // Hash attribute names
                    foreach (var name in geo.PointAttribs.GetAttributeNames())
                        writer.Write(name);
                    foreach (var name in geo.VertexAttribs.GetAttributeNames())
                        writer.Write(name);
                    foreach (var name in geo.PrimAttribs.GetAttributeNames())
                        writer.Write(name);
                    foreach (var name in geo.DetailAttribs.GetAttributeNames())
                        writer.Write(name);

                    writer.Flush();
                    var hash = sha.ComputeHash(ms.ToArray());
                    var sb = new StringBuilder(16);
                    for (int i = 0; i < 8; i++)
                        sb.Append(hash[i].ToString("x2"));
                    return sb.ToString();
                }
            }
        }
    }
}
