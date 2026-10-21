// Cemengal: take_screenshot (docs/decisions/0010). Runs as a job like every request (0008).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Services.Backends;
using static CEM_NavisIAModeler.Infrastructure.ErrorHandlingMiddleware;

namespace CEM_NavisIAModeler.Controllers
{
    public sealed class ViewController
    {
        private static readonly JavaScriptSerializer _jss = new JavaScriptSerializer();

        /// <summary>RPC method "take_screenshot": params mode ("view" | "window") and max_size (pixels, longest side).</summary>
        public object TakeScreenshot(RpcRequest req)
        {
            return Wrap(() =>
            {
                var p = _jss.ConvertToType<Dictionary<string, object>>(req.@params ?? new object()) ?? new Dictionary<string, object>();
                var mode = p.TryGetValue("mode", out var m) ? m as string : null;
                var side = 0;
                if (p.TryGetValue("max_size", out var s) && s != null)
                {
                    try { side = Convert.ToInt32(s, CultureInfo.InvariantCulture); }
                    catch (Exception) { throw new ArgumentException("max_size must be a number of pixels."); }
                }

                var dto = UiThread.InvokeAsync(() => ScreenCapture.Capture(mode, side)).GetAwaiter().GetResult();
                return RpcResponse<ScreenshotDto>.Success(dto);
            });
        }
    }
}
