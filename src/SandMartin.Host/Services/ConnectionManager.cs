using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Newtonsoft.Json;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class ConnectionManager : GrasshopperServiceBase
    {
        public virtual Task<string> CreateConnection(ConnectionRequest request)
        {
            try {
                if (IsRunningInRhino())
                {
                    return CreateRhinoConnection(request);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> CreateRhinoConnection(ConnectionRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
                        return;
                    }

                    if (!Guid.TryParse(request.SourceId, out Guid sourceId) || !Guid.TryParse(request.TargetId, out Guid targetId)) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Invalid node ID format" }));
                        return;
                    }

                    var sourceObj = doc.FindObject(sourceId, true);
                    var targetObj = doc.FindObject(targetId, true);

                    if (sourceObj == null || targetObj == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Source or target node not found" }));
                        return;
                    }

                    IGH_Param sourceParam = null;
                    if (sourceObj is IGH_Component sourceComp) {
                        if (request.SourceOutputIndex >= 0 && request.SourceOutputIndex < sourceComp.Params.Output.Count) {
                            sourceParam = sourceComp.Params.Output[request.SourceOutputIndex];
                        }
                    } else if (sourceObj is IGH_Param p) {
                        sourceParam = p;
                    }

                    IGH_Param targetParam = null;
                    if (targetObj is IGH_Component targetComp) {
                        if (request.TargetInputIndex >= 0 && request.TargetInputIndex < targetComp.Params.Input.Count) {
                            targetParam = targetComp.Params.Input[request.TargetInputIndex];
                        }
                    } else if (targetObj is IGH_Param p) {
                        targetParam = p;
                    }

                    if (sourceParam == null || targetParam == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Source output or target input parameter not found" }));
                        return;
                    }

                    targetParam.AddSource(sourceParam);
                    targetParam.ExpireSolution(true);

                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "success" }));

                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
                }
            }));

            return tcs.Task;
        }

        public virtual Task<string> DisconnectNode(DisconnectRequest request)
        {
            try {
                if (IsRunningInRhino())
                {
                    return DisconnectRhinoNode(request);
                }
            } catch {
            }

            return Task.FromResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
        }

        private Task<string> DisconnectRhinoNode(DisconnectRequest request)
        {
            var tcs = new TaskCompletionSource<string>();

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => {
                try {
                    var doc = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (doc == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No active Grasshopper document" }));
                        return;
                    }

                    if (!Guid.TryParse(request.SourceId, out Guid sourceId) || !Guid.TryParse(request.TargetId, out Guid targetId)) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Invalid node ID format" }));
                        return;
                    }

                    var sourceObj = doc.FindObject(sourceId, true);
                    var targetObj = doc.FindObject(targetId, true);

                    if (sourceObj == null || targetObj == null) {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "Source or target node not found" }));
                        return;
                    }

                    List<IGH_Param> targetInputs = new List<IGH_Param>();
                    if (targetObj is IGH_Component targetComp) {
                        targetInputs.AddRange(targetComp.Params.Input);
                    } else if (targetObj is IGH_Param p) {
                        targetInputs.Add(p);
                    }

                    List<IGH_Param> sourceOutputs = new List<IGH_Param>();
                    if (sourceObj is IGH_Component sourceComp) {
                        sourceOutputs.AddRange(sourceComp.Params.Output);
                    } else if (sourceObj is IGH_Param p) {
                        sourceOutputs.Add(p);
                    }

                    bool disconnected = false;
                    foreach (var input in targetInputs) {
                        foreach (var output in sourceOutputs) {
                            if (input.Sources.Contains(output)) {
                                input.RemoveSource(output);
                                disconnected = true;
                            }
                        }
                    }

                    if (disconnected) {
                        doc.NewSolution(true);
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "success" }));
                    } else {
                        tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = "No connection found between the specified nodes" }));
                    }

                } catch (Exception ex) {
                    tcs.SetResult(JsonConvert.SerializeObject(new { status = "error", message = ex.Message }));
                }
            }));

            return tcs.Task;
        }
    }
}
