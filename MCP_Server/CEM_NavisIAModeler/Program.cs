using System;
using CEM_NavisIAModeler.Infrastructure;
using CEM_NavisIAModeler_Shared; // LogHelper

namespace CEM_NavisIAModeler
{
    /// <summary>
    /// Entry point for hosting the MCP server inside the Navisworks Add-in process.
    /// Provides lifecycle methods to start and stop the <see cref="RpcHost"/>.
    /// </summary>
    public static class Program
    {
        private static RpcHost _host;

        /// <summary>
        /// Starts the MCP server by creating and initializing a <see cref="RpcHost"/>.
        /// </summary>
        public static void Start()
        {
            LogHelper.LogInfo("MCPServer starting...", "[CEM_NavisIAModeler/Programm]");
            _host = new RpcHost();
            _host.Start();
            LogHelper.LogInfo("MCPServer started.", "[CEM_NavisIAModeler/Programm]");
        }

        /// <summary>
        /// Stops the MCP server and releases resources.
        /// </summary>
        public static void Stop()
        {
            try
            {
                _host?.Dispose();
                LogHelper.LogInfo("MCPServer stopped.", "[CEM_NavisIAModeler/Programm]");
            }
            catch (Exception ex)
            {
                LogHelper.LogError("Error while stopping MCPServer: " + ex, "[CEM_NavisIAModeler/Programm]");
            }
        }
    }
}
