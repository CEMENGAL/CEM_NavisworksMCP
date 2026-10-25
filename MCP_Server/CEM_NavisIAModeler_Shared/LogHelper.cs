using Autodesk.Navisworks.Api;
using CEM_CSharpExtended.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CEM_NavisIAModeler_Shared
{
    /// <summary>
    /// Provides logging functionality for WAABE Navisworks add-ins.
    /// - Writes one line per entry into today's log, <c>%TEMP%\Cemengal\NavisworksMCP\CEM_NavisIAModeler-yyyyMMdd.log</c>.
    /// - Supports different log levels (Info, Warning, Error, Success, Debug).
    /// - Adds startup and shutdown banners for projects.
    /// Cemengal: through CEM_CSharpExtended's CemTempFolder (root decision 0031, docs/decisions/0011), a local folder
    /// that prunes itself; never the DLL folder (the menu may live in a shared ACC folder), nor roaming AppData.
    /// </summary>
    public static class LogHelper
    {
        /// <summary>The log's folder under %TEMP%\Cemengal (root 0031's app name).</summary>
        private const string App = "NavisworksMCP";

        /// <summary>The log's name: <c>CEM_NavisIAModeler-yyyyMMdd.log</c>.</summary>
        private const string Name = "CEM_NavisIAModeler";

        /// <summary>
        /// Logs a message with automatic project detection based on the calling assembly.
        /// </summary> 
        public static void LogEvent(string msg)
        {
            LogEvent(msg, GetCallingProject());
        }

        /// <summary>
        /// Logs a message with an explicit project name.
        /// </summary> 
        public static void LogEvent(string msg, string projectName)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var projectPrefix = string.IsNullOrEmpty(projectName) ? "" : $"[{projectName}] ";
            var line = $"{timestamp} {projectPrefix}{msg}";

            // Cemengal: today's file, appended shared and never throwing (root 0031); was one Markdown table that grew forever.
            if (!CemTempFolder.AppendLog(App, Name, line))
            {
                HandleLogError(timestamp, msg, projectName);
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[WAABE-{projectName}] {timestamp}: {msg}");
        }

        /// <summary>
        /// Logs a message with a specified log level (Info, Warning, Error, Success, Debug).
        /// </summary> 
        public static void LogEvent(string msg, string projectName, LogLevel level)
        {
            string levelPrefix = "";
            switch (level)
            {
                case LogLevel.Info:
                    levelPrefix = "ℹ️";
                    break;
                case LogLevel.Warning:
                    levelPrefix = "⚠️";
                    break;
                case LogLevel.Error:
                    levelPrefix = "❌";
                    break;
                case LogLevel.Success:
                    levelPrefix = "✅";
                    break;
                case LogLevel.Debug:
                    levelPrefix = "🔍";
                    break;
                default:
                    levelPrefix = "";
                    break;
            }

            var formattedMsg = $"{levelPrefix} {msg}";
            LogEvent(formattedMsg, projectName);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary> 
        public static void LogInfo(string msg, string projectName = null)
            => LogEvent(msg, projectName ?? GetCallingProject(), LogLevel.Info);

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        public static void LogWarning(string msg, string projectName = null)
            => LogEvent(msg, projectName ?? GetCallingProject(), LogLevel.Warning);

        /// <summary>
        /// Logs an error message.
        /// </summary>
        public static void LogError(string msg, string projectName = null)
            => LogEvent(msg, projectName ?? GetCallingProject(), LogLevel.Error);

        /// <summary>
        /// Logs a success message.
        /// </summary>
        public static void LogSuccess(string msg, string projectName = null)
            => LogEvent(msg, projectName ?? GetCallingProject(), LogLevel.Success);

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        public static void LogDebug(string msg, string projectName = null)
            => LogEvent(msg, projectName ?? GetCallingProject(), LogLevel.Debug);

        /// <summary>
        /// Writes a startup banner for a project, including version and DLL path.
        /// </summary> 
        public static void LogProjectStartup(string projectName, string version = "1.0.0")
        {
            LogEvent("", "");  
            LogEvent($"=== {projectName.ToUpper()} GESTARTET ===", projectName, LogLevel.Success);
            LogEvent($"Version: {version}", projectName);
            LogEvent($"DLL-Pfad: {Assembly.GetCallingAssembly().Location}", projectName);
            LogEvent($"Gestartet um: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", projectName);
        }

        /// <summary>
        /// Writes a shutdown banner for a project.
        /// </summary> 
        public static void LogProjectShutdown(string projectName)
        {
            LogEvent($"=== {projectName.ToUpper()} BEENDET ===", projectName, LogLevel.Info);
            LogEvent($"Beendet um: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", projectName);
            LogEvent("", "");  
        }


        /// <summary>
        /// Gets the default project name from the calling assembly (e.g., MCP, CHAT, SERVER).
        /// </summary>
        private static string GetCallingProject()
        {
            try
            {
                var callingAssembly = Assembly.GetCallingAssembly();
                var assemblyName = callingAssembly.GetName().Name;

                 
                switch (assemblyName)
                {
                    case "CEM_NavisIAModeler_Ribbon":
                        return "MCP";
                    case "waabe_navi_chatfenster":
                        return "CHAT";
                    case "CEM_NavisIAModeler":
                        return "SERVER";
                    case "CEM_NavisIAModeler_Shared":
                        return "SHARED";
                    default:
                        return assemblyName ?? "UNKNOWN";
                }
            }
            catch
            {
                return "UNKNOWN";
            }
        }

        /// <summary>
        /// Handles logging errors by writing to the Debug console. Cemengal: no second file in %TEMP% (root 0031),
        /// and no Markdown header: the daily file has one line per entry.
        /// </summary>
        private static void HandleLogError(string timestamp, string msg, string projectName)
        {
            System.Diagnostics.Debug.WriteLine($"[WAABE-ERROR] LogHelper could not write to: {GetCurrentLogPath()}");
            System.Diagnostics.Debug.WriteLine($"[WAABE-FALLBACK] {timestamp}: [{projectName}] {msg}");
        }


        /// <summary>
        /// Returns the current log file path being used: today's file (a session over midnight rolls to the next one).
        /// </summary>
        public static string GetCurrentLogPath()
        {
            return CemTempFolder.LogFile(App, Name);
        }

        /// <summary>
        /// Logs a system-wide startup event, including Navisworks version if available.
        /// </summary>
        public static void LogStartup()
        {
            LogProjectStartup("WAABE-SYSTEM");

            try
            {
                LogEvent($"Navisworks Version: {Autodesk.Navisworks.Api.Application.Version}", "SYSTEM");
            }
            catch
            {
                LogEvent("Navisworks Version: Nicht verfügbar", "SYSTEM");
            }
        }

        /// <summary>
        /// Logs detailed debug information about the bundle and log file paths.
        /// </summary>
        public static void LogPathDebug()
        {
            try
            {
                LogDebug($"Per-user home: {CemUserHome.NavisworksFolder}");
                LogDebug($"Finaler Log-Pfad: {GetCurrentLogPath()}");
            }
            catch (Exception ex)
            {
                LogError($"DEBUG FEHLER: {ex.Message}");
            }
        }

        


    }

    /// <summary>
    /// Defines the severity levels used in <see cref="LogHelper"/>.
    /// </summary> 
    public enum LogLevel
    {
        Info,
        Warning,
        Error,
        Success,
        Debug
    }


}