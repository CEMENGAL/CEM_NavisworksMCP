// Cemengal: the job tools (docs/decisions/0008). They run outside the one-job rule (RpcJobs.Immediate)
// and never wait on Navisworks' UI thread longer than get_server_status's short probe, so an agent
// can always ask what Navisworks is doing, wait for a job, or stop it.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web.Script.Serialization;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler.Infrastructure;
using CEM_NavisIAModeler.Services.Backends;
using static CEM_NavisIAModeler.Infrastructure.ErrorHandlingMiddleware;

namespace CEM_NavisIAModeler.Controllers
{
    public sealed class JobsController
    {
        /// <summary>How long get_server_status waits for the UI thread to run its probe.</summary>
        private const int UiProbeMs = 1500;

        private static readonly JavaScriptSerializer _jss = new JavaScriptSerializer();

        /// <summary>RPC method "get_server_status": busy or not, the UI thread's health, the recent jobs.</summary>
        public object GetServerStatus(RpcRequest req)
        {
            return Wrap(() =>
            {
                var running = RpcJobs.Current;
                var dto = new ServerStatusDto
                {
                    server_version = Settings.ServerVersion,
                    busy = running != null,
                    running_job = running == null ? null : RpcJobs.Snapshot(running, false),
                    recent_jobs = RpcJobs.Recent(5)
                };
                ProbeUiThread(dto);

                if (dto.ui_thread_responsive)
                    dto.message = running == null ? "Idle." : $"Running job {running.Id} ({running.Method}); Navisworks answers between its steps.";
                else if (running != null)
                    dto.message = $"Running job {running.Id} ({running.Method}) in a step that cannot pause (a clash test computing, for example), or Navisworks shows a dialog. Wait for it with get_job_status.";
                else
                    dto.message = "No job is running, but Navisworks' UI thread does not answer: it may show a dialog, be busy with something else, or hang.";

                return RpcResponse<ServerStatusDto>.Success(dto);
            });
        }

        /// <summary>
        /// RPC method "get_job_status": params job_id (default: the running or last job) and wait_ms
        /// (wait for the job to end, at most RpcJobs.MaxStatusWaitMs). A finished job carries its reply.
        /// </summary>
        public object GetJobStatus(RpcRequest req)
        {
            return Wrap(() =>
            {
                var p = Params(req);
                var job = RpcJobs.Find(Text(p, "job_id"));
                if (job == null) return NotFound<JobStatusDto>(Text(p, "job_id"));

                RpcJobs.Wait(job, Number(p, "wait_ms"));
                return RpcResponse<JobStatusDto>.Success(RpcJobs.Snapshot(job, true));
            });
        }

        /// <summary>RPC method "cancel_job": params job_id (default: the running job).</summary>
        public object CancelJob(RpcRequest req)
        {
            return Wrap(() =>
            {
                var p = Params(req);
                var job = RpcJobs.Find(Text(p, "job_id"));
                if (job == null) return NotFound<JobStatusDto>(Text(p, "job_id"));

                RpcJobs.Cancel(job, "cancel_job");
                return RpcResponse<JobStatusDto>.Success(RpcJobs.Snapshot(job, false));
            });
        }

        /// <summary>
        /// Posts a tiny read of the active document to the UI thread and waits <see cref="UiProbeMs"/>.
        /// A running scan pauses every 100 ms, so the probe answers during one; a step that cannot
        /// pause, a modal dialog or a hang leaves it unanswered (it then runs later, harmlessly).
        /// </summary>
        private static void ProbeUiThread(ServerStatusDto dto)
        {
            if (!UiThread.IsInitialized) return;

            var sw = Stopwatch.StartNew();
            var probe = UiThread.InvokeAsync(() =>
            {
                var doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
                return doc == null ? null : Tuple.Create(doc.Title ?? "", doc.Models?.Count ?? 0);
            });

            try
            {
                dto.ui_thread_responsive = probe.Wait(UiProbeMs);
            }
            catch (AggregateException)
            {
                dto.ui_thread_responsive = true; // it answered, with an error
            }
            dto.ui_thread_reply_ms = sw.ElapsedMilliseconds;

            if (dto.ui_thread_responsive && probe.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && probe.Result != null)
            {
                dto.document_title = probe.Result.Item1;
                dto.models_count = probe.Result.Item2;
            }
        }

        private static RpcResponse<T> NotFound<T>(string id)
            => RpcResponse<T>.Fail("NVX_NOT_FOUND", string.IsNullOrWhiteSpace(id)
                ? "No job has run yet."
                : $"No job '{id}'. The AI Modeler keeps the last 20 jobs, and forgets them when it restarts.");

        private static Dictionary<string, object> Params(RpcRequest req)
            => _jss.ConvertToType<Dictionary<string, object>>(req.@params ?? new object())
               ?? new Dictionary<string, object>();

        private static string Text(Dictionary<string, object> p, string key)
            => p.TryGetValue(key, out var v) ? v?.ToString()?.Trim() : null;

        private static int Number(Dictionary<string, object> p, string key)
        {
            if (!p.TryGetValue(key, out var v) || v == null) return 0;
            try { return Convert.ToInt32(v, System.Globalization.CultureInfo.InvariantCulture); }
            catch (Exception) { return 0; }
        }
    }
}
