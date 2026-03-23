using NUnit.Framework;
using PCGToolkit.Communication;
using PCGToolkit.Skill;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class AgentIntegrationTests
    {
        [Test]
        public void HandleRequest_ListSkills_ReturnsNonEmpty()
        {
            var server = new AgentServer(AgentServer.ProtocolType.Http, 0);
            string requestJson = "{ \"action\": \"list_skills\", \"request_id\": \"test_001\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(responseJson.Contains("\"Success\":true") || responseJson.Contains("\"Success\": true"),
                $"Expected success response, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_GetSchema_Box_ReturnsSchema()
        {
            var server = new AgentServer(AgentServer.ProtocolType.Http, 0);
            string requestJson = "{ \"action\": \"get_schema\", \"skill_name\": \"Box\", \"request_id\": \"test_002\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(responseJson.Contains("\"Success\":true") || responseJson.Contains("\"Success\": true"),
                $"Expected success, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_ExecuteSkill_Box_ReturnsGeometry()
        {
            var server = new AgentServer(AgentServer.ProtocolType.Http, 0);
            string requestJson = "{ \"action\": \"execute_skill\", \"skill_name\": \"Box\", \"parameters\": \"{ \\\"sizeX\\\": 2, \\\"sizeY\\\": 1, \\\"sizeZ\\\": 3 }\", \"request_id\": \"test_003\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(responseJson.Contains("\"Success\":true") || responseJson.Contains("\"Success\": true"),
                $"Expected success, got: {responseJson}");
            Assert.IsTrue(responseJson.Contains("pointCount"),
                $"Response should contain geometry summary, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_UnknownAction_ReturnsError()
        {
            var server = new AgentServer(AgentServer.ProtocolType.Http, 0);
            string requestJson = "{ \"action\": \"unknown_action\", \"request_id\": \"test_err\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(responseJson.Contains("\"Success\":false") || responseJson.Contains("\"Success\": false"));
            Assert.IsTrue(responseJson.Contains("Unknown action"));
        }

        [Test]
        public void ParseRequest_EmptyJson_ThrowsException()
        {
            Assert.Throws<System.ArgumentException>(() => AgentProtocol.ParseRequest(""));
        }

        [Test]
        public void SkillSchemaExporter_ExportAll_ReturnsValidJson()
        {
            string json = SkillSchemaExporter.ExportAll();
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("\"skills\""));
            Assert.IsTrue(json.Contains("\"version\""));
        }

        [Test]
        public void PCGNodeSkillAdapter_GetJsonSchema_ContainsFields()
        {
            SkillRegistry.EnsureInitialized();
            var skill = SkillRegistry.GetSkill("Box");
            Assert.IsNotNull(skill, "Box skill should be registered");

            string schema = skill.GetJsonSchema();
            Assert.IsTrue(schema.Contains("\"name\""), $"Schema should contain name field: {schema}");
            Assert.IsTrue(schema.Contains("\"parameters\""), $"Schema should contain parameters field: {schema}");
        }
    }
}
