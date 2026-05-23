using System;
using Xunit;
using SandMartin.Host.Services;
using SandMartin.Host.Components;
using Grasshopper.Kernel;
using Moq;

namespace SandMartin.Host.Tests
{
    public class ServerManagerTests
    {
        [Fact]
        public void ServerManager_IsSingleton()
        {
            var instance1 = ServerManager.Instance;
            var instance2 = ServerManager.Instance;
            
            Assert.Same(instance1, instance2);
        }

        [Fact]
        public void ServerManager_InitialState_IsStopped()
        {
            // Note: Since it's a singleton, this might be affected by other tests
            // but in a clean run it should be stopped.
            var manager = ServerManager.Instance;
            Assert.False(manager.IsRunning);
        }
    }
}
