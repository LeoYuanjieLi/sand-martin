using System;
using System.IO;
using SandMartin.Host.Services;

namespace SandMartin.Host.Services
{
    public class ServerManager
    {
        private static ServerManager _instance;
        public static ServerManager Instance => _instance ??= new ServerManager();

        public event EventHandler ServerStateChanged;

        private HttpListenerServer _server;
        private bool _isRunning = false;
        private string _authToken;
        private bool _allowCodeInjection = true;

        public bool IsRunning => _isRunning;
        public string AuthToken => _authToken;
        public bool AllowCodeInjection => _allowCodeInjection;

        private ServerManager() { }

        public void Start(bool allowCodeInjection)
        {
            if (_isRunning && _server != null)
            {
                // Update settings if already running
                _allowCodeInjection = allowCodeInjection;
                _server.UpdateSecuritySettings(_authToken, _allowCodeInjection);
                return;
            }

            _allowCodeInjection = allowCodeInjection;
            _authToken = GenerateAuthToken();

            var manager = new CanvasManager();
            var dispatcher = new RequestDispatcher(manager, _authToken, _allowCodeInjection);
            _server = new HttpListenerServer(dispatcher);
            
            _server.Start();
            _isRunning = true;
            WriteTokenToFile(_authToken);

            Rhino.RhinoApp.WriteLine("--------------------------------------------------");
            Rhino.RhinoApp.WriteLine("SAND MARTIN SERVER STARTED (STICKY)");
            Rhino.RhinoApp.WriteLine($"TOKEN: {_authToken}");
            Rhino.RhinoApp.WriteLine("--------------------------------------------------");

            ServerStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _server?.Stop();
            _server = null;
            _isRunning = false;
            DeleteTokenFile();
            
            Rhino.RhinoApp.WriteLine("Sand Martin Server stopped.");

            ServerStateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void UpdateSecuritySettings(bool allowCodeInjection)
        {
            _allowCodeInjection = allowCodeInjection;
            _server?.UpdateSecuritySettings(null, _allowCodeInjection);
        }

        private string GetTokenFilePath()
        {
            return Path.Combine(Path.GetTempPath(), "sand_martin.token");
        }

        private void WriteTokenToFile(string token)
        {
            try
            {
                string path = GetTokenFilePath();
                File.WriteAllText(path, token);
                Rhino.RhinoApp.WriteLine($"[SandMartin] Security token saved to: {path}");
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[SandMartin] Error writing token file: {ex.Message}");
            }
        }

        private void DeleteTokenFile()
        {
            try
            {
                string path = GetTokenFilePath();
                if (File.Exists(path))
                {
                    File.Delete(path);
                    Rhino.RhinoApp.WriteLine("[SandMartin] Security token file deleted.");
                }
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"[SandMartin] Error deleting token file: {ex.Message}");
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
    }
}
