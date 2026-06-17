using Newtonsoft.Json;
using SandMartin.Host.Models;
using Xunit;

namespace SandMartin.Host.Tests
{
    public class ModelsTests
    {
        [Fact]
        public void NodeInfo_Serialization_ReturnsCorrectJson()
        {
            // Arrange
            var node = new NodeInfo
            {
                Id = "guid-123",
                Name = "Circle",
                Nickname = "C",
                Type = "GH_Component",
                X = 100,
                Y = 200
            };

            node.Inputs.Add(new ParameterInfo { Name = "Radius", Index = 0, Access = "item", Optional = true, Type = "Param_Number" });
            node.Outputs.Add(new ParameterInfo { Name = "Circle", Index = 0, Access = "item", Type = "Param_Curve" });

            // Act
            var json = JsonConvert.SerializeObject(node);

            // Assert
            Assert.Contains("\"id\":\"guid-123\"", json);
            Assert.Contains("\"name\":\"Circle\"", json);
            Assert.Contains("\"x\":100.0", json);
            Assert.Contains("\"inputs\":[{\"name\":\"Radius\"", json);
            Assert.Contains("\"access\":\"item\"", json);
            Assert.Contains("\"optional\":true", json);
            Assert.Contains("\"type\":\"Param_Number\"", json);
        }

        [Fact]
        public void CanvasStateResponse_Serialization_HandlesEmptyNodes()
        {
            // Arrange
            var response = new CanvasStateResponse { Nodes = new System.Collections.Generic.List<NodeInfo>() };

            // Act
            var json = JsonConvert.SerializeObject(response);

            // Assert
            Assert.Equal("{\"nodes\":[]}", json);
        }

        [Fact]
        public void DisconnectRequest_Serialization_ReturnsCorrectJson()
        {
            // Arrange
            var request = new DisconnectRequest
            {
                SourceId = "source-guid",
                TargetId = "target-guid"
            };

            // Act
            var json = JsonConvert.SerializeObject(request);

            // Assert
            Assert.Contains("\"source_id\":\"source-guid\"", json);
            Assert.Contains("\"target_id\":\"target-guid\"", json);
        }

        [Fact]
        public void ComponentParameterRequest_Serialization_ReturnsCorrectJson()
        {
            var request = new ComponentParameterRequest
            {
                NodeId = "node-guid",
                Side = "input",
                Index = 1,
                Name = "radius",
                Nickname = "r",
                Description = "Input radius",
                Access = "item",
                Optional = true,
                ParameterType = "generic"
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.Contains("\"nodeId\":\"node-guid\"", json);
            Assert.Contains("\"side\":\"input\"", json);
            Assert.Contains("\"index\":1", json);
            Assert.Contains("\"name\":\"radius\"", json);
            Assert.Contains("\"nickname\":\"r\"", json);
            Assert.Contains("\"description\":\"Input radius\"", json);
            Assert.Contains("\"access\":\"item\"", json);
            Assert.Contains("\"optional\":true", json);
            Assert.Contains("\"parameterType\":\"generic\"", json);
        }

        [Fact]
        public void UpdateComponentParameterRequest_Serialization_ReturnsCorrectJson()
        {
            var request = new UpdateComponentParameterRequest
            {
                NodeId = "node-guid",
                Side = "output",
                Index = 0,
                Name = "result",
                Nickname = "result",
                Access = "list",
                Optional = false
            };

            var json = JsonConvert.SerializeObject(request);

            Assert.Contains("\"nodeId\":\"node-guid\"", json);
            Assert.Contains("\"side\":\"output\"", json);
            Assert.Contains("\"index\":0", json);
            Assert.Contains("\"name\":\"result\"", json);
            Assert.Contains("\"access\":\"list\"", json);
            Assert.Contains("\"optional\":false", json);
        }
    }
}
