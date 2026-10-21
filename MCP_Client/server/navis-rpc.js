// Cemengal: the client's half of the job protocol (docs/decisions/0008-one-job-at-a-time.md).
// Navisworks runs one AI Modeler request at a time, on its UI thread. The server answers within
// RpcJobs.DispatchWaitMs; a request that runs longer comes back as NVX_JOB_RUNNING with its job id,
// and this module follows the job with get_job_status. Every tool call ends within TOOL_BUDGET_MS,
// under the agent host's request timeout (60 s in Claude Desktop), so the agent always gets an
// answer: the result, or the job id to wait on. It does not import the MCP SDK, so the tests can
// load it without npm install.
//
// It also sends the AI Modeler's access token (docs/decisions/0009-local-access-token.md), read
// again for every request, so a restarted AI Modeler (a new token) needs no client restart.
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

/**
 * The file the AI Modeler writes its token to at each start: the user's Navisworks settings folder
 * (AccessToken.cs, CemUserHome.NavisworksSettingsFile). NAVISWORKS_TOKEN_FILE overrides it.
 */
export function defaultTokenFile(env = process.env) {
    return join(env.APPDATA ?? '', 'Autodesk', 'ApplicationPlugins', 'CEM_NavisworksAPI.bundle', 'Settings', 'CEM_NavisIAModeler.token');
}

function tokenFile() {
    return process.env.NAVISWORKS_TOKEN_FILE || defaultTokenFile();
}

function readToken() {
    try { return readFileSync(tokenFile(), 'utf8').trim(); } catch { return ''; }
}

/** A tool call's whole time budget, under the agent host's 60 s request timeout. */
export const TOOL_BUDGET_MS = 45000;

/** One HTTP request's timeout, above the server's longest wait (RpcJobs.MaxStatusWaitMs). */
export const HTTP_TIMEOUT_MS = 30000;

/** The longest wait the client asks get_job_status for, per request. */
export const STATUS_WAIT_MS = 10000;

/** The AI Modeler refused or failed a call. `code` is its NVX_* code or a transport code. */
export class NavisRpcError extends Error {
    constructor(code, message, job = null) {
        super(message);
        this.name = 'NavisRpcError';
        this.code = code;
        this.job = job;
    }
}

/** The call is still running in Navisworks as `job` when the tool call's budget ran out. */
export class JobPending extends Error {
    constructor(job) {
        super(`job ${job?.job_id} is still running`);
        this.name = 'JobPending';
        this.job = job;
    }
}

/** One POST /rpc. Returns the reply { ok, data, error }; throws NavisRpcError if there is none. */
export async function postRpc(baseUrl, method, params = {}, { timeoutMs = HTTP_TIMEOUT_MS } = {}) {
    const headers = { 'Content-Type': 'application/json' };
    const token = readToken();
    if (token) headers.Authorization = `Bearer ${token}`;

    let resp;
    try {
        resp = await fetch(`${baseUrl}/rpc`, {
            method: 'POST',
            headers,
            body: JSON.stringify({ id: String(Date.now()), method, params }),
            signal: AbortSignal.timeout(timeoutMs),
        });
    } catch (e) {
        if (e?.name === 'TimeoutError' || e?.name === 'AbortError') {
            throw new NavisRpcError('NO_ANSWER',
                `The AI Modeler did not answer within ${Math.round(timeoutMs / 1000)} s. Navisworks may be ` +
                'showing a dialog or be frozen: call get_server_status, or look at Navisworks.');
        }
        throw new NavisRpcError('NOT_RUNNING',
            `Cannot reach the AI Modeler at ${baseUrl}. In Navisworks, press Cemengal > AI > AI Modeler ` +
            '(the button stays pressed while it runs).');
    }
    if (resp.status === 401) {
        throw new NavisRpcError('UNAUTHORIZED',
            `The AI Modeler refused the request: no valid access token. The client reads it from ${tokenFile()}, ` +
            'which the AI Modeler writes each time it starts. Check that the file exists for this Windows user ' +
            '(or set NAVISWORKS_TOKEN_FILE), or press AI Modeler off and on again.');
    }
    if (!resp.ok) throw new NavisRpcError(`HTTP_${resp.status}`, `HTTP ${resp.status}: ${resp.statusText}`);

    let reply;
    try { reply = await resp.json(); } catch { reply = null; }
    if (!reply || typeof reply.ok !== 'boolean') {
        throw new NavisRpcError('BAD_REPLY', 'Invalid RPC reply (no ok field).');
    }
    return reply;
}

/** A finished reply's data, or a NavisRpcError with its code. */
function unwrap(reply) {
    if (reply.ok) return reply.data;
    const code = reply.error?.code ?? 'UNKNOWN';
    throw new NavisRpcError(code, `RPC error ${code}: ${reply.error?.msg ?? 'unknown error'}`, reply.data ?? null);
}

/**
 * Follows a job with get_job_status until it ends or `deadline` comes. Returns the job's final
 * status, whose `result` is the request's own reply; throws JobPending if it is still running.
 */
async function followJob(baseUrl, job, deadline) {
    let last = job;
    for (;;) {
        const remaining = deadline - Date.now();
        if (remaining <= 1000) throw new JobPending(last);
        const wait_ms = Math.min(STATUS_WAIT_MS, remaining - 1000);
        const status = unwrap(await postRpc(baseUrl, 'get_job_status', { job_id: last.job_id, wait_ms }));
        if (status.state !== 'running') return status;
        last = status;
    }
}

/**
 * Calls `method` and returns its data. A call still running after the server's wait is followed
 * until `budgetMs` runs out, then it throws JobPending with the job to wait on.
 */
export async function callRpc(baseUrl, method, params = {}, { budgetMs = TOOL_BUDGET_MS } = {}) {
    const deadline = Date.now() + budgetMs;
    const reply = await postRpc(baseUrl, method, params);
    if (reply.ok || reply.error?.code !== 'NVX_JOB_RUNNING') return unwrap(reply);

    const status = await followJob(baseUrl, reply.data, deadline);
    return unwrap(status.result ?? noResult(status));
}

/**
 * Waits for the job `jobId` until `budgetMs` runs out. Returns { status, data } when it ended
 * well, throws NavisRpcError when it failed or was cancelled, and JobPending while it runs.
 */
export async function awaitJob(baseUrl, jobId, { budgetMs = TOOL_BUDGET_MS } = {}) {
    const status = await followJob(baseUrl, { job_id: jobId }, Date.now() + budgetMs);
    return { status, data: unwrap(status.result ?? noResult(status)) };
}

function noResult(status) {
    return { ok: false, error: { code: 'NVX_NO_RESULT', msg: `job ${status.job_id} ended (${status.state}) without a result.` } };
}

/** What the agent reads when a job outlasts its tool call. */
export function pendingText(job) {
    const secs = Math.round((job?.elapsed_ms ?? 0) / 1000);
    const scanned = job?.items_visited ? `, ${job.items_visited} items scanned so far` : '';
    return `Still running in Navisworks as job ${job?.job_id} (${job?.method}, ${secs} s${scanned}). ` +
        `Call get_job_status with job_id "${job?.job_id}" to wait for its result, or cancel_job to stop it. ` +
        'Do not send the request again: Navisworks runs one request at a time and refuses others while this one runs.';
}
