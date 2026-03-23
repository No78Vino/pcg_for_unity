using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
using PCGToolkit.Core;
using PCGToolkit.Graph;
using PCGToolkit.Skill;

namespace PCGToolkit.Communication
{
    public class AgentServer
    {
        public enum ProtocolType
        {
            Http,
            WebSocket,
            StdInOut
        }

        private ProtocolType protocol;
        private int port;
        private bool isRunning;
        private SkillExecutor skillExecutor;
        private AgentSession session;

        private HttpListener _listener;
        private Thread _listenThread;
        private ConcurrentQueue<HttpListenerContext> _pendingRequests
            = new ConcurrentQueue<HttpListenerContext>();

        // WebSocket state
        private Thread _wsListenThread;
        private System.Net.WebSockets.WebSocket _activeWebSocket;
        private ConcurrentQueue<string> _wsIncoming = new ConcurrentQueue<string>();

        public bool IsRunning => isRunning;

        public AgentServer(ProtocolType protocol = ProtocolType.Http, int port = 8765)
        {
            this.protocol = protocol;
            this.port = port;
            this.skillExecutor = new SkillExecutor();
            this.session = new AgentSession();
        }

        public void Start()
        {
            if (protocol == ProtocolType.StdInOut)
            {
                Debug.LogWarning("AgentServer: StdInOut protocol is not supported");
                return;
            }

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");

            try
            {
                _listener.Start();
            }
            catch (HttpListenerException e)
            {
                Debug.LogError($"AgentServer: Failed to start HTTP listener - {e.Message}");
                return;
            }

            isRunning = true;

            _listenThread = new Thread(ListenLoop) { IsBackground = true };
            _listenThread.Start();

            EditorApplication.update += PollAndProcessRequests;

            string protoName = protocol == ProtocolType.WebSocket ? "HTTP+WebSocket" : "HTTP";
            Debug.Log($"AgentServer: {protoName} server started, listening on http://localhost:{port}/");
        }

        private void ListenLoop()
        {
            while (isRunning && _listener != null && _listener.IsListening)
            {
                try
                {
                    var asyncResult = _listener.BeginGetContext(null, null);
                    if (asyncResult.AsyncWaitHandle.WaitOne(500))
                    {
                        var ctx = _listener.EndGetContext(asyncResult);

                        // WebSocket upgrade
                        if (protocol == ProtocolType.WebSocket && ctx.Request.IsWebSocketRequest)
                        {
                            HandleWebSocketUpgrade(ctx);
                            continue;
                        }

                        _pendingRequests.Enqueue(ctx);
                    }
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        private void HandleWebSocketUpgrade(HttpListenerContext httpCtx)
        {
            try
            {
                var wsCtx = httpCtx.AcceptWebSocketAsync("pcg-agent").Result;
                _activeWebSocket = wsCtx.WebSocket;

                _wsListenThread = new Thread(() => WebSocketReadLoop(_activeWebSocket)) { IsBackground = true };
                _wsListenThread.Start();

                Debug.Log("AgentServer: WebSocket client connected");
            }
            catch (Exception e)
            {
                Debug.LogError($"AgentServer: WebSocket upgrade failed - {e.Message}");
            }
        }

        private void WebSocketReadLoop(System.Net.WebSockets.WebSocket ws)
        {
            var buffer = new byte[8192];
            while (isRunning && ws.State == System.Net.WebSockets.WebSocketState.Open)
            {
                try
                {
                    var segment = new ArraySegment<byte>(buffer);
                    var result = ws.ReceiveAsync(segment, CancellationToken.None).Result;

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Close)
                    {
                        ws.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                            "Closing", CancellationToken.None).Wait();
                        break;
                    }

                    if (result.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        _wsIncoming.Enqueue(msg);
                    }
                }
                catch { break; }
            }
        }

        private void PollAndProcessRequests()
        {
            // Process HTTP requests
            int processed = 0;
            while (processed < 5 && _pendingRequests.TryDequeue(out var httpCtx))
            {
                try
                {
                    string body = ReadBody(httpCtx.Request);
                    string responseJson = HandleRequest(body);
                    WriteResponse(httpCtx, responseJson);
                }
                catch (Exception e)
                {
                    WriteResponse(httpCtx,
                        AgentProtocol.CreateErrorResponse($"Internal error: {e.Message}"));
                }
                processed++;
            }

            // Process WebSocket messages
            while (_wsIncoming.TryDequeue(out var wsMsg))
            {
                try
                {
                    string responseJson = HandleRequest(wsMsg);
                    SendWebSocketMessage(responseJson);
                }
                catch (Exception e)
                {
                    SendWebSocketMessage(
                        AgentProtocol.CreateErrorResponse($"Internal error: {e.Message}"));
                }
            }
        }

        private void SendWebSocketMessage(string json)
        {
            if (_activeWebSocket == null ||
                _activeWebSocket.State != System.Net.WebSockets.WebSocketState.Open)
                return;

            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                _activeWebSocket.SendAsync(new ArraySegment<byte>(bytes),
                    System.Net.WebSockets.WebSocketMessageType.Text, true,
                    CancellationToken.None).Wait();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AgentServer: WebSocket send failed - {e.Message}");
            }
        }

        private string ReadBody(HttpListenerRequest request)
        {
            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                return reader.ReadToEnd();
        }

        private void WriteResponse(HttpListenerContext ctx, string json)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(json);
                ctx.Response.ContentType = "application/json; charset=utf-8";
                ctx.Response.ContentLength64 = bytes.Length;
                ctx.Response.StatusCode = 200;
                ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
                ctx.Response.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"AgentServer: Failed to write response - {e.Message}");
            }
        }

        public void Stop()
        {
            isRunning = false;
            EditorApplication.update -= PollAndProcessRequests;

            try
            {
                if (_activeWebSocket != null &&
                    _activeWebSocket.State == System.Net.WebSockets.WebSocketState.Open)
                {
                    _activeWebSocket.CloseAsync(
                        System.Net.WebSockets.WebSocketCloseStatus.NormalClosure,
                        "Server stopping", CancellationToken.None).Wait(2000);
                }
            }
            catch { }

            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }

            _listenThread?.Join(2000);
            _wsListenThread?.Join(2000);
            Debug.Log("AgentServer: Stopped");
        }

        public string HandleRequest(string requestJson)
        {
            try
            {
                var request = AgentProtocol.ParseRequest(requestJson);
                return ProcessRequest(request);
            }
            catch (Exception e)
            {
                return AgentProtocol.CreateErrorResponse($"Request handling failed: {e.Message}");
            }
        }

        private string ProcessRequest(AgentProtocol.AgentRequest request)
        {
            switch (request.Action)
            {
                case "execute_skill":
                    return AgentProtocol.CreateSuccessResponse(
                        skillExecutor.ExecuteSkill(request.SkillName, request.Parameters),
                        request.RequestId);

                case "execute_pipeline":
                    return AgentProtocol.CreateSuccessResponse(
                        skillExecutor.ExecutePipeline(request.pipeline_skills, request.pipeline_params),
                        request.RequestId);

                case "list_skills":
                    return AgentProtocol.CreateSuccessResponse(
                        skillExecutor.ListSkills(), request.RequestId);

                case "get_schema":
                    return AgentProtocol.CreateSuccessResponse(
                        SkillSchemaExporter.ExportSingle(request.SkillName), request.RequestId);

                case "get_all_schemas":
                    return AgentProtocol.CreateSuccessResponse(
                        SkillSchemaExporter.ExportAll(), request.RequestId);

                case "list_nodes":
                    return HandleListNodes(request);

                case "create_graph":
                    return HandleCreateGraph(request);

                case "add_node":
                    return HandleAddNode(request);

                case "connect_nodes":
                    return HandleConnectNodes(request);

                case "set_param":
                    return HandleSetParam(request);

                case "save_graph":
                    return HandleSaveGraph(request);

                case "execute_graph":
                    return HandleExecuteGraph(request);

                case "get_graph_info":
                    return HandleGetGraphInfo(request);

                case "delete_node":
                    return HandleDeleteNode(request);

                case "disconnect_nodes":
                    return HandleDisconnectNodes(request);

                case "delete_graph":
                    return HandleDeleteGraph(request);

                case "list_graphs":
                    return HandleListGraphs(request);

                default:
                    return AgentProtocol.CreateErrorResponse($"Unknown action: {request.Action}", request.RequestId);
            }
        }

        // ---- Graph Operation Handlers ----

        private string HandleListNodes(AgentProtocol.AgentRequest request)
        {
            PCGNodeRegistry.EnsureInitialized();

            var categories = new Dictionary<string, List<string>>();
            foreach (var node in PCGNodeRegistry.GetAllNodes())
            {
                string cat = node.Category.ToString();
                if (!categories.ContainsKey(cat))
                    categories[cat] = new List<string>();

                var sb = new StringBuilder();
                sb.Append("{ ");
                sb.Append($"\"name\": \"{JsonHelper.Esc(node.Name)}\", ");
                sb.Append($"\"description\": \"{JsonHelper.Esc(node.Description)}\", ");

                // inputs
                sb.Append("\"inputs\": [");
                if (node.Inputs != null)
                {
                    for (int i = 0; i < node.Inputs.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var inp = node.Inputs[i];
                        sb.Append($"{{ \"name\": \"{JsonHelper.Esc(inp.Name)}\", \"type\": \"{inp.PortType}\" }}");
                    }
                }
                sb.Append("], ");

                // outputs
                sb.Append("\"outputs\": [");
                if (node.Outputs != null)
                {
                    for (int i = 0; i < node.Outputs.Length; i++)
                    {
                        if (i > 0) sb.Append(", ");
                        var outp = node.Outputs[i];
                        sb.Append($"{{ \"name\": \"{JsonHelper.Esc(outp.Name)}\", \"type\": \"{outp.PortType}\" }}");
                    }
                }
                sb.Append("] }");

                categories[cat].Add(sb.ToString());
            }

            var result = new StringBuilder();
            result.Append("{ \"categories\": { ");
            bool firstCat = true;
            foreach (var kvp in categories)
            {
                if (!firstCat) result.Append(", ");
                firstCat = false;
                result.Append($"\"{kvp.Key}\": [{string.Join(", ", kvp.Value)}]");
            }
            result.Append(" } }");

            return AgentProtocol.CreateSuccessResponse(result.ToString(), request.RequestId);
        }

        private string HandleCreateGraph(AgentProtocol.AgentRequest request)
        {
            string graphName = request.GraphName;
            if (string.IsNullOrEmpty(graphName) && !string.IsNullOrEmpty(request.Parameters))
            {
                var p = JsonHelper.ParseSimpleJson(request.Parameters);
                if (p.ContainsKey("graph_name"))
                    graphName = p["graph_name"].ToString();
            }

            string graphId = session.CreateGraph(graphName);

            return AgentProtocol.CreateSuccessResponse(
                $"{{ \"graph_id\": \"{graphId}\", \"graph_name\": \"{JsonHelper.Esc(graphName ?? "Untitled")}\" }}",
                request.RequestId);
        }

        private string HandleAddNode(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            string nodeType = request.NodeType;
            if (string.IsNullOrEmpty(nodeType))
                return AgentProtocol.CreateErrorResponse(
                    "Missing node_type", request.RequestId);

            PCGNodeRegistry.EnsureInitialized();
            var template = PCGNodeRegistry.GetNode(nodeType);
            if (template == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Unknown node type: {nodeType}", request.RequestId);

            var position = new Vector2(request.position_x, request.position_y);
            var nodeData = graph.AddNode(nodeType, position);

            return AgentProtocol.CreateSuccessResponse(
                $"{{ \"node_id\": \"{nodeData.NodeId}\", \"node_type\": \"{JsonHelper.Esc(nodeType)}\" }}",
                request.RequestId);
        }

        private string HandleConnectNodes(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            if (string.IsNullOrEmpty(request.OutputNodeId) ||
                string.IsNullOrEmpty(request.InputNodeId))
                return AgentProtocol.CreateErrorResponse(
                    "Missing output_node_id or input_node_id", request.RequestId);

            string outPort = string.IsNullOrEmpty(request.OutputPort) ? "geometry" : request.OutputPort;
            string inPort = string.IsNullOrEmpty(request.InputPort) ? "input" : request.InputPort;

            // Port existence validation
            var outNodeData = graph.Nodes.Find(n => n.NodeId == request.OutputNodeId);
            var inNodeData = graph.Nodes.Find(n => n.NodeId == request.InputNodeId);

            if (outNodeData == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Output node not found: {request.OutputNodeId}", request.RequestId);
            if (inNodeData == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Input node not found: {request.InputNodeId}", request.RequestId);

            PCGNodeRegistry.EnsureInitialized();
            var outTemplate = PCGNodeRegistry.GetNode(outNodeData.NodeType);
            var inTemplate = PCGNodeRegistry.GetNode(inNodeData.NodeType);

            if (outTemplate != null && outTemplate.Outputs != null)
            {
                bool hasOutPort = false;
                foreach (var o in outTemplate.Outputs)
                {
                    if (o.Name == outPort) { hasOutPort = true; break; }
                }
                if (!hasOutPort)
                    return AgentProtocol.CreateErrorResponse(
                        $"Port '{outPort}' not found on output node type '{outNodeData.NodeType}'", request.RequestId);
            }

            if (inTemplate != null && inTemplate.Inputs != null)
            {
                bool hasInPort = false;
                foreach (var inp in inTemplate.Inputs)
                {
                    if (inp.Name == inPort) { hasInPort = true; break; }
                }
                if (!hasInPort)
                    return AgentProtocol.CreateErrorResponse(
                        $"Port '{inPort}' not found on input node type '{inNodeData.NodeType}'", request.RequestId);
            }

            graph.AddEdge(request.OutputNodeId, outPort, request.InputNodeId, inPort);

            return AgentProtocol.CreateSuccessResponse(
                "{ \"edge_created\": true }", request.RequestId);
        }

        private string HandleSetParam(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            string nodeId = request.NodeId;
            if (string.IsNullOrEmpty(nodeId))
                return AgentProtocol.CreateErrorResponse(
                    "Missing node_id", request.RequestId);

            var nodeData = graph.Nodes.Find(n => n.NodeId == nodeId);
            if (nodeData == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Node not found: {nodeId}", request.RequestId);

            if (string.IsNullOrEmpty(request.Parameters))
                return AgentProtocol.CreateErrorResponse(
                    "Missing parameters", request.RequestId);

            var paramDict = JsonHelper.ParseSimpleJson(request.Parameters);
            int setCount = 0;
            foreach (var kvp in paramDict)
            {
                nodeData.SetParameter(kvp.Key, kvp.Value);
                setCount++;
            }

            return AgentProtocol.CreateSuccessResponse(
                $"{{ \"params_set\": {setCount} }}", request.RequestId);
        }

        private string HandleSaveGraph(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            string assetPath = request.AssetPath;
            if (string.IsNullOrEmpty(assetPath))
                return AgentProtocol.CreateErrorResponse(
                    "Missing asset_path", request.RequestId);

            if (!assetPath.EndsWith(".asset"))
                assetPath += ".asset";

            string directory = Path.GetDirectoryName(assetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            try
            {
                PCGGraphSerializer.SaveAsAsset(graph, assetPath);
                return AgentProtocol.CreateSuccessResponse(
                    $"{{ \"asset_path\": \"{JsonHelper.Esc(assetPath)}\" }}", request.RequestId);
            }
            catch (Exception e)
            {
                return AgentProtocol.CreateErrorResponse(
                    $"Failed to save graph: {e.Message}", request.RequestId);
            }
        }

        private string HandleExecuteGraph(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            try
            {
                var executor = new PCGGraphExecutor(graph);
                executor.Execute(continueOnError: true);

                var sb = new StringBuilder();
                sb.Append("{ ");
                sb.Append($"\"nodes_executed\": {graph.Nodes.Count}, ");
                sb.Append("\"outputs\": { ");

                bool first = true;
                foreach (var nodeData in graph.Nodes)
                {
                    var outputs = executor.GetNodeAllOutputs(nodeData.NodeId);
                    if (outputs == null) continue;

                    foreach (var kvp in outputs)
                    {
                        if (!first) sb.Append(", ");
                        first = false;

                        var geo = kvp.Value;
                        sb.Append($"\"{JsonHelper.Esc(nodeData.NodeId)}_{JsonHelper.Esc(kvp.Key)}\": {{ ");
                        sb.Append($"\"node_type\": \"{JsonHelper.Esc(nodeData.NodeType)}\", ");
                        sb.Append($"\"pointCount\": {geo?.Points.Count ?? 0}, ");
                        sb.Append($"\"primCount\": {geo?.Primitives.Count ?? 0}");
                        sb.Append(" }");
                    }
                }

                sb.Append(" } }");

                return AgentProtocol.CreateSuccessResponse(sb.ToString(), request.RequestId);
            }
            catch (Exception e)
            {
                return AgentProtocol.CreateErrorResponse(
                    $"Graph execution failed: {e.Message}", request.RequestId);
            }
        }

        private string HandleGetGraphInfo(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            var sb = new StringBuilder();
            sb.Append("{ ");
            sb.Append($"\"graph_name\": \"{JsonHelper.Esc(graph.GraphName)}\", ");

            // nodes
            sb.Append("\"nodes\": [");
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var n = graph.Nodes[i];
                sb.Append($"{{ \"id\": \"{n.NodeId}\", \"type\": \"{JsonHelper.Esc(n.NodeType)}\", ");
                sb.Append($"\"position\": [{JsonHelper.F(n.Position.x)}, {JsonHelper.F(n.Position.y)}], ");
                sb.Append("\"parameters\": { ");
                for (int p = 0; p < n.Parameters.Count; p++)
                {
                    if (p > 0) sb.Append(", ");
                    sb.Append($"\"{JsonHelper.Esc(n.Parameters[p].Key)}\": \"{JsonHelper.Esc(n.Parameters[p].ValueJson)}\"");
                }
                sb.Append(" } }");
            }
            sb.Append("], ");

            // edges
            sb.Append("\"edges\": [");
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var e = graph.Edges[i];
                sb.Append($"{{ \"output_node\": \"{e.OutputNodeId}\", \"output_port\": \"{JsonHelper.Esc(e.OutputPort)}\", ");
                sb.Append($"\"input_node\": \"{e.InputNodeId}\", \"input_port\": \"{JsonHelper.Esc(e.InputPort)}\" }}");
            }
            sb.Append("] }");

            return AgentProtocol.CreateSuccessResponse(sb.ToString(), request.RequestId);
        }

        private string HandleDeleteNode(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            string nodeId = request.NodeId;
            if (string.IsNullOrEmpty(nodeId))
                return AgentProtocol.CreateErrorResponse(
                    "Missing node_id", request.RequestId);

            var nodeData = graph.Nodes.Find(n => n.NodeId == nodeId);
            if (nodeData == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Node not found: {nodeId}", request.RequestId);

            int edgesBefore = graph.Edges.Count;
            graph.RemoveNode(nodeId);
            int edgesRemoved = edgesBefore - graph.Edges.Count;

            return AgentProtocol.CreateSuccessResponse(
                $"{{ \"deleted\": true, \"edges_removed\": {edgesRemoved} }}", request.RequestId);
        }

        private string HandleDisconnectNodes(AgentProtocol.AgentRequest request)
        {
            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            if (string.IsNullOrEmpty(request.OutputNodeId) ||
                string.IsNullOrEmpty(request.InputNodeId))
                return AgentProtocol.CreateErrorResponse(
                    "Missing output_node_id or input_node_id", request.RequestId);

            string outPort = string.IsNullOrEmpty(request.OutputPort) ? "geometry" : request.OutputPort;
            string inPort = string.IsNullOrEmpty(request.InputPort) ? "input" : request.InputPort;

            int removed = graph.Edges.RemoveAll(e =>
                e.OutputNodeId == request.OutputNodeId &&
                e.OutputPort == outPort &&
                e.InputNodeId == request.InputNodeId &&
                e.InputPort == inPort);

            if (removed == 0)
                return AgentProtocol.CreateErrorResponse(
                    $"Edge not found: {request.OutputNodeId}:{outPort} -> {request.InputNodeId}:{inPort}",
                    request.RequestId);

            return AgentProtocol.CreateSuccessResponse(
                "{ \"disconnected\": true }", request.RequestId);
        }

        private string HandleDeleteGraph(AgentProtocol.AgentRequest request)
        {
            if (string.IsNullOrEmpty(request.GraphId))
                return AgentProtocol.CreateErrorResponse(
                    "Missing graph_id", request.RequestId);

            var graph = session.GetGraph(request.GraphId);
            if (graph == null)
                return AgentProtocol.CreateErrorResponse(
                    $"Graph not found: {request.GraphId}", request.RequestId);

            session.RemoveGraph(request.GraphId);

            return AgentProtocol.CreateSuccessResponse(
                "{ \"deleted\": true }", request.RequestId);
        }

        private string HandleListGraphs(AgentProtocol.AgentRequest request)
        {
            var summaries = session.ListGraphSummaries();

            var sb = new StringBuilder();
            sb.Append("{ \"graphs\": [");
            for (int i = 0; i < summaries.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                var s = summaries[i];
                sb.Append($"{{ \"graph_id\": \"{JsonHelper.Esc(s.id)}\", ");
                sb.Append($"\"graph_name\": \"{JsonHelper.Esc(s.name)}\", ");
                sb.Append($"\"node_count\": {s.nodeCount} }}");
            }
            sb.Append("] }");

            return AgentProtocol.CreateSuccessResponse(sb.ToString(), request.RequestId);
        }
    }
}
