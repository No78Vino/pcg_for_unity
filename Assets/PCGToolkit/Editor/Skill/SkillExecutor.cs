using System;
using System.Collections.Generic;
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

                    var parameters = JsonHelper.ParseSimpleJson(paramsJson);
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
    }
}
