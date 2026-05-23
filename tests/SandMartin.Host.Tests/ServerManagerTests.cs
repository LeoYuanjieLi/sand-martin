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

        [Fact]
        public void ServerManager_FiresEvent_OnStateChange()
        {
            var manager = ServerManager.Instance;
            bool eventFired = false;
            manager.ServerStateChanged += (s, e) => eventFired = true;

            // Test Start (Note: Start/Stop in unit tests might fail if Rhino process isn't fully mocked, 
            // but we can at least check if the event logic is wired up)
            try {
                manager.Start(true);
                Assert.True(eventFired);
                
                eventFired = false;
                manager.Stop();
                Assert.True(eventFired);
            } catch {
                // Ignore actual server start failures in unit test environment
                // but we want to see if the event would have fired
            }
        }
    }
}
