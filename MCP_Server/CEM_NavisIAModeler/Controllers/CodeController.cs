// Cemengal: send_code_to_navisworks, the escape hatch for what no tool does (docs/decisions/0010), as
// CEM_RevitMCP's send_code_to_revit. It runs as a job like every request (0008), behind the access
// token (0009). The code is compiled on the job's thread and run on Navisworks' UI thread; it is never
// logged or written anywhere, only its size and timings.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Web.Script.Serialization;
using CEM_CSharpExtended.Hosting;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Infrastructure;
using CEM_NavisIAModeler.Services.Backends;
using CEM_NavisIAModeler.Services.Code;
using CEM_NavisIAModeler_Shared;
using static CEM_NavisIAModeler.Infrastructure.ErrorHandlingMiddleware;

namespace CEM_NavisIAModeler.Controllers
{
    public sealed class CodeController
    {
        private static readonly JavaScriptSerializer _jss = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        /// <summary>
        /// RPC method "send_code_to_navisworks": params code (the body of Execute) and parameters (string[]).
        /// NVX_COMPILE when it does not compile, NVX_SCRIPT when it throws, NVX_CANCELED when it stops on ct.
        /// </summary>
        public object SendCode(RpcRequest req)
        {
            return Wrap(() =>
            {
                var p = _jss.ConvertToType<Dictionary<string, object>>(req.@params ?? new object()) ?? new Dictionary<string, object>();
                var code = p.TryGetValue("code", out var c) ? c as string : null;
                if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException("code is missing or empty.");
                var parameters = p.TryGetValue("parameters", out var a) && a is object[] list
                    ? list.Select(x => Convert.ToString(x, System.Globalization.CultureInfo.InvariantCulture) ?? "").ToArray()
                    : new string[0];

                // The bundle's binding redirects, before the first Roslyn type loads (Execute below).
                DependencyResolver.Install();
                return Execute(code, parameters, RpcJobs.CurrentToken);
            });
        }

        // A method of its own, so the JIT loads Roslyn's types only once DependencyResolver is installed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static RpcResponse<CodeRunDto> Execute(string code, string[] parameters, CancellationToken ct)
        {
            var lines = code.Split('\n').Length;
            CompiledScript script;
            try
            {
                script = CodeRunner.Compile(code);
            }
            catch (ScriptCompileException ex)
            {
                LogHelper.LogEvent($"[CODE] {lines} lines: did not compile");
                return RpcResponse<CodeRunDto>.Fail("NVX_COMPILE", ex.Message);
            }

            ct.ThrowIfCancellationRequested();
            RpcJobs.Report("running the code", 0);
            try
            {
                var dto = UiThread.InvokeAsync(() =>
                {
                    var run = CodeRunner.Run(script, parameters, ct);
                    return new CodeRunDto
                    {
                        result_json = ScriptValue.ToJson(run.Value),
                        result_type = TypeName(run.Value),
                        output = run.Output,
                        compile_ms = script.CompileMs,
                        run_ms = run.RunMs,
                        warnings = script.Warnings
                    };
                }).GetAwaiter().GetResult();

                LogHelper.LogEvent($"[CODE] {lines} lines: compiled in {dto.compile_ms} ms, ran in {dto.run_ms} ms");
                return RpcResponse<CodeRunDto>.Success(dto);
            }
            catch (ScriptFailedException ex)
            {
                LogHelper.LogEvent($"[CODE] {lines} lines: threw {ex.InnerException?.GetType().Name}");
                return RpcResponse<CodeRunDto>.Fail("NVX_SCRIPT", ex.Message);
            }
        }

        /// <summary>
        /// The returned value's type as an agent reads it: "List&lt;anonymous object&gt;" rather than the
        /// compiler's names with their assembly qualifications.
        /// </summary>
        private static string TypeName(object value) => value == null ? "" : Readable(value.GetType());

        private static string Readable(Type type)
        {
            if (type.Name.StartsWith("<>f__AnonymousType", StringComparison.Ordinal)) return "anonymous object";
            if (type.IsArray) return Readable(type.GetElementType()) + "[]";
            if (!type.IsGenericType) return type.FullName ?? type.Name;

            var name = type.GetGenericTypeDefinition().FullName ?? type.Name;
            var tick = name.IndexOf('`');
            if (tick > 0) name = name.Substring(0, tick);
            return name + "<" + string.Join(", ", type.GetGenericArguments().Select(Readable)) + ">";
        }
    }
}
