using NUnit.Framework;
using PCGToolkit.Communication;
using PCGToolkit.Skill;

namespace PCGToolkit.Tests
{
    [TestFixture]
    public class AgentIntegrationTests
    {
        private AgentServer CreateServer() => new AgentServer(AgentServer.ProtocolType.Http, 0);

        private bool IsSuccess(string json) =>
            json.Contains("\"Success\":true") || json.Contains("\"Success\": true");

        private bool IsFailure(string json) =>
            json.Contains("\"Success\":false") || json.Contains("\"Success\": false");

        // ---- Existing Tests ----

        [Test]
        public void HandleRequest_ListSkills_ReturnsNonEmpty()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"list_skills\", \"request_id\": \"test_001\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson),
                $"Expected success response, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_GetSchema_Box_ReturnsSchema()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"get_schema\", \"skill_name\": \"Box\", \"request_id\": \"test_002\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson),
                $"Expected success, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_ExecuteSkill_Box_ReturnsGeometry()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"execute_skill\", \"skill_name\": \"Box\", \"parameters\": \"{ \\\"sizeX\\\": 2, \\\"sizeY\\\": 1, \\\"sizeZ\\\": 3 }\", \"request_id\": \"test_003\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson),
                $"Expected success, got: {responseJson}");
            Assert.IsTrue(responseJson.Contains("pointCount"),
                $"Response should contain geometry summary, got: {responseJson}");
        }

        [Test]
        public void HandleRequest_UnknownAction_ReturnsError()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"unknown_action\", \"request_id\": \"test_err\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsFailure(responseJson));
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

        // ---- New: get_all_schemas ----

        [Test]
        public void HandleRequest_GetAllSchemas_ReturnsMultipleSkills()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"get_all_schemas\", \"request_id\": \"test_all\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson));
            Assert.IsTrue(responseJson.Contains("skills"));
        }

        // ---- New: list_nodes ----

        [Test]
        public void HandleRequest_ListNodes_ReturnsCategorizedNodes()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"list_nodes\", \"request_id\": \"test_ln\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson));
            Assert.IsTrue(responseJson.Contains("categories"));
            Assert.IsTrue(responseJson.Contains("Create"));
        }

        // ---- New: create_graph ----

        [Test]
        public void HandleRequest_CreateGraph_ReturnsGraphId()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\", \"request_id\": \"test_cg\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson),
                $"create_graph should succeed, got: {responseJson}");
            Assert.IsTrue(responseJson.Contains("graph_id"),
                $"Response should contain graph_id, got: {responseJson}");
        }

        // ---- New: add_node ----

        [Test]
        public void HandleRequest_AddNode_ReturnsNodeId()
        {
            var server = CreateServer();

            // First create a graph
            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            Assert.IsTrue(IsSuccess(createResp));
            string graphId = ExtractField(createResp, "graph_id");
            Assert.IsNotEmpty(graphId, "Should have graph_id");

            // Add a node
            string addResp = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\", \"position_x\": 100, \"position_y\": 200 }}");

            Assert.IsTrue(IsSuccess(addResp),
                $"add_node should succeed, got: {addResp}");
            Assert.IsTrue(addResp.Contains("node_id"),
                $"Response should contain node_id, got: {addResp}");
        }

        // ---- New: add_node with unknown type ----

        [Test]
        public void HandleRequest_AddNode_UnknownType_ReturnsError()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addResp = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"NonExistentNode\" }}");

            Assert.IsTrue(IsFailure(addResp), "Adding unknown node type should fail");
        }

        // ---- New: connect_nodes ----

        [Test]
        public void HandleRequest_ConnectNodes_ReturnsSuccess()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addNode1 = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            string nodeId1 = ExtractField(addNode1, "node_id");

            string addNode2 = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            string nodeId2 = ExtractField(addNode2, "node_id");

            string connectResp = server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{nodeId1}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{nodeId2}\", \"input_port\": \"input\" }}");

            Assert.IsTrue(IsSuccess(connectResp),
                $"connect_nodes should succeed, got: {connectResp}");
            Assert.IsTrue(connectResp.Contains("edge_created"));
        }

        // ---- New: set_param ----

        [Test]
        public void HandleRequest_SetParam_ReturnsParamCount()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addResp = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            string nodeId = ExtractField(addResp, "node_id");

            string setResp = server.HandleRequest(
                $"{{ \"action\": \"set_param\", \"graph_id\": \"{graphId}\", " +
                $"\"node_id\": \"{nodeId}\", \"parameters\": \"{{ \\\"rows\\\": 10, \\\"columns\\\": 10 }}\" }}");

            Assert.IsTrue(IsSuccess(setResp),
                $"set_param should succeed, got: {setResp}");
            Assert.IsTrue(setResp.Contains("params_set"),
                $"Response should contain params_set, got: {setResp}");
        }

        // ---- New: execute_graph ----

        [Test]
        public void HandleRequest_ExecuteGraph_ReturnsOutputs()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            string graphId = ExtractField(createResp, "graph_id");

            server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Box\" }}");

            string execResp = server.HandleRequest(
                $"{{ \"action\": \"execute_graph\", \"graph_id\": \"{graphId}\" }}");

            Assert.IsTrue(IsSuccess(execResp),
                $"execute_graph should succeed, got: {execResp}");
            Assert.IsTrue(execResp.Contains("nodes_executed"),
                $"Response should contain nodes_executed, got: {execResp}");
        }

        // ---- New: get_graph_info ----

        [Test]
        public void HandleRequest_GetGraphInfo_ReturnsStructure()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"InfoTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");

            Assert.IsTrue(IsSuccess(infoResp),
                $"get_graph_info should succeed, got: {infoResp}");
            Assert.IsTrue(infoResp.Contains("graph_name"));
            Assert.IsTrue(infoResp.Contains("nodes"));
            Assert.IsTrue(infoResp.Contains("edges"));
        }

        // ---- New: get_graph_info for non-existent graph ----

        [Test]
        public void HandleRequest_GetGraphInfo_NonExistent_ReturnsError()
        {
            var server = CreateServer();
            string infoResp = server.HandleRequest(
                "{ \"action\": \"get_graph_info\", \"graph_id\": \"non_existent\" }");

            Assert.IsTrue(IsFailure(infoResp), "Non-existent graph should return error");
        }

        // ---- New: End-to-End flow ----

        [Test]
        public void EndToEnd_CreateBuildExecuteGraph()
        {
            var server = CreateServer();

            // 1. Create graph
            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"E2E_Test\" }");
            Assert.IsTrue(IsSuccess(createResp));
            string graphId = ExtractField(createResp, "graph_id");

            // 2. Add Grid node
            string addGrid = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            Assert.IsTrue(IsSuccess(addGrid));
            string gridNodeId = ExtractField(addGrid, "node_id");

            // 3. Add Subdivide node
            string addSubdiv = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            Assert.IsTrue(IsSuccess(addSubdiv));
            string subdivNodeId = ExtractField(addSubdiv, "node_id");

            // 4. Connect Grid -> Subdivide
            string connectResp = server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridNodeId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivNodeId}\", \"input_port\": \"input\" }}");
            Assert.IsTrue(IsSuccess(connectResp));

            // 5. Execute graph
            string execResp = server.HandleRequest(
                $"{{ \"action\": \"execute_graph\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(execResp),
                $"Graph execution should succeed, got: {execResp}");
            Assert.IsTrue(execResp.Contains("nodes_executed"));

            // 6. Verify graph info
            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(infoResp));
            Assert.IsTrue(infoResp.Contains("Grid"));
            Assert.IsTrue(infoResp.Contains("Subdivide"));
        }

        // ---- Helper ----

        private static string ExtractField(string json, string fieldName)
        {
            string pattern = $"\"{fieldName}\": \"";
            int idx = json.IndexOf(pattern);
            if (idx < 0)
            {
                pattern = $"\"{fieldName}\":\"";
                idx = json.IndexOf(pattern);
            }
            if (idx < 0) return "";

            int start = idx + pattern.Length;
            int end = json.IndexOf("\"", start);
            if (end < 0) return "";

            return json.Substring(start, end - start);
        }
    }
}
