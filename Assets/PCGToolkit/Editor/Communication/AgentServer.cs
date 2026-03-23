using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;
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

        private HttpListener _listener;
        private Thread _listenThread;
        private ConcurrentQueue<HttpListenerContext> _pendingRequests
            = new ConcurrentQueue<HttpListenerContext>();

        public bool IsRunning => isRunning;

        public AgentServer(ProtocolType protocol = ProtocolType.Http, int port = 8765)
        {
            this.protocol = protocol;
            this.port = port;
            this.skillExecutor = new SkillExecutor();
        }

        public void Start()
        {
            if (protocol != ProtocolType.Http)
            {
                Debug.LogWarning($"AgentServer: Only HTTP protocol is currently supported, got {protocol}");
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

            Debug.Log($"AgentServer: HTTP server started, listening on http://localhost:{port}/");
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
                        _pendingRequests.Enqueue(ctx);
                    }
                }
                catch (HttpListenerException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        private void PollAndProcessRequests()
        {
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

            try { _listener?.Stop(); } catch { }
            try { _listener?.Close(); } catch { }

            _listenThread?.Join(2000);
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

                default:
                    return AgentProtocol.CreateErrorResponse($"Unknown action: {request.Action}", request.RequestId);
            }
        }
    }
}
