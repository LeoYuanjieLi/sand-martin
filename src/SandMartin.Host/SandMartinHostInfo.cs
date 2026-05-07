using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SandMartin.Host
{
    public class SandMartinHostInfo : GH_AssemblyInfo
    {
        public override string Name => "SandMartin Host";
        
        // Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        public override string Description => "HTTP Server for remote Grasshopper orchestration via MCP.";

        public override Guid Id => new Guid("B5D96525-452D-4C5D-8A7B-9E3D9E3D9E3D"); // Replace with a stable GUID

        public override string AuthorName => "SandMartin Team";

        public override string AuthorContact => "https://github.com/your-repo/sand-martin";
    }
}
