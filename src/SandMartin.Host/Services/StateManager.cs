using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using SandMartin.Host.Models;
using Models = SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class StateManager : GrasshopperServiceBase
    {
        public virtual Task<string> GetCanvasState()
        {
            try {
                if (IsRunningInRhino())
                {
                    return GetRhinoCanvasState();
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { nodes = new List<NodeInfo>() }));
        }

        private Task<string> GetRhinoCanvasState()
        {
            var tcs = new TaskCompletionSource<string>();
            
            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
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
                                var paramInfo = new Models.ParameterInfo { Name = p.Name, Nickname = p.NickName, Index = i };
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
                                node.Outputs.Add(new Models.ParameterInfo { Name = p.Name, Nickname = p.NickName, Index = i });
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
