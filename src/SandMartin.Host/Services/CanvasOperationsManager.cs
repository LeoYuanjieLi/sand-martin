using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino;
using Rhino.FileIO;
using Rhino.Geometry;
using SandMartin.Host.Components;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    /// <summary>
    /// Host-only canvas lifecycle operations used by external automation.
    /// These operations are intentionally not exposed by the MCP bridge.
    /// </summary>
    public class CanvasOperationsManager : GrasshopperServiceBase
    {
        private const string OutputNickname = "OUTPUT";
        private const int OutputIndex = 0;

        public virtual Task<string> ResetCanvas()
        {
            try
            {
                if (IsRunningInRhino())
                {
                    return ResetRhinoCanvas();
                }
            }
            catch
            {
            }

            return Task.FromResult(Error("No active Grasshopper document"));
        }

        public virtual Task<string> ExportOutput(ExportOutputRequest request)
        {
            var validationError = ValidateExportRequest(request, out var fullPath);
            if (validationError != null)
            {
                return Task.FromResult(Error(validationError));
            }

            try
            {
                if (IsRunningInRhino())
                {
                    return ExportRhinoOutput(fullPath, request.Overwrite);
                }
            }
            catch
            {
            }

            return Task.FromResult(Error("No active Grasshopper document"));
        }

        internal static string ValidateExportRequest(ExportOutputRequest request, out string fullPath)
        {
            fullPath = null;
            if (request == null)
            {
                return "Export request body is required";
            }

            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return "Export path is required";
            }

            try
            {
                fullPath = System.IO.Path.GetFullPath(request.Path);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException)
            {
                return "Export path is invalid";
            }

            var extension = System.IO.Path.GetExtension(fullPath);
            if (!extension.Equals(".step", StringComparison.OrdinalIgnoreCase) &&
                !extension.Equals(".stp", StringComparison.OrdinalIgnoreCase))
            {
                return "Export path must end in .step or .stp";
            }

            if (File.Exists(fullPath) && !request.Overwrite)
            {
                return "Export path already exists; set overwrite to true to replace it";
            }

            return null;
        }

        private static Task<string> ResetRhinoCanvas()
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var document = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (document == null)
                    {
                        tcs.SetResult(Error("No active Grasshopper document"));
                        return;
                    }

                    var protectedIds = FindServerInfrastructure(document);
                    var toRemove = document.Objects
                        .Where(obj => !protectedIds.Contains(obj.InstanceGuid))
                        .ToList();

                    document.RemoveObjects(toRemove, false);
                    document.NewSolution(true, GH_SolutionMode.Silent);

                    tcs.SetResult(JsonConvert.SerializeObject(new
                    {
                        status = "ok",
                        removed = toRemove.Count,
                        preserved = protectedIds.Count
                    }));
                }
                catch (Exception ex)
                {
                    tcs.SetResult(Error(ex.Message));
                }
            }));

            return tcs.Task;
        }

        private static Task<string> ExportRhinoOutput(string fullPath, bool overwrite)
        {
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                try
                {
                    var document = Grasshopper.Instances.ActiveCanvas?.Document;
                    if (document == null)
                    {
                        tcs.SetResult(Error("No active Grasshopper document"));
                        return;
                    }

                    document.NewSolution(true, GH_SolutionMode.Silent);

                    var matches = document.Objects
                        .OfType<IGH_Component>()
                        .Where(component => string.Equals(component.NickName, OutputNickname, StringComparison.Ordinal))
                        .ToList();

                    if (matches.Count == 0)
                    {
                        tcs.SetResult(Error($"No component nicknamed '{OutputNickname}' was found"));
                        return;
                    }

                    if (matches.Count > 1)
                    {
                        tcs.SetResult(Error($"Expected one component nicknamed '{OutputNickname}', found {matches.Count}"));
                        return;
                    }

                    var component = matches[0];
                    if (component.Params.Output.Count <= OutputIndex)
                    {
                        tcs.SetResult(Error($"Component '{OutputNickname}' has no output at index {OutputIndex}"));
                        return;
                    }

                    var values = component.Params.Output[OutputIndex]
                        .VolatileData
                        .AllData(true)
                        .ToList();

                    if (values.Count != 1)
                    {
                        tcs.SetResult(Error($"Expected one item at '{OutputNickname}' output {OutputIndex}, found {values.Count}"));
                        return;
                    }

                    if (!TryGetClosedSolid(values[0], out var brep, out var geometryError))
                    {
                        tcs.SetResult(Error(geometryError));
                        return;
                    }

                    var directory = System.IO.Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (File.Exists(fullPath) && !overwrite)
                    {
                        tcs.SetResult(Error("Export path already exists; set overwrite to true to replace it"));
                        return;
                    }

                    var tempPath = System.IO.Path.Combine(
                        directory ?? string.Empty,
                        $".{System.IO.Path.GetFileNameWithoutExtension(fullPath)}.{Guid.NewGuid():N}.step");

                    try
                    {
                        using (var rhinoDocument = RhinoDoc.CreateHeadless(null))
                        {
                            if (rhinoDocument == null)
                            {
                                tcs.SetResult(Error("Could not create a temporary Rhino document"));
                                return;
                            }

                            var objectId = rhinoDocument.Objects.AddBrep(brep);
                            if (objectId == Guid.Empty)
                            {
                                tcs.SetResult(Error("Could not add the output solid to the temporary Rhino document"));
                                return;
                            }

                            var options = new FileWriteOptions
                            {
                                SuppressDialogBoxes = true,
                                SuppressAllInput = true,
                                WriteGeometryOnly = true,
                                UpdateDocumentPath = false
                            };

                            if (!rhinoDocument.WriteFile(tempPath, options))
                            {
                                tcs.SetResult(Error("Rhino failed to export the output solid as STEP"));
                                return;
                            }
                        }

                        File.Move(tempPath, fullPath, overwrite);
                    }
                    finally
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }

                    tcs.SetResult(JsonConvert.SerializeObject(new
                    {
                        status = "ok",
                        path = fullPath,
                        componentId = component.InstanceGuid.ToString(),
                        nickname = OutputNickname,
                        outputIndex = OutputIndex
                    }));
                }
                catch (Exception ex)
                {
                    tcs.SetResult(Error(ex.Message));
                }
            }));

            return tcs.Task;
        }

        private static HashSet<Guid> FindServerInfrastructure(GH_Document document)
        {
            var protectedIds = new HashSet<Guid>();
            foreach (var server in document.Objects.OfType<SandMartinServerComponent>())
            {
                ProtectObjectAndSources(server, protectedIds);
            }

            return protectedIds;
        }

        private static void ProtectObjectAndSources(IGH_DocumentObject documentObject, ISet<Guid> protectedIds)
        {
            if (documentObject == null || !protectedIds.Add(documentObject.InstanceGuid))
            {
                return;
            }

            if (!(documentObject is IGH_Component component))
            {
                return;
            }

            foreach (var input in component.Params.Input)
            {
                foreach (var source in input.Sources)
                {
                    ProtectObjectAndSources(source.Attributes?.GetTopLevel?.DocObject, protectedIds);
                }
            }
        }

        private static bool TryGetClosedSolid(IGH_Goo value, out Brep brep, out string error)
        {
            brep = null;
            error = null;

            Brep castBrep = null;
            if (value != null && value.CastTo(out castBrep) && castBrep != null)
            {
                brep = castBrep.DuplicateBrep();
            }
            else
            {
                Extrusion extrusion = null;
                if (value != null && value.CastTo(out extrusion) && extrusion != null)
                {
                    brep = extrusion.ToBrep();
                }
                else
                {
                    Surface surface = null;
                    if (value != null && value.CastTo(out surface) && surface != null)
                    {
                        brep = surface.ToBrep();
                    }
                }
            }

            if (brep == null)
            {
                error = $"Item at '{OutputNickname}' output {OutputIndex} is not Brep-compatible geometry";
                return false;
            }

            if (!brep.IsValid)
            {
                error = $"Item at '{OutputNickname}' output {OutputIndex} is invalid geometry";
                return false;
            }

            if (!brep.IsSolid)
            {
                error = $"Item at '{OutputNickname}' output {OutputIndex} is not a closed solid";
                return false;
            }

            return true;
        }

        private static string Error(string message)
        {
            return JsonConvert.SerializeObject(new { status = "error", message });
        }
    }
}
