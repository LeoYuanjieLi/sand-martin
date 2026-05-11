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