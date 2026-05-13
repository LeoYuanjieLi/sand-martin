using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SandMartin.Host
{
    public class SandMartinHostInfo : GH_AssemblyInfo
    {
        public override string Name => "SandMartin.Host";
        public override Bitmap Icon => Resources.ResourceLoader.SandMartinIcon;
        public override string Description => "Grasshopper HTTP Server for MCP";
        public override Guid Id => new Guid("7309024a-3406-77f5-8915-bbba466ce306");
        public override string AuthorName => "Sand Martin Contributors";
        public override string AuthorContact => "";
    }
}