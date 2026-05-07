using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class CanvasManager
    {
        public Task<string> CreateNode(CreateNodeRequest request)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Not implemented yet" }));
        }

        public Task<string> UpdateCode(UpdateCodeRequest request)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Not implemented yet" }));
        }

        public Task<string> ConnectNodes(ConnectionRequest request)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Not implemented yet" }));
        }

        public Task<string> GetCanvasState()
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { nodes = new List<NodeInfo>() }));
                        return;
                    }

                    var nodes = new List<NodeInfo>();
                    foreach (var obj in doc.Objects)
                    {
                        var node = new NodeInfo {
                            Id = obj.InstanceGuid.ToString(),
                            Name = obj.Name,
                            Nickname = obj.NickName,
                            Type = obj.GetType().Name,
                            X = obj.Attributes.Pivot.X,
                            Y = obj.Attributes.Pivot.Y
                        };

                        if (obj is IGH_Component component)
                        {
                            for (int i = 0; i < component.Params.Input.Count; i++)
                            {
                                var p = component.Params.Input[i];
                                var paramInfo = new ParameterInfo { Name = p.Name, Nickname = p.NickName, Index = i };
                                foreach (var source in p.Sources)
                                {
                                    paramInfo.Connections.Add(new ConnectionInfo {
                                        TargetId = source.Attributes.GetTopLevel.DocObject.InstanceGuid.ToString(),
                                        TargetIndex = 0 
                                    });
                                }
                                node.Inputs.Add(paramInfo);
                            }

                            for (int i = 0; i < component.Params.Output.Count; i++)
                            {
                                var p = component.Params.Output[i];
                                node.Outputs.Add(new ParameterInfo { Name = p.Name, Nickname = p.NickName, Index = i });
                            }
                        }
                        
                        nodes.Add(node);
                    }

                    tcs.SetResult(JsonConvert.SerializeObject(new { nodes = nodes }));
                } catch (Exception ex) {
                    tcs.SetException(ex);
                }
            }));

            return tcs.Task;
        }
    }
}
