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
                            var paramInfo = CreateParameterInfo(p, i);
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
                            nodeInfo.Outputs.Add(CreateParameterInfo(p, i));
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
            if (request == null)
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Create request body is required" }));
            }

            if (string.IsNullOrWhiteSpace(request.Type))
            {
                return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "Component type is required" }));
            }

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
                        if (p == null)
                        {
                            continue;
                        }

                        var proxyName = p.Desc?.Name;
                        var proxyTypeName = p.Type?.Name;
                        if (string.Equals(proxyName, request.Type, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(proxyTypeName, request.Type, StringComparison.OrdinalIgnoreCase))
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
                    if (obj.Attributes == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = $"Component type '{request.Type}' did not create canvas attributes" }));
                        return;
                    }

                    obj.Attributes.Pivot = new System.Drawing.PointF(request.CanvasX, request.CanvasY);

                    if (!string.IsNullOrEmpty(request.Name)) {
                        obj.NickName = request.Name;
                    }

                    // Rhino 8 initializes SDK script state only after the component is
                    // attached to a document. Parameters such as Code must be applied
                    // after this point or the script context falls back to legacy mode.
                    doc.AddObject(obj, false);

                    if (request.Parameters != null)
                    {
                        foreach (var kvp in request.Parameters)
                        {
                            bool set = false;
                            if (kvp.Value == null)
                            {
                                doc.RemoveObject(obj, true);
                                tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = $"Parameter '{kvp.Key}' cannot be null" }));
                                return;
                            }

                            // 1. Try Code Injection
                            if (kvp.Key.Equals("Code", StringComparison.OrdinalIgnoreCase))
                            {
                                if (ScriptInjector.SetComponentCode(obj, kvp.Value.ToString())) set = true;
                            }

                            // 2. Try reflection (for properties like Text on Scribbles, or Value on Sliders)
                            if (!set)
                            {
                                try {
                                    // Search Properties including base classes
                                    var prop = obj.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
                                    if (prop != null && prop.CanWrite)
                                    {
                                        var targetType = prop.PropertyType;
                                        object convertedValue = (targetType == typeof(string)) ? kvp.Value.ToString() :
                                                               (targetType == typeof(decimal)) ? Convert.ToDecimal(kvp.Value) :
                                                               Convert.ChangeType(kvp.Value, targetType);
                                        prop.SetValue(obj, convertedValue);
                                        set = true;
                                    }

                                    // Search Fields (if property not found)
                                    if (!set)
                                    {
                                        var field = obj.GetType().GetField(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy) ??
                                                    obj.GetType().GetField("m_" + kvp.Key, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                        if (field != null)
                                        {
                                            var targetType = field.FieldType;
                                            object convertedValue = (targetType == typeof(string)) ? kvp.Value.ToString() :
                                                                   (targetType == typeof(decimal)) ? Convert.ToDecimal(kvp.Value) :
                                                                   Convert.ChangeType(kvp.Value, targetType);
                                            field.SetValue(obj, convertedValue);
                                            set = true;
                                        }
                                    }
                                } catch { }
                            }

                            // 3. Try IGH_Component Inputs
                            if (!set && obj is IGH_Component component)
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

        public virtual Task<string> AddComponentParameter(ComponentParameterRequest request)
        {
            var validationError = ValidateParameterMutationRequest(request, requireIndex: false);
            if (validationError != null)
            {
                return Task.FromResult(Error(validationError));
            }

            try {
                if (IsRunningInRhino())
                {
                    return AddRhinoComponentParameter(request);
                }
            } catch {
            }

            return Task.FromResult(Error("No active Grasshopper document"));
        }

        public virtual Task<string> UpdateComponentParameter(UpdateComponentParameterRequest request)
        {
            if (request == null)
            {
                return Task.FromResult(Error("Parameter request body is required"));
            }

            var validationError = ValidateParameterSide(request.Side);
            if (validationError != null)
            {
                return Task.FromResult(Error(validationError));
            }

            if (string.IsNullOrWhiteSpace(request.NodeId))
            {
                return Task.FromResult(Error("Node ID is required"));
            }

            if (request.Index < 0)
            {
                return Task.FromResult(Error("Parameter index must be greater than or equal to zero"));
            }

            try {
                if (IsRunningInRhino())
                {
                    return UpdateRhinoComponentParameter(request);
                }
            } catch {
            }

            return Task.FromResult(Error("No active Grasshopper document"));
        }

        public virtual Task<string> RemoveComponentParameter(ComponentParameterRequest request)
        {
            var validationError = ValidateParameterMutationRequest(request, requireIndex: true);
            if (validationError != null)
            {
                return Task.FromResult(Error(validationError));
            }

            try {
                if (IsRunningInRhino())
                {
                    return RemoveRhinoComponentParameter(request);
                }
            } catch {
            }

            return Task.FromResult(Error("No active Grasshopper document"));
        }

        private Task<string> AddRhinoComponentParameter(ComponentParameterRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(Error("No active Grasshopper document"));
                        return;
                    }

                    if (!TryFindComponent(doc, request.NodeId, out var component, out var error)) {
                        tcs.SetResult(Error(error));
                        return;
                    }

                    var side = ToParameterSide(request.Side);
                    int index = request.Index ?? GetParameterList(component, side).Count;
                    if (index < 0 || index > GetParameterList(component, side).Count) {
                        tcs.SetResult(Error("Parameter index is outside the valid insertion range"));
                        return;
                    }

                    var variable = component as IGH_VariableParameterComponent;
                    if (variable == null) {
                        tcs.SetResult(Error("Component type does not support variable parameters"));
                        return;
                    }

                    if (!variable.CanInsertParameter(side, index)) {
                        tcs.SetResult(Error($"Component rejected inserting a {request.Side} parameter at index {index}"));
                        return;
                    }

                    var param = variable.CreateParameter(side, index);
                    if (param == null) {
                        tcs.SetResult(Error("Component did not create a parameter"));
                        return;
                    }

                    var propertyError = ApplyParameterProperties(component, param, request.Name, request.Nickname, request.Description, request.Access, request.Optional);
                    if (propertyError != null) {
                        tcs.SetResult(Error(propertyError));
                        return;
                    }

                    bool registered = side == GH_ParameterSide.Input
                        ? component.Params.RegisterInputParam(param, index)
                        : component.Params.RegisterOutputParam(param, index);

                    if (!registered) {
                        tcs.SetResult(Error("Failed to register component parameter"));
                        return;
                    }

                    variable.VariableParameterMaintenance();
                    var maintainedParam = GetParameterList(component, side)[index];
                    propertyError = ApplyParameterProperties(component, maintainedParam, request.Name, request.Nickname, request.Description, request.Access, request.Optional);
                    if (propertyError != null) {
                        tcs.SetResult(Error(propertyError));
                        return;
                    }

                    RefreshComponent(component);
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = component.InstanceGuid.ToString(), index = index }));
                } catch (Exception ex) {
                    tcs.SetResult(Error(ex.Message));
                }
            }));

            return tcs.Task;
        }

        private Task<string> UpdateRhinoComponentParameter(UpdateComponentParameterRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(Error("No active Grasshopper document"));
                        return;
                    }

                    if (!TryFindComponent(doc, request.NodeId, out var component, out var error)) {
                        tcs.SetResult(Error(error));
                        return;
                    }

                    var side = ToParameterSide(request.Side);
                    var parameters = GetParameterList(component, side);
                    if (request.Index < 0 || request.Index >= parameters.Count) {
                        tcs.SetResult(Error("Parameter index is outside the valid range"));
                        return;
                    }

                    var param = parameters[request.Index];
                    var propertyError = ApplyParameterProperties(component, param, request.Name, request.Nickname, request.Description, request.Access, request.Optional);
                    if (propertyError != null) {
                        tcs.SetResult(Error(propertyError));
                        return;
                    }

                    if (component is IGH_VariableParameterComponent variable) {
                        variable.VariableParameterMaintenance();
                        var maintainedParam = GetParameterList(component, side)[request.Index];
                        propertyError = ApplyParameterProperties(component, maintainedParam, request.Name, request.Nickname, request.Description, request.Access, request.Optional);
                        if (propertyError != null) {
                            tcs.SetResult(Error(propertyError));
                            return;
                        }
                    }

                    RefreshComponent(component);
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = component.InstanceGuid.ToString(), index = request.Index }));
                } catch (Exception ex) {
                    tcs.SetResult(Error(ex.Message));
                }
            }));

            return tcs.Task;
        }

        private Task<string> RemoveRhinoComponentParameter(ComponentParameterRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(Error("No active Grasshopper document"));
                        return;
                    }

                    if (!TryFindComponent(doc, request.NodeId, out var component, out var error)) {
                        tcs.SetResult(Error(error));
                        return;
                    }

                    var side = ToParameterSide(request.Side);
                    var parameters = GetParameterList(component, side);
                    int index = request.Index.Value;
                    if (index < 0 || index >= parameters.Count) {
                        tcs.SetResult(Error("Parameter index is outside the valid range"));
                        return;
                    }

                    var variable = component as IGH_VariableParameterComponent;
                    if (variable == null) {
                        tcs.SetResult(Error("Component type does not support variable parameters"));
                        return;
                    }

                    if (!variable.CanRemoveParameter(side, index)) {
                        tcs.SetResult(Error($"Component rejected removing the {request.Side} parameter at index {index}"));
                        return;
                    }

                    var param = parameters[index];
                    if (!variable.DestroyParameter(side, index)) {
                        tcs.SetResult(Error($"Component rejected destroying the {request.Side} parameter at index {index}"));
                        return;
                    }

                    bool unregistered = side == GH_ParameterSide.Input
                        ? component.Params.UnregisterInputParameter(param, true)
                        : component.Params.UnregisterOutputParameter(param, true);

                    if (!unregistered) {
                        tcs.SetResult(Error("Failed to unregister component parameter"));
                        return;
                    }

                    variable.VariableParameterMaintenance();
                    RefreshComponent(component);
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success", id = component.InstanceGuid.ToString(), index = index }));
                } catch (Exception ex) {
                    tcs.SetResult(Error(ex.Message));
                }
            }));

            return tcs.Task;
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

                                    // 1. Try generic reflection for properties (e.g., CurrentValue, Value, Text)
                                    var prop = obj.GetType().GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);
                                    if (prop != null)
                                    {
                                        if (prop.CanWrite)
                                        {
                                            try {
                                                var targetType = prop.PropertyType;
                                                object convertedValue = (targetType == typeof(string)) ? kvp.Value.ToString() :
                                                                       (targetType == typeof(decimal)) ? Convert.ToDecimal(kvp.Value) :
                                                                       Convert.ChangeType(kvp.Value, targetType);

                                                prop.SetValue(obj, convertedValue);
                                                propertySet = true;
                                                modified = true;
                                            } catch (Exception ex) {
                                                errors.Add($"Failed to set property '{kvp.Key}': {ex.Message}");
                                            }
                                        }
                                        else
                                        {
                                            errors.Add($"Property '{kvp.Key}' is read-only.");
                                        }
                                    }

                                    // 2. Try Fields
                                    if (!propertySet)
                                    {
                                        var field = obj.GetType().GetField(kvp.Key, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy) ??
                                                    obj.GetType().GetField("m_" + kvp.Key, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

                                        if (field != null)
                                        {
                                            try {
                                                var targetType = field.FieldType;
                                                object convertedValue = (targetType == typeof(string)) ? kvp.Value.ToString() :
                                                                       (targetType == typeof(decimal)) ? Convert.ToDecimal(kvp.Value) :
                                                                       Convert.ChangeType(kvp.Value, targetType);
                                                field.SetValue(obj, convertedValue);
                                                propertySet = true;
                                                modified = true;
                                            } catch (Exception ex) {
                                                errors.Add($"Failed to set field '{kvp.Key}': {ex.Message}");
                                            }
                                        }
                                    }

                                    // 3. Fallback to IGH_Component inputs
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

        internal static Models.ParameterInfo CreateParameterInfo(IGH_Param parameter, int index)
        {
            return new Models.ParameterInfo {
                Name = parameter.Name,
                Nickname = parameter.NickName,
                Index = index,
                Description = parameter.Description,
                Access = parameter.Access.ToString().ToLowerInvariant(),
                Optional = parameter.Optional,
                Type = parameter.GetType().Name
            };
        }

        internal static bool IsValidIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!(char.IsLetter(value[0]) || value[0] == '_'))
            {
                return false;
            }

            for (int i = 1; i < value.Length; i++)
            {
                if (!(char.IsLetterOrDigit(value[i]) || value[i] == '_'))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool TryParseAccess(string access, out GH_ParamAccess paramAccess)
        {
            paramAccess = GH_ParamAccess.item;
            if (string.IsNullOrWhiteSpace(access))
            {
                return true;
            }

            switch (access.Trim().ToLowerInvariant())
            {
                case "item":
                    paramAccess = GH_ParamAccess.item;
                    return true;
                case "list":
                    paramAccess = GH_ParamAccess.list;
                    return true;
                case "tree":
                    paramAccess = GH_ParamAccess.tree;
                    return true;
                default:
                    return false;
            }
        }

        private static string ApplyParameterProperties(IGH_Component component, IGH_Param param, string name, string nickname, string description, string access, bool? optional)
        {
            var variableName = !string.IsNullOrWhiteSpace(nickname) ? nickname : name;
            if (ScriptInjector.GetComponentCode(component) != null && !string.IsNullOrWhiteSpace(variableName) && !IsValidIdentifier(variableName))
            {
                return $"Script parameter name '{variableName}' is not a valid C# identifier";
            }

            if (!TryParseAccess(access, out var paramAccess))
            {
                return $"Parameter access '{access}' is invalid. Use item, list, or tree.";
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                param.Name = name;
            }

            if (!string.IsNullOrWhiteSpace(nickname))
            {
                param.NickName = nickname;
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                param.NickName = name;
            }

            if (description != null)
            {
                param.Description = description;
            }

            if (!string.IsNullOrWhiteSpace(access))
            {
                param.Access = paramAccess;
            }

            if (optional.HasValue)
            {
                param.Optional = optional.Value;
            }

            return null;
        }

        private static void RefreshComponent(IGH_Component component)
        {
            component.Params.OnParametersChanged();
            component.Attributes?.ExpireLayout();
            component.ExpireSolution(true);
        }

        private static IList<IGH_Param> GetParameterList(IGH_Component component, GH_ParameterSide side)
        {
            return side == GH_ParameterSide.Input ? component.Params.Input : component.Params.Output;
        }

        private static GH_ParameterSide ToParameterSide(string side)
        {
            return string.Equals(side, "output", StringComparison.OrdinalIgnoreCase)
                ? GH_ParameterSide.Output
                : GH_ParameterSide.Input;
        }

        private static string ValidateParameterMutationRequest(ComponentParameterRequest request, bool requireIndex)
        {
            if (request == null)
            {
                return "Parameter request body is required";
            }

            if (string.IsNullOrWhiteSpace(request.NodeId))
            {
                return "Node ID is required";
            }

            var sideError = ValidateParameterSide(request.Side);
            if (sideError != null)
            {
                return sideError;
            }

            if (requireIndex && !request.Index.HasValue)
            {
                return "Parameter index is required";
            }

            if (request.Index.HasValue && request.Index.Value < 0)
            {
                return "Parameter index must be greater than or equal to zero";
            }

            return null;
        }

        private static string ValidateParameterSide(string side)
        {
            if (string.IsNullOrWhiteSpace(side))
            {
                return "Parameter side is required";
            }

            if (!string.Equals(side, "input", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(side, "output", StringComparison.OrdinalIgnoreCase))
            {
                return "Parameter side must be 'input' or 'output'";
            }

            return null;
        }

        private static bool TryFindComponent(Grasshopper.Kernel.GH_Document doc, string nodeId, out IGH_Component component, out string error)
        {
            component = null;
            error = null;

            if (!Guid.TryParse(nodeId, out Guid guid)) {
                error = "Invalid node ID format";
                return false;
            }

            var obj = doc.FindObject(guid, true);
            if (obj == null) {
                error = "Node not found";
                return false;
            }

            component = obj as IGH_Component;
            if (component == null) {
                error = "Node is not a component";
                return false;
            }

            return true;
        }

        private static string Error(string message)
        {
            return JsonConvert.SerializeObject(new { status = "error", message = message });
        }
    }
}
