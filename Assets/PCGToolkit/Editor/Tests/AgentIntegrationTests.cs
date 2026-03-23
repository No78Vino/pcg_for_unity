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

        // ---- Skill Tests ----

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

        // ---- get_all_schemas ----

        [Test]
        public void HandleRequest_GetAllSchemas_ReturnsMultipleSkills()
        {
            var server = CreateServer();
            string requestJson = "{ \"action\": \"get_all_schemas\", \"request_id\": \"test_all\" }";

            string responseJson = server.HandleRequest(requestJson);

            Assert.IsTrue(IsSuccess(responseJson));
            Assert.IsTrue(responseJson.Contains("skills"));
        }

        // ---- list_nodes ----

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

        // ---- create_graph ----

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

        // ---- add_node ----

        [Test]
        public void HandleRequest_AddNode_ReturnsNodeId()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"TestGraph\" }");
            Assert.IsTrue(IsSuccess(createResp));
            string graphId = ExtractField(createResp, "graph_id");
            Assert.IsNotEmpty(graphId, "Should have graph_id");

            string addResp = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\", \"position_x\": 100, \"position_y\": 200 }}");

            Assert.IsTrue(IsSuccess(addResp),
                $"add_node should succeed, got: {addResp}");
            Assert.IsTrue(addResp.Contains("node_id"),
                $"Response should contain node_id, got: {addResp}");
        }

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

        // ---- connect_nodes ----

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

        // ---- connect_nodes port validation ----

        [Test]
        public void HandleRequest_ConnectNodes_InvalidPort_ReturnsError()
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
                $"\"output_node_id\": \"{nodeId1}\", \"output_port\": \"nonexistent_port\", " +
                $"\"input_node_id\": \"{nodeId2}\", \"input_port\": \"input\" }}");

            Assert.IsTrue(IsFailure(connectResp),
                $"Connecting invalid port should fail, got: {connectResp}");
            Assert.IsTrue(connectResp.Contains("nonexistent_port"),
                $"Error should mention port name, got: {connectResp}");
        }

        // ---- set_param ----

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

        // ---- execute_graph ----

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

        // ---- get_graph_info ----

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

        [Test]
        public void HandleRequest_GetGraphInfo_NonExistent_ReturnsError()
        {
            var server = CreateServer();
            string infoResp = server.HandleRequest(
                "{ \"action\": \"get_graph_info\", \"graph_id\": \"non_existent\" }");

            Assert.IsTrue(IsFailure(infoResp), "Non-existent graph should return error");
        }

        // ---- delete_node ----

        [Test]
        public void HandleRequest_DeleteNode_Success()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"DeleteTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addResp = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            string nodeId = ExtractField(addResp, "node_id");

            string deleteResp = server.HandleRequest(
                $"{{ \"action\": \"delete_node\", \"graph_id\": \"{graphId}\", \"node_id\": \"{nodeId}\" }}");

            Assert.IsTrue(IsSuccess(deleteResp),
                $"delete_node should succeed, got: {deleteResp}");
            Assert.IsTrue(deleteResp.Contains("\"deleted\": true") || deleteResp.Contains("\"deleted\":true"),
                $"Response should confirm deletion, got: {deleteResp}");

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsFalse(infoResp.Contains(nodeId),
                "Deleted node should not appear in graph info");
        }

        [Test]
        public void HandleRequest_DeleteNode_NotFound_ReturnsError()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"DeleteTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string deleteResp = server.HandleRequest(
                $"{{ \"action\": \"delete_node\", \"graph_id\": \"{graphId}\", \"node_id\": \"nonexistent\" }}");

            Assert.IsTrue(IsFailure(deleteResp),
                "Deleting non-existent node should fail");
        }

        [Test]
        public void HandleRequest_DeleteNode_CleansEdges()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"EdgeCleanTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addGrid = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            string gridId = ExtractField(addGrid, "node_id");

            string addSubdiv = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            string subdivId = ExtractField(addSubdiv, "node_id");

            server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivId}\", \"input_port\": \"input\" }}");

            string deleteResp = server.HandleRequest(
                $"{{ \"action\": \"delete_node\", \"graph_id\": \"{graphId}\", \"node_id\": \"{gridId}\" }}");

            Assert.IsTrue(IsSuccess(deleteResp));
            Assert.IsTrue(deleteResp.Contains("edges_removed"),
                $"Response should show edges_removed, got: {deleteResp}");

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsFalse(infoResp.Contains(gridId),
                "Deleted node should not appear in edges");
        }

        // ---- disconnect_nodes ----

        [Test]
        public void HandleRequest_DisconnectNodes_Success()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"DisconnectTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string addGrid = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            string gridId = ExtractField(addGrid, "node_id");

            string addSubdiv = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            string subdivId = ExtractField(addSubdiv, "node_id");

            server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivId}\", \"input_port\": \"input\" }}");

            string disconnectResp = server.HandleRequest(
                $"{{ \"action\": \"disconnect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivId}\", \"input_port\": \"input\" }}");

            Assert.IsTrue(IsSuccess(disconnectResp),
                $"disconnect_nodes should succeed, got: {disconnectResp}");

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(infoResp.Contains("\"edges\": []") || infoResp.Contains("\"edges\":[]"),
                $"Edges should be empty after disconnect, got: {infoResp}");
        }

        [Test]
        public void HandleRequest_DisconnectNodes_NotFound_ReturnsError()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"DisconnectTest\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string disconnectResp = server.HandleRequest(
                $"{{ \"action\": \"disconnect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"fake1\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"fake2\", \"input_port\": \"input\" }}");

            Assert.IsTrue(IsFailure(disconnectResp),
                "Disconnecting non-existent edge should fail");
        }

        // ---- delete_graph ----

        [Test]
        public void HandleRequest_DeleteGraph_Success()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"ToDelete\" }");
            string graphId = ExtractField(createResp, "graph_id");

            string deleteResp = server.HandleRequest(
                $"{{ \"action\": \"delete_graph\", \"graph_id\": \"{graphId}\" }}");

            Assert.IsTrue(IsSuccess(deleteResp),
                $"delete_graph should succeed, got: {deleteResp}");

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsFailure(infoResp),
                "Accessing deleted graph should fail");
        }

        [Test]
        public void HandleRequest_DeleteGraph_NotFound_ReturnsError()
        {
            var server = CreateServer();

            string deleteResp = server.HandleRequest(
                "{ \"action\": \"delete_graph\", \"graph_id\": \"nonexistent\" }");

            Assert.IsTrue(IsFailure(deleteResp),
                "Deleting non-existent graph should fail");
        }

        // ---- list_graphs ----

        [Test]
        public void HandleRequest_ListGraphs_ReturnsCorrectCount()
        {
            var server = CreateServer();

            server.HandleRequest("{ \"action\": \"create_graph\", \"graph_name\": \"Graph1\" }");
            string createResp2 = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"Graph2\" }");
            string graphId2 = ExtractField(createResp2, "graph_id");

            string listResp = server.HandleRequest("{ \"action\": \"list_graphs\" }");

            Assert.IsTrue(IsSuccess(listResp),
                $"list_graphs should succeed, got: {listResp}");
            Assert.IsTrue(listResp.Contains("Graph1"));
            Assert.IsTrue(listResp.Contains("Graph2"));

            server.HandleRequest(
                $"{{ \"action\": \"delete_graph\", \"graph_id\": \"{graphId2}\" }}");

            string listResp2 = server.HandleRequest("{ \"action\": \"list_graphs\" }");
            Assert.IsTrue(IsSuccess(listResp2));
            Assert.IsTrue(listResp2.Contains("Graph1"));
            Assert.IsFalse(listResp2.Contains("Graph2"),
                $"Deleted graph should not appear in list, got: {listResp2}");
        }

        // ---- End-to-End flows ----

        [Test]
        public void EndToEnd_CreateBuildExecuteGraph()
        {
            var server = CreateServer();

            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"E2E_Test\" }");
            Assert.IsTrue(IsSuccess(createResp));
            string graphId = ExtractField(createResp, "graph_id");

            string addGrid = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            Assert.IsTrue(IsSuccess(addGrid));
            string gridNodeId = ExtractField(addGrid, "node_id");

            string addSubdiv = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            Assert.IsTrue(IsSuccess(addSubdiv));
            string subdivNodeId = ExtractField(addSubdiv, "node_id");

            string connectResp = server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridNodeId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivNodeId}\", \"input_port\": \"input\" }}");
            Assert.IsTrue(IsSuccess(connectResp));

            string execResp = server.HandleRequest(
                $"{{ \"action\": \"execute_graph\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(execResp),
                $"Graph execution should succeed, got: {execResp}");
            Assert.IsTrue(execResp.Contains("nodes_executed"));

            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(infoResp));
            Assert.IsTrue(infoResp.Contains("Grid"));
            Assert.IsTrue(infoResp.Contains("Subdivide"));
        }

        [Test]
        public void EndToEnd_GraphBuildWithErrorRecovery()
        {
            var server = CreateServer();

            // 1. Create graph
            string createResp = server.HandleRequest(
                "{ \"action\": \"create_graph\", \"graph_name\": \"ErrorRecovery\" }");
            Assert.IsTrue(IsSuccess(createResp));
            string graphId = ExtractField(createResp, "graph_id");

            // 2. Add Grid
            string addGrid = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Grid\" }}");
            Assert.IsTrue(IsSuccess(addGrid));
            string gridId = ExtractField(addGrid, "node_id");

            // 3. Accidentally add Box (wrong node)
            string addBox = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Box\" }}");
            Assert.IsTrue(IsSuccess(addBox));
            string boxId = ExtractField(addBox, "node_id");

            // 4. Delete the wrong Box
            string deleteBox = server.HandleRequest(
                $"{{ \"action\": \"delete_node\", \"graph_id\": \"{graphId}\", \"node_id\": \"{boxId}\" }}");
            Assert.IsTrue(IsSuccess(deleteBox));

            // 5. Add the correct Subdivide
            string addSubdiv = server.HandleRequest(
                $"{{ \"action\": \"add_node\", \"graph_id\": \"{graphId}\", \"node_type\": \"Subdivide\" }}");
            Assert.IsTrue(IsSuccess(addSubdiv));
            string subdivId = ExtractField(addSubdiv, "node_id");

            // 6. Connect Grid -> Subdivide
            string connectResp = server.HandleRequest(
                $"{{ \"action\": \"connect_nodes\", \"graph_id\": \"{graphId}\", " +
                $"\"output_node_id\": \"{gridId}\", \"output_port\": \"geometry\", " +
                $"\"input_node_id\": \"{subdivId}\", \"input_port\": \"input\" }}");
            Assert.IsTrue(IsSuccess(connectResp));

            // 7. Set params
            string setResp = server.HandleRequest(
                $"{{ \"action\": \"set_param\", \"graph_id\": \"{graphId}\", " +
                $"\"node_id\": \"{gridId}\", \"parameters\": \"{{ \\\"rows\\\": 10, \\\"columns\\\": 10 }}\" }}");
            Assert.IsTrue(IsSuccess(setResp));

            // 8. Execute
            string execResp = server.HandleRequest(
                $"{{ \"action\": \"execute_graph\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(execResp),
                $"Graph execution should succeed, got: {execResp}");

            // 9. Verify graph info only has Grid + Subdivide
            string infoResp = server.HandleRequest(
                $"{{ \"action\": \"get_graph_info\", \"graph_id\": \"{graphId}\" }}");
            Assert.IsTrue(IsSuccess(infoResp));
            Assert.IsTrue(infoResp.Contains("Grid"));
            Assert.IsTrue(infoResp.Contains("Subdivide"));
            Assert.IsFalse(infoResp.Contains(boxId),
                "Deleted Box should not appear in graph info");
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
