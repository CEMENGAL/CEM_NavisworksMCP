// Cemengal: the entry point a host ribbon uses to run the AI Modeler server (docs/decisions/0006).
// CEM_NavisworksAPI's CEM_RibbonUI calls it from its "AI Modeler" button, as CEM_RevitAPI's ribbon
// calls CEM_RevitMCP's MCPServiceConnection. It shows no dialog: the host reflects the state in the
// button (pressed while running, the port in its label).
using System;
using System.Threading.Tasks;
using CEM_NavisIAModeler.Plugins;
using CEM_NavisIAModeler_Shared;

namespace CEM_NavisIAModeler.Core
{
    public static class MCPServiceConnection
    {
        /// <summary>The port used when none is saved; the Node client's default too.</summary>
        public const int DefaultPort = 8765;

        private static MCPServerService Service =>
            ServiceRegistry.GetService("CEM_NavisIAModeler") as MCPServerService;

        /// <summary>True while the HTTP/RPC server is listening.</summary>
        public static bool IsRunning => Service?.IsServerRunning == true;

        /// <summary>The port the server listens on (or will, when started).</summary>
        public static int Port => Service?.CurrentPort ?? DefaultPort;

        /// <summary>
        /// Starts the server if it is stopped, stops it if it is running. Call it on Navisworks' UI
        /// thread (a ribbon command is): the first call creates the service there, which captures the
        /// UI thread for the backends. Never throws; the task's result is the new running state.
        /// </summary>
        public static Task<bool> ToggleAsync()
        {
            try
            {
                var service = Service;
                if (service == null)
                {
                    // The service starts itself when the saved settings say it was running last time.
                    service = new MCPServerService();
                    ServiceRegistry.Register(service);
                    if (service.IsServerRunning || service.GetServerEnabledFromSettings())
                        return Task.FromResult(service.IsServerRunning);
                }

                return service.IsServerRunning ? StopAsync(service) : StartAsync(service);
            }
            catch (Exception ex)
            {
                LogHelper.LogEvent($"[MCPServiceConnection] Toggle failed: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private static async Task<bool> StartAsync(MCPServerService service)
        {
            var port = service.GetPortFromSettings();
            return await service.StartServerAsync(port > 0 ? port : DefaultPort).ConfigureAwait(false);
        }

        private static async Task<bool> StopAsync(MCPServerService service)
        {
            await service.StopServerAsync().ConfigureAwait(false);
            return false;
        }
    }
}
