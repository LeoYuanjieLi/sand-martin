using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SandMartin.Host.Models;
using SandMartin.Host.Services;
using Xunit;

namespace SandMartin.Host.Tests
{
    public class CanvasManagerTests
    {
        [Fact]
        public async Task GetCanvasState_NotRunningInRhino_ReturnsEmptyNodesList()
        {
            // Arrange
            var manager = new CanvasManager();

            // Act
            var result = await manager.GetCanvasState();
            var response = JsonConvert.DeserializeObject<CanvasStateResponse>(result);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Nodes);
            Assert.Empty(response.Nodes);
        }

        [Fact]
        public async Task CreateNode_NotRunningInRhino_ReturnsErrorJson()
        {
            // Arrange
            // We use the base CanvasManager directly. Since we are running in an xUnit runner
            // and not inside the Rhino UI thread, any UI invocation or active document 
            // check will fail or return null. We expect our code to catch this gracefully.
            var manager = new CanvasManager();
            var request = new CreateNodeRequest
            {
                Type = "Circle",
                Name = "My Circle",
                CanvasX = 100,
                CanvasY = 200
            };

            // Act
            var jsonResult = await manager.CreateNode(request);
            
            // Assert
            Assert.NotNull(jsonResult);
            
            // We parse the JSON to a generic dictionary to check its fields dynamically
            var response = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(jsonResult);
            
            Assert.True(response.ContainsKey("status"), "Response should contain a 'status' field");
            Assert.Equal("error", response["status"]); // Because it's not in Rhino
            
            Assert.True(response.ContainsKey("message"), "Response should contain a 'message' field");
            Assert.Equal("No active Grasshopper document", response["message"]);
        }

        [Fact]
        public async Task UpdateNode_NotRunningInRhino_ReturnsErrorJson()
        {
            // Arrange
            var manager = new CanvasManager();
            var request = new UpdateNodeRequest
            {
                NodeId = Guid.NewGuid().ToString(),
                Name = "Updated Name"
            };

            // Act
            var jsonResult = await manager.UpdateNode(request);
            
            // Assert
            Assert.NotNull(jsonResult);
            
            var response = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(jsonResult);
            
            Assert.True(response.ContainsKey("status"), "Response should contain a 'status' field");
            Assert.Equal("error", response["status"]);
            
            Assert.True(response.ContainsKey("message"), "Response should contain a 'message' field");
            Assert.Equal("No active Grasshopper document", response["message"]);
        }

        [Fact]
        public async Task DeleteNode_NotRunningInRhino_ReturnsErrorJson()
        {
            // Arrange
            var manager = new CanvasManager();
            var nodeId = Guid.NewGuid().ToString();

            // Act
            var jsonResult = await manager.DeleteNode(nodeId);
            
            // Assert
            Assert.NotNull(jsonResult);
            
            var response = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, string>>(jsonResult);
            
            Assert.True(response.ContainsKey("status"), "Response should contain a 'status' field");
            Assert.Equal("error", response["status"]);
            
            Assert.True(response.ContainsKey("message"), "Response should contain a 'message' field");
            Assert.Equal("No active Grasshopper document", response["message"]);
        }
    }
}