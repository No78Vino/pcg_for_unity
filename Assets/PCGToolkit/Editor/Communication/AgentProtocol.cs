using System;
using UnityEngine;

namespace PCGToolkit.Communication
{
    public static class AgentProtocol
    {
        [Serializable]
        public class AgentRequest
        {
            public string action;
            public string skill_name;
            public string parameters;
            public string request_id;

            // pipeline fields
            public string[] pipeline_skills;
            public string[] pipeline_params;

            // graph operation fields
            public string graph_id;
            public string node_type;
            public string node_id;
            public string output_node_id;
            public string output_port;
            public string input_node_id;
            public string input_port;
            public string asset_path;
            public string graph_name;
            public float position_x;
            public float position_y;
            public int timeout_ms;

            public string Action => action;
            public string SkillName => skill_name;
            public string Parameters => parameters;
            public string RequestId => request_id;
            public string GraphId => graph_id;
            public string NodeType => node_type;
            public string NodeId => node_id;
            public string OutputNodeId => output_node_id;
            public string OutputPort => output_port;
            public string InputNodeId => input_node_id;
            public string InputPort => input_port;
            public string AssetPath => asset_path;
            public string GraphName => graph_name;
            public int TimeoutMs => timeout_ms > 0 ? timeout_ms : 30000;
        }

        [Serializable]
        public class AgentResponse
        {
            public bool Success;
            public string RequestId;
            public string Data;
            public string Error;
        }

        public static AgentRequest ParseRequest(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("Request JSON is empty");

            var request = JsonUtility.FromJson<AgentRequest>(json);

            if (string.IsNullOrEmpty(request.action))
                throw new ArgumentException("Missing 'action' field in request");

            return request;
        }

        public static string CreateSuccessResponse(string data, string requestId = "")
        {
            var response = new AgentResponse
            {
                Success = true,
                RequestId = requestId,
                Data = data,
                Error = "",
            };
            return JsonUtility.ToJson(response);
        }

        public static string CreateErrorResponse(string error, string requestId = "")
        {
            var response = new AgentResponse
            {
                Success = false,
                RequestId = requestId,
                Data = "",
                Error = error,
            };
            return JsonUtility.ToJson(response);
        }
    }
}
