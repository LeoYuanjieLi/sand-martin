using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SandMartin.Host.Models;

namespace SandMartin.Host.Services
{
    public class RequestDispatcher
    {
        private readonly CanvasManager _canvasManager;
        private string _authToken;
        private bool _allowCodeInjection;

        public RequestDispatcher(CanvasManager canvasManager, string authToken, bool allowCodeInjection)
        {
            _canvasManager = canvasManager;
            _authToken = authToken;
            _allowCodeInjection = allowCodeInjection;
        }

        public void UpdateSecuritySettings(string token, bool allowCode)
        {
            if (!string.IsNullOrEmpty(token)) _authToken = token;
            _allowCodeInjection = allowCode;
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Security Check: Authentication
                string authHeader = request.Headers["Authorization"];
                if (string.IsNullOrEmpty(authHeader) || authHeader != $"Bearer {_authToken}")
                {
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    byte[] authBuffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = "Unauthorized: Missing or invalid token." }));
                    await response.OutputStream.WriteAsync(authBuffer, 0, authBuffer.Length);
                    return;
                }

                string path = request.Url.AbsolutePath.ToLower().TrimEnd('/');
                string method = request.HttpMethod.ToUpper();
                string responseBody = "";

                if (method == "GET")
                {
                    if (path == "/state")
                    {
                        responseBody = await _canvasManager.GetCanvasState();
                    }
                    else if (path.StartsWith("/node/"))
                    {
                        var nodeId = path.Substring("/node/".Length);
                        responseBody = await _canvasManager.GetNodeDetails(nodeId);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                else if (method == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();

                        switch (path)
                        {
                            case "/create":
                                var createReq = JsonConvert.DeserializeObject<CreateNodeRequest>(body);
                                if (!_allowCodeInjection && createReq?.Parameters != null && createReq.Parameters.ContainsKey("Code"))
                                {
                                    response.StatusCode = (int)HttpStatusCode.Forbidden;
                                    responseBody = JsonConvert.SerializeObject(new { error = "Code injection is disabled on the Sand Martin Server component." });
                                }
                                else
                                {
                                    responseBody = await _canvasManager.CreateNode(createReq);
                                }
                                break;
                            case "/connection":
                                var connReq = JsonConvert.DeserializeObject<ConnectionRequest>(body);
                                responseBody = await _canvasManager.CreateConnection(connReq);
                                break;
                            case "/disconnect":
                                var discReq = JsonConvert.DeserializeObject<DisconnectRequest>(body);
                                responseBody = await _canvasManager.DisconnectNode(discReq);
                                break;
                            default:
                                if (TryParseParameterRoute(path, out var route) && route.IsCollection)
                                {
                                    var paramReq = JsonConvert.DeserializeObject<ComponentParameterRequest>(body) ?? new ComponentParameterRequest();
                                    paramReq.NodeId = route.NodeId;
                                    responseBody = await _canvasManager.AddComponentParameter(paramReq);
                                }
                                else
                                {
                                    response.StatusCode = (int)HttpStatusCode.NotFound;
                                }
                                break;
                        }
                    }
                }
                else if (method == "PATCH")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();

                        if (path.StartsWith("/update/"))
                        {
                            var nodeId = path.Substring("/update/".Length);
                            var updateReq = JsonConvert.DeserializeObject<UpdateNodeRequest>(body) ?? new UpdateNodeRequest();
                            updateReq.NodeId = nodeId;

                            if (!_allowCodeInjection && updateReq.Parameters != null && updateReq.Parameters.ContainsKey("Code"))
                            {
                                response.StatusCode = (int)HttpStatusCode.Forbidden;
                                responseBody = JsonConvert.SerializeObject(new { error = "Code injection is disabled on the Sand Martin Server component." });
                            }
                            else
                            {
                                responseBody = await _canvasManager.UpdateNode(updateReq);
                            }
                        }
                        else if (TryParseParameterRoute(path, out var route) && !route.IsCollection)
                        {
                            var updateReq = JsonConvert.DeserializeObject<UpdateComponentParameterRequest>(body) ?? new UpdateComponentParameterRequest();
                            updateReq.NodeId = route.NodeId;
                            updateReq.Side = route.Side;
                            updateReq.Index = route.Index.Value;
                            responseBody = await _canvasManager.UpdateComponentParameter(updateReq);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                    }
                }
                else if (method == "DELETE")
                {
                    if (TryParseParameterRoute(path, out var route) && !route.IsCollection)
                    {
                        responseBody = await _canvasManager.RemoveComponentParameter(new ComponentParameterRequest {
                            NodeId = route.NodeId,
                            Side = route.Side,
                            Index = route.Index
                        });
                    }
                    else if (path.StartsWith("/node/"))
                    {
                        var nodeId = path.Substring("/node/".Length);
                        responseBody = await _canvasManager.DeleteNode(nodeId);
                    }
                    else
                    {
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    responseBody = JsonConvert.SerializeObject(new { error = "Endpoint not found or method not allowed." });
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                // Sanitize error message: only return the message, not the stack trace
                byte[] buffer = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { error = ex.Message }));
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        internal static bool TryParseParameterRoute(string path, out ParameterRoute route)
        {
            route = null;
            if (string.IsNullOrWhiteSpace(path))
            {
                return false;
            }

            var parts = path.Trim('/').Split('/');
            if (parts.Length == 3 &&
                parts[0] == "node" &&
                parts[2] == "parameter" &&
                !string.IsNullOrWhiteSpace(parts[1]))
            {
                route = new ParameterRoute(parts[1], null, null, true);
                return true;
            }

            if (parts.Length == 5 &&
                parts[0] == "node" &&
                parts[2] == "parameter" &&
                !string.IsNullOrWhiteSpace(parts[1]) &&
                !string.IsNullOrWhiteSpace(parts[3]) &&
                int.TryParse(parts[4], out var index))
            {
                route = new ParameterRoute(parts[1], parts[3], index, false);
                return true;
            }

            return false;
        }

        internal sealed class ParameterRoute
        {
            public ParameterRoute(string nodeId, string side, int? index, bool isCollection)
            {
                NodeId = nodeId;
                Side = side;
                Index = index;
                IsCollection = isCollection;
            }

            public string NodeId { get; }
            public string Side { get; }
            public int? Index { get; }
            public bool IsCollection { get; }
        }
    }
}
