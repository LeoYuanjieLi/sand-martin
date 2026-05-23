using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using SandMartin.Host.Models;
using Models = SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class NodeManager : GrasshopperServiceBase
    {
        public virtual Task<string> GetNodeDetails(string nodeId)
        {
            try {
                if (IsRunningInRhino())
                {
                    return GetRhinoNodeDetails(nodeId);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> GetRhinoNodeDetails(string nodeId)
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

                    var nodeInfo = new NodeInfo {
                        Id = obj.InstanceGuid.ToString(),
                        Name = obj.Name,
                        Nickname = obj.NickName,
                        Type = obj.GetType().Name,
                        X = obj.Attributes.Pivot.X,
                        Y = obj.Attributes.Pivot.Y
                    };

                    // Extract properties via reflection
                    var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var prop in props)
                    {
                        try {
                            if (prop.CanRead)
                            {
                                var type = prop.PropertyType;
                                // Only include simple types to avoid serialization issues
                                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type.IsEnum)
                                {
                                    nodeInfo.Parameters[prop.Name] = new PropertyDetail { 
                                        Value = prop.GetValue(obj), 
                                        IsReadOnly = !prop.CanWrite 
                                    };
                                }
                            }
                        } catch {
                            // Skip properties that fail to read
                        }
                    }

                    // Extract script code if applicable
                    var code = ScriptInjector.GetComponentCode(obj);
                    if (code != null)
                    {
                        nodeInfo.Parameters["Code"] = new PropertyDetail { Value = code, IsReadOnly = false };
                    }

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
                            nodeInfo.Inputs.Add(paramInfo);
                        }

                        for (int i = 0; i < component.Params.Output.Count; i++)
                        {
                            var p = component.Params.Output[i];
                            nodeInfo.Outputs.Add(new Models.ParameterInfo { Name = p.Name, Nickname = p.NickName, Index = i });
                        }
                    }

                    tcs.SetResult(JsonConvert.SerializeObject(nodeInfo));
                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
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

                    if (request.Parameters != null)
                    {
                        var errors = new List<string>();

                        foreach (var kvp in request.Parameters)
                        {
                            try {
                                if (kvp.Key.Equals("Code", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!ScriptInjector.SetComponentCode(obj, kvp.Value.ToString()))
                                    {
                                        errors.Add($"Failed to inject code for parameter '{kvp.Key}'.");
                                    }
                                    else
                                    {
                                        modified = true;
                                    }
                                }
                                else
                                {
                                    bool propertySet = false;

                                    // 1. Try generic reflection for public properties (e.g., CurrentValue, Value, UserText)
                                    var prop = obj.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                    if (prop != null)
                                    {
                                        if (prop.CanWrite)
                                        {
                                            try {
                                                var targetType = prop.PropertyType;
                                                object convertedValue;
                                                if (targetType == typeof(decimal)) {
                                                    convertedValue = Convert.ToDecimal(kvp.Value);
                                                } else {
                                                    convertedValue = Convert.ChangeType(kvp.Value, targetType);
                                                }
                                                
                                                prop.SetValue(obj, convertedValue);
                                                propertySet = true;
                                                modified = true;
                                                Rhino.RhinoApp.WriteLine($"[SandMartin] Set property {prop.Name} on {obj.GetType().Name}");
                                            } catch (Exception ex) {
                                                errors.Add($"Failed to set property '{kvp.Key}': {ex.Message}");
                                            }
                                        }
                                        else
                                        {
                                            errors.Add($"Property '{kvp.Key}' is read-only.");
                                        }
                                    }

                                    // 2. Fallback to IGH_Component inputs
                                    if (!propertySet && obj is IGH_Component component)
                                    {
                                        var inputParam = component.Params.Input.Find(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                                                                                         p.NickName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));

                                        if (inputParam != null)
                                        {
                                            inputParam.VolatileData.Clear();
                                            inputParam.AddVolatileData(new Grasshopper.Kernel.Data.GH_Path(0), 0, kvp.Value.ToString());
                                            propertySet = true;
                                            modified = true;
                                            Rhino.RhinoApp.WriteLine($"[SandMartin] Set input {inputParam.NickName} on {obj.GetType().Name}");
                                        }
                                    }

                                    if (!propertySet && !errors.Exists(e => e.Contains($"'{kvp.Key}'")))
                                    {
                                        errors.Add($"Parameter or property '{kvp.Key}' not found on {obj.GetType().Name}.");
                                    }
                                }
                            } catch (Exception ex) {
                                errors.Add($"Error processing parameter '{kvp.Key}': {ex.Message}");
                            }
                        }

                        if (errors.Count > 0)
                        {
                            tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = string.Join(" ", errors) }));
                            return;
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
    }
}
