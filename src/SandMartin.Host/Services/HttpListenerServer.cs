using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Rhino;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class HttpListenerServer
    {
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private readonly RequestDispatcher _dispatcher;
        private readonly int _port;

        public HttpListenerServer(RequestDispatcher dispatcher, int port = 8081)
        {
            _dispatcher = dispatcher;
            _port = port;
        }

        public void Start()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
            
            _cts = new CancellationTokenSource();
            
            Task.Run(() => ListenLoop(_cts.Token));
            
            RhinoApp.WriteLine($"SandMartin Server started on port {_port}");
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
            _listener?.Close();
            RhinoApp.WriteLine("SandMartin Server stopped.");
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener.IsListening)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => _dispatcher.HandleRequest(context), token);
                }
                catch (HttpListenerException) when (token.IsCancellationRequested)
                {
                    // Ignore exception when stopping
                }
                catch (Exception)
                {
                    RhinoApp.WriteLine("SandMartin Server Error occurred.");
                }
            }
        }
    }
}
