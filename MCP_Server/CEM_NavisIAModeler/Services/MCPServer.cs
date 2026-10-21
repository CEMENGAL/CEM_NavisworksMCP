// CEM_NavisIAModeler/Services/MCPServer.cs
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;       
using CEM_NavisIAModeler.Infrastructure;  
using CEM_NavisIAModeler.Mapping;         
using CEM_NavisIAModeler_Shared;                     

namespace CEM_NavisIAModeler.Services
{
    /// <summary>
    /// Datei/File: Services/MCPServer.cs | Klasse/Class: MCPServer
    ///   HTTP adapter for JSON-RPC and helper routes (/health, /manifest).
    /// </summary> 
    public sealed class MCPServer : IDisposable
    {
        private readonly int _port;
        private readonly HttpListener _listener = new HttpListener();
        // Cemengal: no 2 MB reply limit (the serializer's default). A whole-model property distribution can
        // pass it after minutes of scanning, and the reply was then lost (docs/decisions/0008).
        private readonly JavaScriptSerializer _jss = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        private readonly RpcRouter _router;
        private CancellationTokenSource _cts;
        private Task _acceptLoopTask;

        // Cemengal: this start's access token; every request but the health check must carry it (docs/decisions/0009).
        private string _token;

        /// <summary>
        /// Fired when the server emits an informational message (e.g. startup, shutdown).
        /// </summary>
        public event EventHandler<string> ServerMessage;

        /// <summary>
        /// Fired when the server encounters an error.
        /// </summary>
        public event EventHandler<Exception> ServerError;

        /// <summary>
        /// Gets whether the server is currently running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// The port number the server is configured to listen on.
        /// </summary>
        public int Port => _port;

        /// <summary>
        /// Returns the base server URL (127.0.0.1:{port}).
        /// </summary> 
        public string ServerUrl => $"http://127.0.0.1:{_port}/";

        /// <summary>
        /// Creates a new MCPServer instance for the given port.
        /// Initializes the RPC router and registers routes.
        /// </summary>
        public MCPServer(int port)
        {
            _port = port;

            _router = new RpcRouter();
            try
            {
                 
                RpcMap.Register(_router.Routes);
            }
            catch (Exception ex)
            {
                LogHelper.LogEvent($"[MCPServer] Warnung: RpcMap.Register konnte nicht aufgerufen werden: {ex.Message}");
            }
        }

        /// <summary>
        /// Starts the HTTP listener asynchronously on the configured port.
        /// </summary>
        /// <returns>
        /// True if the listener started successfully, false otherwise.
        /// </returns>
        public async Task<bool> StartAsync()
        {
            if (IsRunning) return true;

            try
            {
                var urlA = $"http://127.0.0.1:{_port}/";
                var urlB = $"http://localhost:{_port}/";

                _listener.Prefixes.Clear();
                _listener.Prefixes.Add(urlA);
                if (!string.Equals(urlA, urlB, StringComparison.OrdinalIgnoreCase))
                    _listener.Prefixes.Add(urlB);

                _listener.Start();

                // Cemengal: issued once the port is ours, so a start that fails to bind never replaces the
                // token of an AI Modeler already listening. No token file, no server (docs/decisions/0009).
                try
                {
                    _token = AccessToken.Issue();
                }
                catch
                {
                    _listener.Stop();
                    throw;
                }

                _cts = new CancellationTokenSource();
                _acceptLoopTask = Task.Run(() => AcceptLoopAsync(_cts.Token), _cts.Token);

                IsRunning = true;
                OnServerMessage($"Listening on: {urlA} (and localhost)");
                return true;
            }
            catch (HttpListenerException hex)
            {
                IsRunning = false;
                OnServerError(hex);
                OnServerMessage("Hinweis/Note: Prüfe URL-ACL (netsh) oder ob ein anderer Prozess den Port belegt.");
                return false;
            }
            catch (Exception ex)
            {
                IsRunning = false;
                OnServerError(ex);
                return false;
            }
        }

        /// <summary>
        /// Stops the HTTP listener and cancels the accept loop.
        /// </summary>
        public async Task StopAsync()
        {
            if (!IsRunning) return;

            // Cemengal: a job must not keep Navisworks busy after the AI Modeler stops (docs/decisions/0008).
            RpcJobs.CancelRunning("the AI Modeler was stopped");

            try
            {
                _cts?.Cancel();

                try { _listener.Stop(); } catch { /* ignore */ }

                if (_acceptLoopTask != null)
                {
                    try { await _acceptLoopTask.ConfigureAwait(false); } catch { /* ignore */ }
                }
            }
            finally
            {
                IsRunning = false;
                _cts?.Dispose();
                _cts = null;

                AccessToken.Revoke(_token); // Cemengal: docs/decisions/0009
                _token = null;
            }
        }

        /// <summary>
        /// Background loop that continuously accepts incoming HTTP requests until cancellation.
        /// </summary>
        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx = null;
                try
                {
                    ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                    _ = Task.Run(() => HandleRequestAsync(ctx), ct);
                }
                catch (ObjectDisposedException) { break; }
                catch (HttpListenerException)
                {
                    if (ct.IsCancellationRequested) break;
                     
                }
                catch (Exception ex)
                {
                    OnServerError(ex);
                }
            }
        }

        /// <summary>
        /// Handles a single incoming HTTP request.
        /// Supports helper endpoints (/health, /manifest, /mcp_manifest.json) and /rpc for JSON-RPC.
        /// </summary>
        private async Task HandleRequestAsync(HttpListenerContext ctx)
        {
             
            // Cemengal: no CORS headers. Upstream sent "Access-Control-Allow-Origin: *", which with no
            // authentication let any web page in the user's browser call /rpc. The MCP client is Node
            // and needs no CORS (docs/decisions/0005).

            try
            {
                var req = ctx.Request;
                var res = ctx.Response;
                var path = (req.Url?.AbsolutePath ?? "/").TrimEnd('/').ToLowerInvariant();
                var method = req.HttpMethod?.ToUpperInvariant();

                // Cemengal: browsers attach an Origin header to cross-origin requests, including the
                // "simple" POSTs that need no preflight; the Node client never sends one. Refuse them,
                // so a web page cannot trigger side effects it cannot read (docs/decisions/0005).
                if (!string.IsNullOrEmpty(req.Headers["Origin"]))
                {
                    res.StatusCode = 403;
                    res.Close();
                    return;
                }

                 
                if (method == "OPTIONS")
                {
                    res.StatusCode = 204;
                    res.Close();
                    return;
                }

                 
                if (method == "GET" && (path == "" || path == "/"))
                {
                    await WriteTextAsync(res, 200, "CEM Navisworks AI Modeler is running. Try POST /rpc").ConfigureAwait(false);
                    return;
                }

                 
                if (method == "GET" && path == "/health")
                {
                    await WriteTextAsync(res, 200, "OK").ConfigureAwait(false);
                    return;
                }

                // Cemengal: everything past the health check needs this start's token (docs/decisions/0009).
                if (!AccessToken.IsAuthorized(req.Headers["Authorization"], _token))
                {
                    await WriteJsonAsync(res, 401, new
                    {
                        ok = false,
                        error = new { code = "NVX_UNAUTHORIZED", msg = "Missing or wrong access token. The client reads it from the AI Modeler's token file in the user's Navisworks settings folder." }
                    }).ConfigureAwait(false);
                    return;
                }

                 
                if (method == "GET" && path == "/manifest")
                {
                    var baseUrl = GetBaseUrl(req);
                    var doc = ManifestBuilder.Build(baseUrl);

                    var json = _jss.Serialize(doc);
                    await WriteRawAsync(res, 200, "application/json; charset=utf-8", json).ConfigureAwait(false);
                    return;
                }

                 
                if (method == "GET" && path == "/mcp_manifest.json")
                {
                    var baseUrl = GetBaseUrl(req);
                    var doc = ManifestBuilder.Build(baseUrl);

                    var json = _jss.Serialize(doc);
                    await WriteRawAsync(res, 200, "application/json; charset=utf-8", json).ConfigureAwait(false);
                    return;
                }

                 
                if (method == "POST" && (path == "/rpc" || path == ""))
                {
                    string body;
                    using (var reader = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8))
                        body = await reader.ReadToEndAsync().ConfigureAwait(false);

                    if (string.IsNullOrWhiteSpace(body))
                    {
                        await WriteJsonAsync(res, 400, new { error = "empty body" }).ConfigureAwait(false);
                        return;
                    }

                    RpcRequest rpcReq;
                    try
                    {
                        rpcReq = _jss.Deserialize<RpcRequest>(body);
                    }
                    catch (Exception dex)
                    {
                        await WriteJsonAsync(res, 400, new { error = "invalid json", detail = dex.Message }).ConfigureAwait(false);
                        return;
                    }

                    object rpcResp;
                    try
                    {
                        rpcResp = _router.Dispatch(rpcReq);
                    }
                    catch (Exception ex)
                    {
                        OnServerError(ex);
                         
                        var fail = RpcResponse<object>.Fail("NVX_INTERNAL", ex.Message);
                         
                        rpcResp = fail;
                    }

                    var json = _jss.Serialize(rpcResp);
                    await WriteRawAsync(res, 200, "application/json; charset=utf-8", json).ConfigureAwait(false);
                    return;
                }

                 
                await WriteTextAsync(ctx.Response, 404, "Not Found").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                OnServerError(ex);
                try
                {
                    await WriteJsonAsync(ctx.Response, 500, new { error = ex.Message }).ConfigureAwait(false);
                }
                catch {   }
            }
        }

        /// <summary>
        /// Builds a base URL string from the given request (scheme, host, port).
        /// </summary>
        private static string GetBaseUrl(HttpListenerRequest req)
        {
            try
            {
                var scheme = req.Url?.Scheme ?? "http";
                var host = req.UserHostName ?? "127.0.0.1";
                var port = req.Url?.Port ?? 80;
                return $"{scheme}://{host}:{port}/";
            }
            catch
            {
                return "http://127.0.0.1/";
            }
        }

        /// <summary>
        /// Writes plain text to the HTTP response with the given status code.
        /// </summary>
        private static async Task WriteTextAsync(HttpListenerResponse res, int code, string text)
            => await WriteRawAsync(res, code, "text/plain; charset=utf-8", text);

        /// <summary>
        /// Serializes the given object as JSON and writes it to the HTTP response.
        /// </summary>
        private static async Task WriteJsonAsync(HttpListenerResponse res, int code, object obj)
        {
            var jss = new JavaScriptSerializer();
            var json = jss.Serialize(obj);
            await WriteRawAsync(res, code, "application/json; charset=utf-8", json);
        }

        /// <summary>
        /// Writes a raw string payload to the HTTP response with the specified content type.
        /// </summary>
        private static async Task WriteRawAsync(HttpListenerResponse res, int code, string contentType, string payload)
        {
            var bytes = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            res.StatusCode = code;
            res.ContentType = contentType;
            res.ContentEncoding = Encoding.UTF8;
            res.ContentLength64 = bytes.LongLength;
            using (var os = res.OutputStream)
            {
                await os.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Raises <see cref="ServerMessage"/> and logs the message.
        /// </summary>
        private void OnServerMessage(string msg)
        {
            try { ServerMessage?.Invoke(this, msg); } catch {   }
            LogHelper.LogEvent($"[MCP] {msg}");
        }

        /// <summary>
        /// Raises <see cref="ServerError"/> and logs the exception.
        /// </summary>
        private void OnServerError(Exception ex)
        {
            try { ServerError?.Invoke(this, ex); } catch {   }
            LogHelper.LogEvent($"[MCP-ERROR] {ex.GetType().Name}: {ex.Message}");
        }

        /// <summary>
        /// Cleans up server resources (listener, cancellation tokens).
        /// </summary>
        public void Dispose()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Close(); } catch { }
            try { _cts?.Dispose(); } catch { }
            AccessToken.Revoke(_token); // Cemengal: docs/decisions/0009
        }
    }
}
