// Cemengal: the replies of the job tools (docs/decisions/0008).
using System.Collections.Generic;

namespace CEM_NavisIAModeler.Contracts
{
    /// <summary>
    /// A job: one request running (or run) in Navisworks. Also the data of an NVX_BUSY or
    /// NVX_JOB_RUNNING reply, so the client knows which job to follow.
    /// </summary>
    public sealed class JobStatusDto
    {
        public string job_id { get; set; } = "";
        public string method { get; set; } = "";

        /// <summary>running | done | failed | cancelled</summary>
        public string state { get; set; } = "";

        /// <summary>ISO 8601, UTC.</summary>
        public string started_utc { get; set; } = "";
        public long elapsed_ms { get; set; }

        /// <summary>What a scan is doing, and how many items it has read so far.</summary>
        public string phase { get; set; } = "";
        public long items_visited { get; set; }

        public bool cancel_requested { get; set; }

        /// <summary>A finished job's own reply ({ ok, data } or { ok, error }); null while it runs.</summary>
        public object result { get; set; }
    }

    /// <summary>The reply of get_server_status.</summary>
    public sealed class ServerStatusDto
    {
        public string server_version { get; set; } = "";
        public bool busy { get; set; }
        public JobStatusDto running_job { get; set; }

        /// <summary>
        /// Whether Navisworks' UI thread ran a small probe within a moment. False while a step that
        /// cannot pause runs (a clash test computing), or when Navisworks shows a dialog or hangs.
        /// </summary>
        public bool ui_thread_responsive { get; set; }
        public long ui_thread_reply_ms { get; set; }

        public string document_title { get; set; } = "";
        public int models_count { get; set; }
        public List<JobStatusDto> recent_jobs { get; set; } = new List<JobStatusDto>();
        public string message { get; set; } = "";
    }
}
