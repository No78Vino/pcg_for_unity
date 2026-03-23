using System.Collections.Generic;
using System.Globalization;

namespace PCGToolkit.Core
{
    public static class JsonHelper
    {
        public static string Esc(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                    .Replace("\n", "\\n").Replace("\r", "\\r");
        }

        public static string F(float v) => v.ToString(CultureInfo.InvariantCulture);

        public static Dictionary<string, object> ParseSimpleJson(string json)
        {
            var dict = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json) || json.Trim() == "{}") return dict;

            json = json.Trim();
            if (json.StartsWith("{")) json = json.Substring(1);
            if (json.EndsWith("}")) json = json.Substring(0, json.Length - 1);

            int depth = 0;
            int start = 0;
            bool inString = false;
            var pairs = new List<string>();

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
            if (start < json.Length) pairs.Add(json.Substring(start));

            foreach (var pair in pairs)
            {
                var colonIdx = pair.IndexOf(':');
                if (colonIdx < 0) continue;
                var key = pair.Substring(0, colonIdx).Trim().Trim('"');
                var value = pair.Substring(colonIdx + 1).Trim();

                if (value.StartsWith("\"") && value.EndsWith("\""))
                    dict[key] = value.Trim('"');
                else if (value == "true") dict[key] = true;
                else if (value == "false") dict[key] = false;
                else if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float f))
                    dict[key] = f;
                else
                    dict[key] = value;
            }

            return dict;
        }
    }
}
