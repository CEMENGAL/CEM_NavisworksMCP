// Cemengal: lets a long scan on Navisworks' UI thread give Navisworks its turn (docs/decisions/0008).
//
// Every Navisworks API call must run on the UI thread, so a scan of a 1.4-million-item federated model
// froze Navisworks for as long as it ran, with no way to stop it. A scan now calls Step() once per
// item; when a slice of SliceMs is used up it awaits PauseAsync(), which reports progress to the job,
// hands the UI thread back for PauseMs (Navisworks repaints, takes input, answers get_server_status),
// and then stops the scan if its job was cancelled, or if the document changed under it meanwhile.
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autodesk.Navisworks.Api;
using CEM_NavisIAModeler.Infrastructure;

namespace CEM_NavisIAModeler.Services.Backends
{
    public sealed class UiSlice
    {
        /// <summary>How long a scan works before it pauses.</summary>
        public const int SliceMs = 100;

        /// <summary>How long it then leaves the UI thread to Navisworks.</summary>
        public const int PauseMs = 20;

        private readonly CancellationToken _ct;
        private readonly Document _doc;
        private readonly string _fileName;
        private readonly int _modelCount;
        private readonly Stopwatch _clock = Stopwatch.StartNew();
        private string _phase;
        private long _visited;

        /// <param name="ct">The job's token; the scan stops at its next pause once it is cancelled.</param>
        /// <param name="doc">The document being scanned, or null to skip the document check.</param>
        /// <param name="phase">What the scan does, for get_job_status.</param>
        public UiSlice(CancellationToken ct, Document doc, string phase)
        {
            ct.ThrowIfCancellationRequested();
            _ct = ct;
            _doc = doc;
            _fileName = doc?.FileName ?? "";
            _modelCount = doc?.Models?.Count ?? 0;
            Phase = phase;
        }

        /// <summary>Items counted so far.</summary>
        public long Visited => _visited;

        /// <summary>The scan's current step, shown by get_job_status.</summary>
        public string Phase
        {
            get => _phase;
            set { _phase = value ?? ""; RpcJobs.Report(_phase, _visited); }
        }

        /// <summary>Counts one item. True when the slice is used up: the caller then awaits <see cref="PauseAsync"/>.</summary>
        public bool Step()
        {
            _visited++;
            return _clock.ElapsedMilliseconds >= SliceMs;
        }

        /// <summary>
        /// Gives Navisworks the UI thread for <see cref="PauseMs"/>, then continues on it. Throws
        /// OperationCanceledException when the job was cancelled, and DocumentChangedException when
        /// the active document or its models changed during the pause.
        /// </summary>
        public async Task PauseAsync()
        {
            _ct.ThrowIfCancellationRequested();
            RpcJobs.Report(_phase, _visited);

            await UiThread.PauseAsync(PauseMs);

            _ct.ThrowIfCancellationRequested();
            if (_doc != null)
            {
                var now = Application.ActiveDocument;
                if (now == null
                    || !string.Equals(now.FileName ?? "", _fileName, StringComparison.OrdinalIgnoreCase)
                    || (now.Models?.Count ?? 0) != _modelCount)
                {
                    throw new DocumentChangedException();
                }
            }
            _clock.Restart();
        }
    }

    /// <summary>The active document or its models changed while a scan paused.</summary>
    public sealed class DocumentChangedException : InvalidOperationException
    {
        public DocumentChangedException()
            : base("The document or its models changed while the AI Modeler was reading them. Send the request again.") { }
    }
}
