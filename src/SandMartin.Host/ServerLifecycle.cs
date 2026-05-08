using System;
using Grasshopper.Kernel;
using SandMartin.Host.Services;

namespace SandMartin.Host
{
    public class ServerLifecycle : GH_AssemblyPriority
    {
        private HttpListenerServer _server;

        public override GH_LoadingInstruction PriorityLoad()
        {
            var manager = new CanvasManager();
            var dispatcher = new RequestDispatcher(manager);
            _server = new HttpListenerServer(dispatcher);
            _server.Start();

            return GH_LoadingInstruction.Proceed;
        }
    }
}
