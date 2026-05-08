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

            node.Inputs.Add(new ParameterInfo { Name = "Radius", Index = 0 });
            node.Outputs.Add(new ParameterInfo { Name = "Circle", Index = 0 });

            // Act
            var json = JsonConvert.SerializeObject(node);

            // Assert
            Assert.Contains("\"id\":\"guid-123\"", json);
            Assert.Contains("\"name\":\"Circle\"", json);
            Assert.Contains("\"x\":100.0", json);
            Assert.Contains("\"inputs\":[{\"name\":\"Radius\"", json);
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
    }
}
