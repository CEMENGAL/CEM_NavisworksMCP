// Cemengal: one AI Modeler request at a time, run as a job (docs/decisions/0008).
//
// Every Navisworks call runs on its UI thread. Upstream posted each request there behind the ones
// before it, so one slow scan held up every later request, the agent's retries included: on
// 2026-10-21 a list_items_to_property over the whole federated model kept Navisworks busy from
// 01:26 to 04:27 while four 1 ms calls waited behind it. Now a request starts a job only when no
// job is running; any other is refused at once with NVX_BUSY and the running job. A job that
// outlasts DispatchWaitMs answers NVX_JOB_RUNNING with its id, and the Immediate methods, which
// never wait on the UI thread, follow or stop it.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CEM_NavisIAModeler.Contracts;
using CEM_NavisIAModeler_Shared;

namespace CEM_NavisIAModeler.Infrastructure
{
    /// <summary>One request running, or run, in Navisworks.</summary>
    public sealed class RpcJob
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private long _itemsVisited;
        private volatile string _phase = "";
        private volatile string _state = "running";

        internal RpcJob(string id, string method)
        {
            Id = id;
            Method = method;
            StartedUtc = DateTime.UtcNow;
        }

        public string Id { get; }
        public string Method { get; }
        public DateTime StartedUtc { get; }
        public DateTime? FinishedUtc { get; private set; }

        /// <summary>running | done | failed | cancelled</summary>
        public string State => _state;
        public CancellationToken Token => _cts.Token;
        public bool CancelRequested => _cts.IsCancellationRequested;
        public string Phase => _phase;
        public long ItemsVisited => Interlocked.Read(ref _itemsVisited);

        /// <summary>The request's own reply, once the job has ended.</summary>
        internal object Response { get; private set; }
        internal Task Completion { get; set; }

        internal void Report(string phase, long itemsVisited)
        {
            _phase = phase ?? "";
            Interlocked.Exchange(ref _itemsVisited, itemsVisited);
        }

        internal void Cancel()
        {
            try { _cts.Cancel(); } catch (ObjectDisposedException) { }
        }

        internal void Finish(object response)
        {
            Response = response;
            FinishedUtc = DateTime.UtcNow;
            var code = ErrorCodeOf(response);
            _state = code == null ? "done" : code == "NVX_CANCELED" ? "cancelled" : "failed";
        }

        /// <summary>The NVX_* code of a failed reply, or null when it succeeded.</summary>
        private static string ErrorCodeOf(object response)
        {
            if (response == null) return "NVX_NO_RESULT";
            var type = response.GetType();
            if (!(type.GetProperty("ok")?.GetValue(response) is bool ok) || ok) return null;
            return (type.GetProperty("error")?.GetValue(response) as RpcError)?.code ?? "UNKNOWN";
        }
    }

    /// <summary>Runs requests as jobs, one at a time, and answers for them.</summary>
    public static class RpcJobs
    {
        /// <summary>
        /// How long a request waits for its job before it answers NVX_JOB_RUNNING. Under the client's
        /// HTTP timeout (MCP_Client/server/navis-rpc.js; tests/job-protocol.test.mjs checks it).
        /// </summary>
        public const int DispatchWaitMs = 20000;

        /// <summary>The longest wait_ms get_job_status honours; also under the client's HTTP timeout.</summary>
        public const int MaxStatusWaitMs = 25000;

        private const int KeepFinished = 20;

        /// <summary>
        /// The methods that run outside the one-job rule: they answer at once, never wait on the UI
        /// thread longer than a short probe, and so work while a job runs.
        /// </summary>
        public static readonly HashSet<string> Immediate = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "get_server_status", "get_job_status", "cancel_job"
        };

        private static readonly object _lock = new object();
        private static readonly List<RpcJob> _finished = new List<RpcJob>();
        private static RpcJob _running;
        private static long _sequence;

        /// <summary>The running job, if any.</summary>
        public static RpcJob Current { get { lock (_lock) return _running; } }

        /// <summary>The running job's token: the work of a request stops when its job is cancelled.</summary>
        public static CancellationToken CurrentToken => Current?.Token ?? CancellationToken.None;

        /// <summary>
        /// Runs <paramref name="work"/> as a job if none is running, and returns its reply if it ends
        /// within <see cref="DispatchWaitMs"/>; otherwise NVX_JOB_RUNNING. Returns NVX_BUSY, without
        /// running anything, while another job runs.
        /// </summary>
        public static object Run(string method, Func<object> work)
        {
            RpcJob job, busy;
            lock (_lock)
            {
                busy = _running;
                if (busy != null)
                {
                    job = null;
                }
                else
                {
                    var n = Interlocked.Increment(ref _sequence).ToString(CultureInfo.InvariantCulture);
                    job = new RpcJob("j" + n + "-" + Guid.NewGuid().ToString("N").Substring(0, 6), method);
                    _running = job;
                    job.Completion = Task.Run(() => Execute(job, work));
                }
            }

            if (job == null)
            {
                var secs = (long)(DateTime.UtcNow - busy.StartedUtc).TotalSeconds;
                LogHelper.LogEvent($"[JOB] {method} refused: busy with {busy.Id} ({busy.Method}, {secs} s)");
                return Fail("NVX_BUSY",
                    $"Navisworks is busy with job {busy.Id} ('{busy.Method}', running {secs} s), so '{method}' was not started. " +
                    $"Wait for the job with get_job_status (job_id \"{busy.Id}\") or stop it with cancel_job, then send '{method}' again.",
                    Snapshot(busy, false));
            }

            if (job.Completion.Wait(DispatchWaitMs)) return job.Response;

            return Fail("NVX_JOB_RUNNING",
                $"'{method}' is still running in Navisworks as job {job.Id}. Follow it with get_job_status (job_id \"{job.Id}\"), or stop it with cancel_job.",
                Snapshot(job, false));
        }

        private static void Execute(RpcJob job, Func<object> work)
        {
            LogHelper.LogEvent($"[JOB] {job.Id} {job.Method} started");
            object response;
            try
            {
                response = work();
            }
            catch (OperationCanceledException)
            {
                response = RpcResponse<object>.Fail("NVX_CANCELED", "Operation was canceled.");
            }
            catch (Exception ex)
            {
                response = RpcResponse<object>.Fail("NVX_INTERNAL", ex.Message);
            }

            lock (_lock)
            {
                job.Finish(response);
                if (ReferenceEquals(_running, job)) _running = null;
                _finished.Insert(0, job);
                if (_finished.Count > KeepFinished) _finished.RemoveAt(_finished.Count - 1);
            }
            LogHelper.LogEvent($"[JOB] {job.Id} {job.Method} {job.State} after {ElapsedMs(job)} ms");
        }

        /// <summary>The job <paramref name="id"/>; without an id, the running job or else the last one.</summary>
        public static RpcJob Find(string id)
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(id)) return _running ?? _finished.FirstOrDefault();
                if (_running != null && _running.Id == id) return _running;
                return _finished.FirstOrDefault(j => j.Id == id);
            }
        }

        /// <summary>Waits up to <paramref name="waitMs"/> (at most <see cref="MaxStatusWaitMs"/>) for the job to end.</summary>
        public static void Wait(RpcJob job, int waitMs)
        {
            if (job?.Completion == null || waitMs <= 0) return;
            job.Completion.Wait(Math.Min(waitMs, MaxStatusWaitMs));
        }

        /// <summary>Asks the job to stop; its work stops at its next check (a scan's next pause).</summary>
        public static void Cancel(RpcJob job, string why)
        {
            if (job == null || job.State != "running") return;
            LogHelper.LogEvent($"[JOB] {job.Id} {job.Method} cancel requested ({why})");
            job.Cancel();
        }

        /// <summary>Cancels the running job, if any.</summary>
        public static void CancelRunning(string why) => Cancel(Current, why);

        /// <summary>Progress from a scan (UiSlice), shown by get_job_status.</summary>
        public static void Report(string phase, long itemsVisited) => Current?.Report(phase, itemsVisited);

        /// <summary>The last <paramref name="count"/> jobs, newest first, without their results.</summary>
        public static List<JobStatusDto> Recent(int count)
        {
            lock (_lock)
                return _finished.Take(count).Select(j => Snapshot(j, false)).ToList();
        }

        public static JobStatusDto Snapshot(RpcJob job, bool withResult)
        {
            return new JobStatusDto
            {
                job_id = job.Id,
                method = job.Method,
                state = job.State,
                started_utc = job.StartedUtc.ToString("o", CultureInfo.InvariantCulture),
                elapsed_ms = ElapsedMs(job),
                phase = job.Phase,
                items_visited = job.ItemsVisited,
                cancel_requested = job.CancelRequested,
                result = withResult ? job.Response : null
            };
        }

        private static long ElapsedMs(RpcJob job)
            => (long)((job.FinishedUtc ?? DateTime.UtcNow) - job.StartedUtc).TotalMilliseconds;

        private static RpcResponse<JobStatusDto> Fail(string code, string msg, JobStatusDto job)
            => new RpcResponse<JobStatusDto> { ok = false, data = job, error = new RpcError { code = code, msg = msg } };
    }
}
