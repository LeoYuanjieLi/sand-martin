using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Special;
using Newtonsoft.Json;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class CanvasManager
    {
        public Task<string> CreateNode(CreateNodeRequest request)
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var canvas = Instances.ActiveCanvas;
                    var document = canvas.Document;
                    if (document == null) throw new Exception("No active Grasshopper document.");

                    // Logic to find and create the component by 'type' string
                    // This is a placeholder for actual component instantiation logic
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", message = $"Created {request.Type}" }));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }

        public Task<string> UpdateCode(UpdateCodeRequest request)
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    // Logic to find node by GUID and inject code
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success" }));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }

        public Task<string> ConnectNodes(ConnectionRequest request)
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    // Logic to wire parameters
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success" }));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }

        public Task<string> GetCanvasState()
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Instances.ActiveCanvas.Document;
                    var nodes = doc.Objects.Select(obj => new NodeInfo {
                        Id = obj.InstanceGuid.ToString(),
                        Name = obj.Name,
                        Nickname = obj.NickName,
                        Type = obj.GetType().Name
                    }).ToList();

                    tcs.SetResult(JsonConvert.SerializeObject(new { nodes = nodes }));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }
    }
}
