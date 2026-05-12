using System.Collections.Generic;
using Newtonsoft.Json;

namespace SandMartin.Host.Models
{
    public class CreateNodeRequest
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("canvasX")]
        public int CanvasX { get; set; }

        [JsonProperty("canvasY")]
        public int CanvasY { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class UpdateNodeRequest
    {
        [JsonProperty("nodeId")]
        public string NodeId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("canvasX")]
        public int? CanvasX { get; set; }

        [JsonProperty("canvasY")]
        public int? CanvasY { get; set; }

        [JsonProperty("parameters")]
        public Dictionary<string, object> Parameters { get; set; }
    }

    public class ConnectionRequest
    {
        [JsonProperty("source_id")]
        public string SourceId { get; set; }

        [JsonProperty("source_output_index")]
        public int SourceOutputIndex { get; set; }

        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("target_input_index")]
        public int TargetInputIndex { get; set; }
    }

    public class CanvasStateResponse
    {
        [JsonProperty("nodes")]
        public List<NodeInfo> Nodes { get; set; }
    }

    public class NodeInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("inputs")]
        public List<ParameterInfo> Inputs { get; set; } = new List<ParameterInfo>();

        [JsonProperty("outputs")]
        public List<ParameterInfo> Outputs { get; set; } = new List<ParameterInfo>();
    }

    public class ParameterInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("connections")]
        public List<ConnectionInfo> Connections { get; set; } = new List<ConnectionInfo>();
    }

    public class ConnectionInfo
    {
        [JsonProperty("target_id")]
        public string TargetId { get; set; }

        [JsonProperty("target_index")]
        public int TargetIndex { get; set; }
    }
}