// Cemengal: send_code_to_navisworks compiles C# with Roslyn and runs it (docs/decisions/0010), as
// CEM_RevitMCP's send_code_to_revit does. Compile() runs on the job's thread, so Roslyn's few seconds
// never freeze Navisworks; Run() runs on the UI thread, where the Navisworks API must be called.
//
// The code is the body of Execute(Document doc, string[] parameters, CancellationToken ct). Using
// directives at its top are moved above the class, and a #line directive maps the compiler's errors
// to the code's own line numbers. Scripts are never logged or written to disk (CEM_RevitMCP's 0023).
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace CEM_NavisIAModeler.Services.Code
{
    /// <summary>A compiled script, ready to run.</summary>
    public sealed class CompiledScript
    {
        internal MethodInfo Entry { get; set; }
        public long CompileMs { get; internal set; }
        public List<string> Warnings { get; } = new List<string>();
    }

    /// <summary>What a script returned and printed.</summary>
    public sealed class ScriptRun
    {
        public object Value { get; internal set; }
        public string Output { get; internal set; } = "";
        public long RunMs { get; internal set; }
    }

    /// <summary>The code did not compile; the message lists the errors with the code's line numbers.</summary>
    public sealed class ScriptCompileException : Exception
    {
        public ScriptCompileException(string message) : base(message) { }
    }

    /// <summary>The code threw; the message carries its exception and what it printed before.</summary>
    public sealed class ScriptFailedException : Exception
    {
        public ScriptFailedException(string message, Exception inner) : base(message, inner) { }
    }

    public static class CodeRunner
    {
        /// <summary>The path the #line directive gives the code, so its diagnostics can be told apart.</summary>
        private const string CodePath = "code";

        /// <summary>The longest console output returned.</summary>
        private const int MaxOutputChars = 100_000;

        private static readonly ConcurrentDictionary<string, MetadataReference> ReferenceCache =
            new ConcurrentDictionary<string, MetadataReference>(StringComparer.OrdinalIgnoreCase);

        private static readonly Regex UsingDirective = new Regex(
            @"^\s*using\s+(static\s+)?[A-Za-z_][\w.]*(\s*=\s*[A-Za-z_][\w.<>, ]*)?\s*;\s*$", RegexOptions.Compiled);

        /// <summary>Compiles <paramref name="code"/>. Throws <see cref="ScriptCompileException"/> with the errors.</summary>
        public static CompiledScript Compile(string code)
        {
            var clock = Stopwatch.StartNew();
            var source = Wrap(code ?? "");

            var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), path: "script.cs", encoding: Encoding.UTF8);
            var compilation = CSharpCompilation.Create(
                "CemNavisScript_" + Guid.NewGuid().ToString("N"),
                new[] { tree },
                References(),
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, optimizationLevel: OptimizationLevel.Debug));

            using (var pe = new MemoryStream())
            using (var pdb = new MemoryStream())
            {
                var emitted = compilation.Emit(pe, pdb, options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                if (!emitted.Success)
                {
                    var errors = emitted.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(Describe);
                    throw new ScriptCompileException("The code did not compile:\n" + string.Join("\n", errors));
                }

                var assembly = Assembly.Load(pe.ToArray(), pdb.ToArray());
                var script = new CompiledScript { Entry = assembly.GetType("CemNavisScript.Script").GetMethod("Execute") };
                script.Warnings.AddRange(emitted.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Warning && d.Location.GetMappedLineSpan().Path == CodePath)
                    .Select(Describe));
                script.CompileMs = clock.ElapsedMilliseconds;
                return script;
            }
        }

        /// <summary>
        /// Runs <paramref name="script"/> on the active document; call it on Navisworks' UI thread.
        /// Throws <see cref="ScriptFailedException"/> when the code throws, and lets its
        /// OperationCanceledException through.
        /// </summary>
        public static ScriptRun Run(CompiledScript script, string[] parameters, CancellationToken ct)
        {
            var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            var output = new StringWriter();
            var console = Console.Out;
            var clock = Stopwatch.StartNew();

            Console.SetOut(output);
            try
            {
                var value = script.Entry.Invoke(null, new object[] { doc, parameters ?? new string[0], ct });
                return new ScriptRun { Value = value, Output = Bounded(output.ToString()), RunMs = clock.ElapsedMilliseconds };
            }
            catch (TargetInvocationException tie) when (tie.InnerException is OperationCanceledException)
            {
                ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                throw;
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                var ex = tie.InnerException;
                var printed = Bounded(output.ToString());
                throw new ScriptFailedException(
                    $"The code threw {ex.GetType().FullName}: {ex.Message}\n{ScriptFrames(ex)}" +
                    (printed.Length > 0 ? $"\nConsole output before it:\n{printed}" : ""), ex);
            }
            finally
            {
                Console.SetOut(console);
            }
        }

        /// <summary>The code inside the Execute template; its leading using directives moved above the class.</summary>
        private static string Wrap(string code)
        {
            var lines = code.Replace("\r\n", "\n").Split('\n');
            var usings = new StringBuilder();
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("//")) continue;
                if (!UsingDirective.IsMatch(lines[i])) break;
                usings.AppendLine(trimmed);
                lines[i] = ""; // keeps the code's line numbers
            }

            return
                "using System;\n" +
                "using System.Collections.Generic;\n" +
                "using System.Linq;\n" +
                "using System.Text;\n" +
                "using System.Threading;\n" +
                "using Autodesk.Navisworks.Api;\n" +
                "using Autodesk.Navisworks.Api.Clash;\n" +
                "using Autodesk.Navisworks.Api.DocumentParts;\n" +
                "using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;\n" +
                "using ComApiBridge = Autodesk.Navisworks.Api.ComApi.ComApiBridge;\n" +
                usings +
                "namespace CemNavisScript\n{\n" +
                "public static class Script\n{\n" +
                "public static object Execute(Document doc, string[] parameters, CancellationToken ct)\n{\n" +
                $"#line 1 \"{CodePath}\"\n" +
                string.Join("\n", lines) + "\n" +
                "#line default\n" +
                "#pragma warning disable CS0162\n" +
                "return null;\n" +
                "}\n}\n}\n";
        }

        /// <summary>
        /// Every assembly loaded in Navisworks, the newest of each name, so a script can use the Navisworks API,
        /// Clash Detective, COM and the Cemengal add-ins. The Navisworks assemblies the template names are
        /// loaded first, and the netstandard facade for the libraries that target it.
        /// </summary>
        private static List<MetadataReference> References()
        {
            _ = typeof(Autodesk.Navisworks.Api.Document).Assembly;
            _ = typeof(Autodesk.Navisworks.Api.Clash.ClashTest).Assembly;
            _ = typeof(Autodesk.Navisworks.Api.ComApi.ComApiBridge).Assembly;
            _ = typeof(Autodesk.Navisworks.Api.ComApi.ComApiBridge).GetProperty("State")?.PropertyType.Assembly;
            try { Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"); } catch { }

            var newest = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic) continue;
                string location;
                try { location = assembly.Location; } catch { continue; }
                if (string.IsNullOrEmpty(location) || !File.Exists(location)) continue;

                var name = assembly.GetName();
                if (newest.TryGetValue(name.Name, out var seen) && seen.GetName().Version >= name.Version) continue;
                newest[name.Name] = assembly;
            }

            return newest.Values
                .Select(a => ReferenceCache.GetOrAdd(a.Location, path => MetadataReference.CreateFromFile(path)))
                .ToList();
        }

        private static string Describe(Diagnostic d)
        {
            var span = d.Location.GetMappedLineSpan();
            return span.Path == CodePath
                ? $"line {span.StartLinePosition.Line + 1}, col {span.StartLinePosition.Character + 1}: {d.Id} {d.GetMessage()}"
                : $"{d.Id} {d.GetMessage()}";
        }

        /// <summary>The stack frames inside the script, with its line numbers where the PDB gives them.</summary>
        private static string ScriptFrames(Exception ex)
        {
            var frames = (ex.StackTrace ?? "").Split('\n')
                .Select(f => f.TrimEnd())
                .Where(f => f.Length > 0)
                .Take(8);
            return string.Join("\n", frames);
        }

        private static string Bounded(string text)
            => text.Length <= MaxOutputChars ? text : text.Substring(0, MaxOutputChars) + $"\n... ({text.Length - MaxOutputChars} more characters)";
    }
}
