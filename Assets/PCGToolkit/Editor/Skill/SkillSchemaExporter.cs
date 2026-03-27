using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;

namespace PCGToolkit.Skill
{
    public static class SkillSchemaExporter
    {
        public static string ExportAll()
        {
            SkillRegistry.EnsureInitialized();

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"version\": \"1.0\",");
            sb.AppendLine($"  \"exportTime\": \"{System.DateTime.UtcNow:O}\",");
            sb.AppendLine("  \"skills\": [");

            bool first = true;
            foreach (var skill in SkillRegistry.GetAllSkills())
            {
                if (!first) sb.AppendLine(",");
                first = false;

                string schema = skill.GetJsonSchema();
                sb.Append("    ");
                sb.Append(schema);
            }

            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        public static string ExportSingle(string skillName)
        {
            var skill = SkillRegistry.GetSkill(skillName);
            if (skill == null)
            {
                Debug.LogWarning($"SkillSchemaExporter: Skill not found - {skillName}");
                return "{}";
            }
            return skill.GetJsonSchema();
        }

        /// <summary>
        /// 按类别导出 Skill JSON schema
        /// </summary>
        public static string ExportByCategory(string category)
        {
            SkillRegistry.EnsureInitialized();

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"category\": \"{category}\",");
            sb.AppendLine("  \"skills\": [");

            bool first = true;
            foreach (var skill in SkillRegistry.GetAllSkills())
            {
                var node = PCGNodeRegistry.GetNode(skill.Name);
                if (node == null || node.Category.ToString() != category) continue;

                if (!first) sb.AppendLine(",");
                first = false;
                sb.Append("    ");
                sb.Append(skill.GetJsonSchema());
            }

            sb.AppendLine();
            sb.AppendLine("  ]");
            sb.Append("}");
            return sb.ToString();
        }

        /// <summary>
        /// 按类别导出所有 Skill JSON schema 到目录
        /// </summary>
        public static void ExportAllByCategory(string outputDir)
        {
            var categories = System.Enum.GetNames(typeof(PCGNodeCategory));
            foreach (var cat in categories)
            {
                string json = ExportByCategory(cat);
                string filePath = System.IO.Path.Combine(outputDir, $"SKILL_{cat}_schema.json");
                System.IO.File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);
            }
            Debug.Log($"SkillSchemaExporter: Exported {categories.Length} category schemas to {outputDir}");

            if (outputDir.StartsWith("Assets/"))
                AssetDatabase.Refresh();
        }

        public static void ExportToFile(string filePath)
        {
            string json = ExportAll();

            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);

            System.IO.File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

            var skillCount = SkillRegistry.GetAllSkills().Count();
            Debug.Log($"SkillSchemaExporter: Exported {skillCount} skill schemas to {filePath}");

            if (filePath.StartsWith("Assets/"))
                AssetDatabase.Refresh();
        }
    }
}
