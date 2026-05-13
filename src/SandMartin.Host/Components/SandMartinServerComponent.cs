using System;
using Grasshopper.Kernel;
using SandMartin.Host.Services;

namespace SandMartin.Host.Components
{
    public class SandMartinServerComponent : GH_Component
    {
        private static HttpListenerServer _server;
        private bool _isRunning = false;

        public SandMartinServerComponent()
          : base("Sand Martin Server", "SandMartin",
              "Starts the Sand Martin MCP HTTP Server",
              "Sand Martin", "Server")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Set to true to start the server", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Server status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            if (!DA.GetData(0, ref run)) return;

            if (run && !_isRunning)
            {
                if (_server == null)
                {
                    var manager = new CanvasManager();
                    var dispatcher = new RequestDispatcher(manager);
                    _server = new HttpListenerServer(dispatcher);
                }
                
                _server.Start();
                _isRunning = true;
                Message = "Running";
            }
            else if (!run && _isRunning)
            {
                _server?.Stop();
                _isRunning = false;
                Message = "Stopped";
            }
            else if (!run && !_isRunning)
            {
                Message = "Stopped";
            }
            
            DA.SetData(0, _isRunning ? "Server is running on port 8081" : "Server is stopped");
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            if (_isRunning)
            {
                _server?.Stop();
                _isRunning = false;
            }
            base.RemovedFromDocument(document);
        }

        protected override System.Drawing.Bitmap Icon => SandMartin.Host.Resources.ResourceLoader.SandMartinIcon;

        public override Guid ComponentGuid => new Guid("B531B51A-932F-4DA3-8CC8-8BC9C8F9FEE6");
    }
}