using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            // with a dummy manager.
            
            // Arrange
            // Avoid using Moq on CanvasManager here because Moq tries to reflect over the class
            // to generate a proxy. Since CanvasManager uses Grasshopper types (like IGH_DocumentObject)
            // in its method signatures, Moq fails when it can't load the Grasshopper assembly 
            // in a headless xUnit environment.
            var dummyManager = new CanvasManager();
            var dispatcher = new RequestDispatcher(dummyManager, "test_token", true);

            // Assert
            Assert.NotNull(dispatcher);
        }
    }
}