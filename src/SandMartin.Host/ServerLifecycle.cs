using System;
using Grasshopper.Kernel;
using SandMartin.Host.Services;

namespace SandMartin.Host
{
    public class ServerLifecycle : GH_AssemblyPriority
    {
        private static HttpListenerServer _server;

        public override GH_LoadingInstruction PriorityLoad()
        {
            var canvasManager = new CanvasManager();
            var dispatcher = new RequestDispatcher(canvasManager);
            _server = new HttpListenerServer(dispatcher);
            
            _server.Start();

            // Register event to stop server when Rhino closes
            Rhino.RhinoApp.Closing += (s, e) => _server.Stop();

            return GH_LoadingInstruction.Proceed;
        }
    }
}
