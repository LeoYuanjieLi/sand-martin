using System;
using Grasshopper.Kernel;
using SandMartin.Host.Services;

namespace SandMartin.Host.Components
{
    public class SandMartinServerComponent : GH_Component
    {
        private bool _lastRunState = false;

        public SandMartinServerComponent()
          : base("Sand Martin Server", "SandMartin",
              "Starts the Sand Martin MCP HTTP Server",
              "Sand Martin", "Server")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "R", "Set to true to start the server", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("AllowCodeInjection", "C", "Set to true to allow the AI to inject and execute code (e.g. C# or Python components)", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Server status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            bool allowCode = true;
            if (!DA.GetData(0, ref run)) return;
            DA.GetData(1, ref allowCode);

            var manager = ServerManager.Instance;

            // Only trigger actions on state transitions to avoid accidental stops from new components
            if (run && !_lastRunState)
            {
                // Transition: False -> True
                manager.Start(allowCode);
            }
            else if (!run && _lastRunState)
            {
                // Transition: True -> False
                manager.Stop();
            }
            else if (run && manager.IsRunning)
            {
                // Keep-alive/Update: Update settings if already running and input is True
                manager.UpdateSecuritySettings(allowCode);
            }

            _lastRunState = run;

            // Always synchronize UI and output with the actual global singleton state
            if (manager.IsRunning)
            {
                Message = "Running";
                DA.SetData(0, $"Server is running on port 8081. Code injection: {manager.AllowCodeInjection}");
            }
            else
            {
                Message = "Stopped";
                DA.SetData(0, "Server is stopped");
            }
        }

        protected override System.Drawing.Bitmap Icon => SandMartin.Host.Resources.ResourceLoader.SandMartinIcon;

        public override Guid ComponentGuid => new Guid("B531B51A-932F-4DA3-8CC8-8BC9C8F9FEE6");
    }
}