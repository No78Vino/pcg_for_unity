using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Skill
{
    public class PCGNodeSkillAdapter : ISkill
    {
        private readonly IPCGNode node;

        public PCGNodeSkillAdapter(IPCGNode node)
        {
            this.node = node;
        }

        public string Name => node.Name;
        public string DisplayName => node.DisplayName;
        public string Description => node.Description;

        public Type GetNodeType() => node.GetType();

        public string GetJsonSchema()
        {
            var parameters = GetParameters();

            var sb = new StringBuilder();
            sb.Append("{ ");
            sb.Append($"\"name\": \"{EscapeJson(node.Name)}\", ");
            sb.Append($"\"description\": \"{EscapeJson(node.Description)}\", ");
            sb.Append("\"parameters\": { \"type\": \"object\", \"properties\": { ");

            var requiredList = new List<string>();
            bool first = true;

            foreach (var param in parameters)
            {
                if (!first) sb.Append(", ");
                first = false;

                string jsonType = param.Type switch
                {
                    "float" => "number",
                    "int" => "integer",
                    "bool" => "boolean",
                    "string" => "string",
                    "vector3" => "array",
                    "color" => "array",
                    _ => "string"
                };

                sb.Append($"\"{EscapeJson(param.Name)}\": {{ ");
                sb.Append($"\"type\": \"{jsonType}\", ");
                sb.Append($"\"description\": \"{EscapeJson(param.Description)}\"");

                if (param.DefaultValue != null)
                    sb.Append($", \"default\": {SerializeDefault(param.DefaultValue)}");

                if (jsonType == "array")
                {
                    int itemCount = param.Type == "vector3" ? 3 : 4;
                    sb.Append($", \"items\": {{ \"type\": \"number\" }}, \"minItems\": {itemCount}, \"maxItems\": {itemCount}");
                }

                sb.Append(" }");

                if (param.Required) requiredList.Add(param.Name);
            }

            sb.Append(" }, ");
            sb.Append($"\"required\": [{string.Join(", ", requiredList.Select(r => $"\"{r}\""))}]");
            sb.Append(" } }");

            return sb.ToString();
        }

        public string Execute(string parametersJson)
        {
            try
            {
                var parameters = ParseJsonToDict(parametersJson);
                var ctx = new PCGContext(debug: true);

                var inputGeometries = new Dictionary<string, PCGGeometry>();
                if (parameters.ContainsKey("__input_geometry"))
                {
                    parameters.Remove("__input_geometry");
                }

                var typedParams = new Dictionary<string, object>();
                if (node.Inputs != null)
                {
                    foreach (var input in node.Inputs)
                    {
                        if (input.PortType == PCGPortType.Geometry) continue;
                        if (!parameters.ContainsKey(input.Name))
                        {
                            if (input.DefaultValue != null)
                                typedParams[input.Name] = input.DefaultValue;
                            continue;
                        }

                        object raw = parameters[input.Name];
                        typedParams[input.Name] = input.PortType switch
                        {
                            PCGPortType.Float => Convert.ToSingle(raw, CultureInfo.InvariantCulture),
                            PCGPortType.Int => Convert.ToInt32(raw, CultureInfo.InvariantCulture),
                            PCGPortType.Bool => Convert.ToBoolean(raw),
                            PCGPortType.String => raw.ToString(),
                            PCGPortType.Vector3 => ParseVector3(raw),
                            PCGPortType.Color => ParseColor(raw),
                            _ => raw
                        };
                    }
                }

                var nodeInstance = (IPCGNode)Activator.CreateInstance(node.GetType());
                ctx.CurrentNodeId = $"skill_{node.Name}";
                var result = nodeInstance.Execute(ctx, inputGeometries, typedParams);

                var response = new StringBuilder();
                response.Append("{ \"success\": true");

                if (ctx.HasError)
                    response.Append($", \"warning\": \"{EscapeJson(ctx.ErrorMessage)}\"");

                response.Append(", \"outputs\": { ");
                bool outputFirst = true;
                if (result != null)
                {
                    foreach (var kvp in result)
                    {
                        if (!outputFirst) response.Append(", ");
                        outputFirst = false;

                        var geo = kvp.Value;
                        response.Append($"\"{kvp.Key}\": {{ ");
                        response.Append($"\"pointCount\": {geo?.Points.Count ?? 0}, ");
                        response.Append($"\"primCount\": {geo?.Primitives.Count ?? 0}");

                        if (geo != null && geo.Points.Count > 0)
                        {
                            var bounds = ComputeBounds(geo);
                            response.Append($", \"boundsMin\": [{F(bounds.min.x)}, {F(bounds.min.y)}, {F(bounds.min.z)}]");
                            response.Append($", \"boundsMax\": [{F(bounds.max.x)}, {F(bounds.max.y)}, {F(bounds.max.z)}]");
                        }

                        response.Append(" }");
                    }
                }
                response.Append(" } }");

                return response.ToString();
            }
            catch (Exception e)
            {
                return $"{{ \"success\": false, \"error\": \"{EscapeJson(e.Message)}\" }}";
            }
        }

        public string ExecuteWithGeometry(PCGGeometry inputGeo, Dictionary<string, object> parameters)
        {
            var ctx = new PCGContext(debug: true);
            var inputGeometries = new Dictionary<string, PCGGeometry>();
            if (inputGeo != null)
                inputGeometries["input"] = inputGeo;

            var nodeInstance = (IPCGNode)Activator.CreateInstance(node.GetType());
            ctx.CurrentNodeId = $"pipeline_{node.Name}";
            var result = nodeInstance.Execute(ctx, inputGeometries, parameters ?? new Dictionary<string, object>());

            return result?.Values.FirstOrDefault() != null ? "success" : "empty";
        }

        public PCGGeometry ExecuteAndGetGeometry(PCGGeometry inputGeo, Dictionary<string, object> parameters)
        {
            var ctx = new PCGContext(debug: true);
            var inputGeometries = new Dictionary<string, PCGGeometry>();
            if (inputGeo != null)
                inputGeometries["input"] = inputGeo;

            var nodeInstance = (IPCGNode)Activator.CreateInstance(node.GetType());
            ctx.CurrentNodeId = $"pipeline_{node.Name}";
            var result = nodeInstance.Execute(ctx, inputGeometries, parameters ?? new Dictionary<string, object>());

            return result?.Values.FirstOrDefault();
        }

        public List<SkillParameter> GetParameters()
        {
            var parameters = new List<SkillParameter>();

            if (node.Inputs != null)
            {
                foreach (var input in node.Inputs)
                {
                    if (input.PortType == PCGPortType.Geometry) continue;

                    parameters.Add(new SkillParameter
                    {
                        Name = input.Name,
                        Type = input.PortType.ToString().ToLower(),
                        Description = input.Description,
                        DefaultValue = input.DefaultValue,
                        Required = input.Required,
                    });
                }
            }

            return parameters;
        }

        private static Dictionary<string, object> ParseJsonToDict(string json)
        {
            var dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json) || json == "{}") return dict;

            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            foreach (var pair in SplitJsonPairs(json))
            {
                var colonIdx = pair.IndexOf(':');
                if (colonIdx < 0) continue;

                var key = pair.Substring(0, colonIdx).Trim().Trim('"');
                var value = pair.Substring(colonIdx + 1).Trim();

                if (value.StartsWith("\"") && value.EndsWith("\""))
                    dict[key] = value.Trim('"');
                else if (value == "true")
                    dict[key] = true;
                else if (value == "false")
                    dict[key] = false;
                else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    dict[key] = f;
                else
                    dict[key] = value;
            }

            return dict;
        }

        private static List<string> SplitJsonPairs(string json)
        {
            var pairs = new List<string>();
            int depth = 0;
            int start = 0;
            bool inString = false;

            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                if (c == '"' && (i == 0 || json[i - 1] != '\\')) inString = !inString;
                if (inString) continue;

                if (c == '{' || c == '[') depth++;
                else if (c == '}' || c == ']') depth--;
                else if (c == ',' && depth == 0)
                {
                    pairs.Add(json.Substring(start, i - start));
                    start = i + 1;
                }
            }

            if (start < json.Length)
                pairs.Add(json.Substring(start));

            return pairs;
        }

        private static Vector3 ParseVector3(object raw)
        {
            if (raw is Vector3 v) return v;
            var str = raw.ToString().Trim('[', ']', '(', ')');
            var parts = str.Split(',');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float x);
                float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float y);
                float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float z);
                return new Vector3(x, y, z);
            }
            return Vector3.zero;
        }

        private static Color ParseColor(object raw)
        {
            if (raw is Color c) return c;
            var str = raw.ToString().Trim('[', ']', '(', ')');
            var parts = str.Split(',');
            if (parts.Length >= 3)
            {
                float.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float r);
                float.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float g);
                float.TryParse(parts[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float b);
                float a = 1f;
                if (parts.Length >= 4)
                    float.TryParse(parts[3].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out a);
                return new Color(r, g, b, a);
            }
            return Color.white;
        }

        private static Bounds ComputeBounds(PCGGeometry geo)
        {
            if (geo.Points.Count == 0) return new Bounds();
            var min = geo.Points[0];
            var max = geo.Points[0];
            for (int i = 1; i < geo.Points.Count; i++)
            {
                min = Vector3.Min(min, geo.Points[i]);
                max = Vector3.Max(max, geo.Points[i]);
            }
            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        private static string SerializeDefault(object value)
        {
            if (value is float f) return F(f);
            if (value is int i) return i.ToString();
            if (value is bool b) return b ? "true" : "false";
            if (value is string s) return $"\"{EscapeJson(s)}\"";
            if (value is Vector3 v) return $"[{F(v.x)}, {F(v.y)}, {F(v.z)}]";
            if (value is Color c) return $"[{F(c.r)}, {F(c.g)}, {F(c.b)}, {F(c.a)}]";
            return $"\"{value}\"";
        }

        private static string F(float v) => v.ToString(CultureInfo.InvariantCulture);

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r");
        }
    }
}
