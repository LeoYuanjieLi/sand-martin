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

            if (run && !_isRunning)
            {
                // Generate a random 32-character token for security
                string token = GenerateAuthToken();
                Rhino.RhinoApp.WriteLine("--------------------------------------------------");
                Rhino.RhinoApp.WriteLine("SAND MARTIN SECURITY TOKEN GENERATED");
                Rhino.RhinoApp.WriteLine($"TOKEN: {token}");
                Rhino.RhinoApp.WriteLine("Set the SAND_MARTIN_TOKEN environment variable to this value.");
                Rhino.RhinoApp.WriteLine("--------------------------------------------------");

                if (_server == null)
                {
                    var manager = new CanvasManager();
                    var dispatcher = new RequestDispatcher(manager, token, allowCode);
                    _server = new HttpListenerServer(dispatcher);
                }
                else
                {
                    // Update flags if server already exists
                    _server.UpdateSecuritySettings(token, allowCode);
                }
                
                _server.Start();
                _isRunning = true;
                WriteTokenToFile(token);
                Message = allowCode ? "Running (Insecure Mode)" : "Running (Secure Mode)";
            }
            else if (run && _isRunning)
            {
                // Update code injection flag if changed while running
                _server?.UpdateSecuritySettings(null, allowCode);
                Message = allowCode ? "Running (Insecure Mode)" : "Running (Secure Mode)";
            }
            else if (!run && _isRunning)
            {
                _server?.Stop();
                _isRunning = false;
                DeleteTokenFile();
                Message = "Stopped";
            }
            else if (!run && !_isRunning)
            {
                Message = "Stopped";
            }
            
            DA.SetData(0, _isRunning ? $"Server is running on port 8081. Code injection: {allowCode}" : "Server is stopped");
        }

        private string GetTokenFilePath()
        {
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "sand_martin.token");
        }

        private void WriteTokenToFile(string token)
        {
            try {
                System.IO.File.WriteAllText(GetTokenFilePath(), token);
            } catch (Exception) {
                // Ignore errors
            }
        }

        private void DeleteTokenFile()
        {
            try {
                string path = GetTokenFilePath();
                if (System.IO.File.Exists(path)) {
                    System.IO.File.Delete(path);
                }
            } catch (Exception) {
                // Ignore errors
            }
        }

        private string GenerateAuthToken()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var result = new char[32];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
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