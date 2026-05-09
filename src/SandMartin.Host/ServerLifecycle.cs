using System;
using Grasshopper.Kernel;

namespace SandMartin.Host
{
    public class ServerLifecycle : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            // Do not automatically start the server here anymore.
            // The server will now be managed by the SandMartinServerComponent.
            return GH_LoadingInstruction.Proceed;
        }
    }
}