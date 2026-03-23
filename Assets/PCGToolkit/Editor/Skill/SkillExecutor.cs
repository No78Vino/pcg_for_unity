using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using PCGToolkit.Communication;
using PCGToolkit.Core;

namespace PCGToolkit.Skill
{
    public class SkillExecutor
    {
        public string ExecuteSkill(string skillName, string parametersJson)
        {
            var skill = SkillRegistry.GetSkill(skillName);
            if (skill == null)
            {
                return AgentProtocol.CreateErrorResponse($"Skill not found: {skillName}");
            }

            return skill.Execute(parametersJson);
        }

        public string ExecutePipeline(string[] skillNames, string[] parametersJsonArray)
        {
            if (skillNames == null || skillNames.Length == 0)
                return AgentProtocol.CreateErrorResponse("Pipeline requires at least one skill");

            PCGGeometry currentGeo = null;
            var stepResults = new List<string>();

            for (int i = 0; i < skillNames.Length; i++)
            {
                var skill = SkillRegistry.GetSkill(skillNames[i]);
                if (skill == null)
                    return AgentProtocol.CreateErrorResponse(
                        $"Pipeline step {i}: Skill not found: {skillNames[i]}");

                if (skill is PCGNodeSkillAdapter adapter)
                {
                    string paramsJson = i < parametersJsonArray?.Length
                        ? parametersJsonArray[i]
                        : "{}";

                    var parameters = ParseSimpleJson(paramsJson);
                    currentGeo = adapter.ExecuteAndGetGeometry(currentGeo, parameters);

                    int pts = currentGeo?.Points.Count ?? 0;
                    int prims = currentGeo?.Primitives.Count ?? 0;
                    stepResults.Add($"{{ \"skill\": \"{skillNames[i]}\", \"pointCount\": {pts}, \"primCount\": {prims} }}");

                    if (currentGeo == null || currentGeo.Points.Count == 0)
                    {
                        Debug.LogWarning($"Pipeline step {i} ({skillNames[i]}) produced empty geometry");
                    }
                }
                else
                {
                    string paramsJson = i < parametersJsonArray?.Length
                        ? parametersJsonArray[i]
                        : "{}";
                    string resultJson = skill.Execute(paramsJson);
                    stepResults.Add(resultJson);
                }
            }

            int finalPts = currentGeo?.Points.Count ?? 0;
            int finalPrims = currentGeo?.Primitives.Count ?? 0;

            var sb = new StringBuilder();
            sb.Append("{ \"success\": true, ");
            sb.Append($"\"finalPointCount\": {finalPts}, ");
            sb.Append($"\"finalPrimCount\": {finalPrims}, ");
            sb.Append("\"steps\": [");
            sb.Append(string.Join(", ", stepResults));
            sb.Append("] }");

            return sb.ToString();
        }

        public string ListSkills()
        {
            return SkillSchemaExporter.ExportAll();
        }

        private static Dictionary<string, object> ParseSimpleJson(string json)
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
