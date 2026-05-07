using System;
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

        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }
    }

    public class UpdateCodeRequest
    {
        [JsonProperty("node_id")]
        public string NodeId { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }
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

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }
    }
}
