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

        public RequestDispatcher(CanvasManager canvasManager)
        {
            _canvasManager = canvasManager;
        }

        public async Task HandleRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                string path = request.Url.AbsolutePath.ToLower().TrimEnd('/');
                string method = request.HttpMethod.ToUpper();
                string responseBody = "";

                if (method == "POST")
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string body = await reader.ReadToEndAsync();

                        switch (path)
                        {
                            case "/create":
                                var createReq = JsonConvert.DeserializeObject<CreateNodeRequest>(body);
                                responseBody = await _canvasManager.CreateNode(createReq);
                                break;
                            case "/code":
                                var codeReq = JsonConvert.DeserializeObject<UpdateCodeRequest>(body);
                                responseBody = await _canvasManager.UpdateCode(codeReq);
                                break;
                            case "/connection":
                                var connReq = JsonConvert.DeserializeObject<ConnectionRequest>(body);
                                responseBody = await _canvasManager.ConnectNodes(connReq);
                                break;
                            default:
                                response.StatusCode = (int)HttpStatusCode.NotFound;
                                break;
                        }
                    }
                }
                else if (method == "GET" && path == "/state")
                {
                    responseBody = await _canvasManager.GetCanvasState();
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "application/json";
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
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
