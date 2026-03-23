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

            public string Action => action;
            public string SkillName => skill_name;
            public string Parameters => parameters;
            public string RequestId => request_id;
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
