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

                if (method == "GET" && path == "/state")
                {
                    responseBody = await _canvasManager.GetCanvasState();
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
                                if (!_allowCodeInjection && createReq.Parameters != null && createReq.Parameters.ContainsKey("Code"))
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
                                response.StatusCode = (int)HttpStatusCode.NotFound;
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
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                    }
                }
                else if (method == "DELETE")
                {
                    if (path.StartsWith("/node/"))
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
    }
}
