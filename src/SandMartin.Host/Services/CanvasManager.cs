using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class CanvasManager
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

        private bool IsRunningInRhino()
        {
            try {
                return Grasshopper.Instances.ActiveCanvas?.Document != null;
            } catch {
                return false;
            }
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

        public virtual Task<string> CreateNode(CreateNodeRequest request)
        {
            try {
                if (IsRunningInRhino())
                {
                    return CreateRhinoNode(request);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> CreateRhinoNode(CreateNodeRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
                        return;
                    }

                    IGH_ObjectProxy proxy = null;
                    foreach (var p in Grasshopper.Instances.ComponentServer.ObjectProxies)
                    {
                        if (p.Desc.Name.Equals(request.Type, StringComparison.OrdinalIgnoreCase) || 
                            p.Type.Name.Equals(request.Type, StringComparison.OrdinalIgnoreCase))
                        {
                            proxy = p;
                            break;
                        }
                    }

                    if (proxy == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = $"Component type '{request.Type}' not found" }));
                        return;
                    }

                    var obj = proxy.CreateInstance();
                    if (obj == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Failed to create component instance" }));
                        return;
                    }

                    obj.CreateAttributes();
                    obj.Attributes.Pivot = new System.Drawing.PointF(request.CanvasX, request.CanvasY);

                    if (!string.IsNullOrEmpty(request.Name)) {
                        obj.NickName = request.Name;
                    }

                    doc.AddObject(obj, false);

                    if (request.Parameters != null)
                    {
                        if (obj is IGH_Component component)
                        {
                            foreach (var kvp in request.Parameters)
                            {
                                if (kvp.Key.Equals("Code", StringComparison.OrdinalIgnoreCase))
                                {
                                    ScriptInjector.SetComponentCode(obj, kvp.Value.ToString());
                                }
                                else
                                {
                                    var inputParam = component.Params.Input.Find(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                                                                                    p.NickName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                                    
                                    if (inputParam != null)
                                    {
                                        inputParam.VolatileData.Clear();
                                        inputParam.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, kvp.Value.ToString());
                                    }
                                }
                            }
                        }
                    }

                    obj.ExpireSolution(true);
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = obj.InstanceGuid.ToString() }));

                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
                }
            }));

            return tcs.Task;
        }

        public virtual Task<string> UpdateNode(UpdateNodeRequest request)
        {
            try {
                if (IsRunningInRhino())
                {
                    return UpdateRhinoNode(request);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> UpdateRhinoNode(UpdateNodeRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
                        return;
                    }

                    if (!Guid.TryParse(request.NodeId, out Guid nodeId)) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Invalid node ID format" }));
                        return;
                    }

                    var obj = doc.FindObject(nodeId, true);
                    if (obj == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Node not found" }));
                        return;
                    }

                    bool modified = false;

                    if (request.CanvasX.HasValue || request.CanvasY.HasValue) {
                        float x = request.CanvasX.HasValue ? request.CanvasX.Value : obj.Attributes.Pivot.X;
                        float y = request.CanvasY.HasValue ? request.CanvasY.Value : obj.Attributes.Pivot.Y;
                        obj.Attributes.Pivot = new System.Drawing.PointF(x, y);
                        obj.Attributes.ExpireLayout();
                        modified = true;
                    }

                    if (!string.IsNullOrEmpty(request.Name)) {
                        obj.NickName = request.Name;
                        modified = true;
                    }

                    if (request.Parameters != null && obj is IGH_Component component)
                    {
                        foreach (var kvp in request.Parameters)
                        {
                            if (kvp.Key.Equals("Code", StringComparison.OrdinalIgnoreCase))
                            {
                                bool codeSet = ScriptInjector.SetComponentCode(obj, kvp.Value.ToString());
                                if (codeSet) modified = true;
                            }
                            else
                            {
                                var inputParam = component.Params.Input.Find(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) || 
                                                                                 p.NickName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                                
                                if (inputParam != null)
                                {
                                    inputParam.VolatileData.Clear();
                                    inputParam.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, kvp.Value.ToString());
                                    modified = true;
                                }
                            }
                        }
                    }

                    if (modified) {
                        obj.Attributes.ExpireLayout();
                        obj.ExpireSolution(true);
                    }

                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = obj.InstanceGuid.ToString() }));

                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
                }
            }));

            return tcs.Task;
        }

        public virtual Task<string> DeleteNode(string nodeId)
        {
            try {
                if (IsRunningInRhino())
                {
                    return DeleteRhinoNode(nodeId);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> DeleteRhinoNode(string nodeId)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
                        return;
                    }

                    if (!Guid.TryParse(nodeId, out Guid guid)) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Invalid node ID format" }));
                        return;
                    }

                    var obj = doc.FindObject(guid, true);
                    if (obj == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Node not found" }));
                        return;
                    }

                    doc.RemoveObject(obj, true);

                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = nodeId }));

                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
                }
            }));

            return tcs.Task;
        }

        public virtual Task<string> CreateConnection(ConnectionRequest request)
        {
            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Not implemented yet" }));
        }
    }
}