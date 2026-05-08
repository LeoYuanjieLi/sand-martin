using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Newtonsoft.Json;
using SandMartin.Host.Models;
using SandMartin.Host.Services;
using Xunit;

namespace SandMartin.Host.Tests
{
    public class RequestDispatcherTests
    {
        [Fact]
        public async Task CanvasManager_GetCanvasState_ReturnsValidJson()
        {
            // Testing the CanvasManager directly in .NET 7.0 mode 
            // to ensure it returns the expected "empty" state logic we set up.
            
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
        public void RequestDispatcher_Initialization_Works()
        {
            // Simple architectural test to ensure the dispatcher can be instantiated
            // with a mocked manager.
            
            // Arrange
            var mockManager = new Mock<CanvasManager>();
            var dispatcher = new RequestDispatcher(mockManager.Object);

            // Assert
            Assert.NotNull(dispatcher);
        }
    }
}
