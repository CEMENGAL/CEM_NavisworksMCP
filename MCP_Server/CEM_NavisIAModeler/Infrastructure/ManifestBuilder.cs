// CEM_NavisIAModeler/Infrastructure/ManifestBuilder.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Mapping;
using CEM_NavisIAModeler_Shared;

namespace CEM_NavisIAModeler.Infrastructure
{
    /// <summary>
    /// Builds an MCP manifest at runtime from the currently registered RPC routes.
    /// - Used by the MCP server to expose available routes and capabilities.
    /// - Can also write the manifest to disk as mcp_manifest.json inside the bundle directory.
    ///
    /// Callers:
    /// - MCPServer (HTTP GET /manifest and /mcp_manifest.json)
    /// - MCPServerRegistrar (optionally writes manifest at startup)
    /// </summary>
    public static class ManifestBuilder
    {
        private static readonly JavaScriptSerializer _ser = new JavaScriptSerializer();

        /// <summary>
        /// Builds the manifest document from the current RPC routes.
        /// - baseUrl: the base URL of the running server (e.g. "http://127.0.0.1:1234/").
        /// - Includes metadata, endpoints, routes, and minimal capabilities.
        /// </summary>
        /// <param name="baseUrl">The base URL of the server.</param>
        /// <returns>A populated ManifestDoc object.</returns>
        public static ManifestDoc Build(string baseUrl)
        {
            // Collect available routes
            var routes = RpcMap.BuildRoutes().Keys
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .Select(k => new ManifestRoute { name = k })
                .ToList();

            // Minimal server capabilities
            var capabilities = new Dictionary<string, object>
            {
                ["transport"] = "http",
                ["rpc_path"] = "/rpc",
                ["health"] = "/health"
            };

            // Construct manifest
            var doc = new ManifestDoc
            {
                name = "CEM_NavisIAModeler",
                version = "1.0.0",
                generated_at_utc = DateTime.UtcNow,
                base_url = NormalizeBaseUrl(baseUrl),
                endpoints = new ManifestEndpoints
                {
                    rpc = Combine(NormalizeBaseUrl(baseUrl), "rpc"),
                    health = Combine(NormalizeBaseUrl(baseUrl), "health"),
                    manifest = Combine(NormalizeBaseUrl(baseUrl), "manifest")
                },
                routes = routes,
                capabilities = capabilities
            };

            return doc;
        }

        /// <summary>
        /// Writes the manifest document to disk as "mcp_manifest.json".
        /// - Target path: ApplicationPlugins\CEM_NavisIAModeler.bundle\Contents\v24 (Navisworks 2027; Cemengal port).
        /// - Returns true on success and provides the full file path.
        /// </summary>
        /// <param name="doc">ManifestDoc to write.</param>
        /// <param name="fullPath">Output parameter with the written file path.</param>
        /// <returns>True if file was written successfully, false otherwise.</returns>
        public static bool TryWriteToDisk(ManifestDoc doc, out string fullPath)
        {
            try
            {
                var contentDir = GetDefaultContentDirectory();
                Directory.CreateDirectory(contentDir);
                fullPath = Path.Combine(contentDir, "mcp_manifest.json");
                var json = _ser.Serialize(doc);
                File.WriteAllText(fullPath, json);
                LogHelper.LogEvent($"[Manifest] mcp_manifest.json written: {fullPath}");
                return true;
            }
            catch (Exception ex)
            {
                LogHelper.LogEvent($"[Manifest] Write failed: {ex.Message}");
                fullPath = null;
                return false;
            }
        }

        /// <summary>
        /// Gets the default content directory in the ApplicationPlugins bundle.
        /// - Default path points to CEM_NavisIAModeler.bundle\Contents\v24 (Navisworks 2027; Cemengal port).
        /// - Note: Update the version (v24 = Navisworks 2027) to match the target Navisworks version if needed.
        /// </summary>
        public static string GetDefaultContentDirectory()
        {
            // Cemengal: the folder of this DLL, so the manifest lands in whichever bundle loaded it
            // (CEM_NavisworksAPI.bundle when CEM_RibbonUI hosts it, CEM_NavisIAModeler.bundle standalone).
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        // --- Private Helpers ---

        private static string NormalizeBaseUrl(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) return string.Empty;
            return baseUrl.EndsWith("/") ? baseUrl : baseUrl + "/";
        }

        private static string Combine(string baseUrl, string rel)
        {
            if (string.IsNullOrEmpty(baseUrl)) return "/" + rel.TrimStart('/');
            return baseUrl.TrimEnd('/') + "/" + rel.TrimStart('/');
        }
    }

    // ===== Manifest Data Models =====

    /// <summary>
    /// Root object of the MCP manifest.
    /// - Contains metadata, endpoints, routes, and capabilities.
    /// </summary>
    public sealed class ManifestDoc
    {
        public string name { get; set; }
        public string version { get; set; }
        public string base_url { get; set; }
        public DateTime generated_at_utc { get; set; }
        public ManifestEndpoints endpoints { get; set; }
        public List<ManifestRoute> routes { get; set; }
        public Dictionary<string, object> capabilities { get; set; }
    }

    /// <summary>
    /// Overview of main service endpoints.
    /// - rpc: path for RPC calls.
    /// - health: path for health checks.
    /// - manifest: path for fetching the manifest itself.
    /// </summary>
    public sealed class ManifestEndpoints
    {
        public string rpc { get; set; }
        public string health { get; set; }
        public string manifest { get; set; }
    }

    /// <summary>
    /// Entry for an RPC route inside the manifest.
    /// - Contains the route name (RPC method).
    /// - Can be extended with metadata (e.g., summary, parameters).
    /// </summary>
    public sealed class ManifestRoute
    {
        public string name { get; set; }
        // Extension point: public string summary { get; set; } etc.
    }
}
