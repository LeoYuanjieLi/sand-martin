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

        [Fact]
        public void TryParseParameterRoute_CollectionRoute_ReturnsNodeId()
        {
            var parsed = RequestDispatcher.TryParseParameterRoute("/node/abc-123/parameter", out var route);

            Assert.True(parsed);
            Assert.Equal("abc-123", route.NodeId);
            Assert.True(route.IsCollection);
            Assert.Null(route.Side);
            Assert.Null(route.Index);
        }

        [Fact]
        public void TryParseParameterRoute_IndexedRoute_ReturnsSideAndIndex()
        {
            var parsed = RequestDispatcher.TryParseParameterRoute("/node/abc-123/parameter/input/2", out var route);

            Assert.True(parsed);
            Assert.Equal("abc-123", route.NodeId);
            Assert.Equal("input", route.Side);
            Assert.Equal(2, route.Index);
            Assert.False(route.IsCollection);
        }

        [Fact]
        public void TryParseParameterRoute_InvalidRoute_ReturnsFalse()
        {
            var parsed = RequestDispatcher.TryParseParameterRoute("/node/abc-123", out var route);

            Assert.False(parsed);
            Assert.Null(route);
        }
    }
}
