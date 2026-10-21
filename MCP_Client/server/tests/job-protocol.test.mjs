// Cemengal: the client's half of the job protocol (docs/decisions/0008). A fake AI Modeler replies
// the way the real one does (at once, NVX_JOB_RUNNING followed by get_job_status, NVX_BUSY, nothing)
// and the client must turn each into the right answer for the agent, within its time budget.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFileSync, writeFileSync, mkdtempSync, rmSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import {
    callRpc, awaitJob, postRpc, pendingText, JobPending, NavisRpcError,
    TOOL_BUDGET_MS, HTTP_TIMEOUT_MS, STATUS_WAIT_MS,
} from '../navis-rpc.js';

const sleep = ms => new Promise(r => setTimeout(r, ms));

// Every test talks to the fake server with this token, as the real client reads the AI Modeler's.
const tokenDir = mkdtempSync(join(tmpdir(), 'navis-token-'));
const TOKEN = 'a'.repeat(64);
process.env.NAVISWORKS_TOKEN_FILE = join(tokenDir, 'CEM_NavisIAModeler.token');
writeFileSync(process.env.NAVISWORKS_TOKEN_FILE, TOKEN);
process.on('exit', () => rmSync(tokenDir, { recursive: true, force: true }));

/** A fake AI Modeler. `handler(method, params)` returns the reply, or undefined to never answer. */
async function fakeServer(handler) {
    const calls = [];
    const server = createServer(async (req, res) => {
        let body = '';
        for await (const chunk of req) body += chunk;
        if (req.headers.authorization !== `Bearer ${TOKEN}`) {
            res.writeHead(401, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify({ ok: false, error: { code: 'NVX_UNAUTHORIZED', msg: 'missing or wrong token' } }));
            return;
        }
        const { method, params } = JSON.parse(body);
        calls.push(method);
        const reply = await handler(method, params);
        if (reply === undefined) return;
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify(reply));
    });
    await new Promise(r => server.listen(0, '127.0.0.1', r));
    const url = `http://127.0.0.1:${server.address().port}`;
    const close = () => new Promise(r => { server.closeAllConnections(); server.close(r); });
    return { url, calls, close };
}

const running = (job_id, method) => ({
    ok: false, data: { job_id, method, state: 'running', elapsed_ms: 21000 },
    error: { code: 'NVX_JOB_RUNNING', msg: 'still running' },
});

test('a call that finishes at once returns its data', async () => {
    const fake = await fakeServer(() => ({ ok: true, data: { length_unit: 'mm' } }));
    try {
        assert.deepEqual(await callRpc(fake.url, 'get_units_and_tolerances'), { length_unit: 'mm' });
        assert.deepEqual(fake.calls, ['get_units_and_tolerances']);
    } finally { await fake.close(); }
});

test('a long call is followed with get_job_status and returns the job\'s own data', async () => {
    let polls = 0;
    const fake = await fakeServer((method, params) => {
        if (method === 'list_items_to_property') return running('j1', method);
        assert.equal(params.job_id, 'j1');
        assert.ok(params.wait_ms > 0 && params.wait_ms <= STATUS_WAIT_MS);
        polls++;
        if (polls < 2) return { ok: true, data: { job_id: 'j1', state: 'running' } };
        return { ok: true, data: { job_id: 'j1', state: 'done', result: { ok: true, data: { count: 3 } } } };
    });
    try {
        assert.deepEqual(await callRpc(fake.url, 'list_items_to_property', {}), { count: 3 });
        assert.deepEqual(fake.calls, ['list_items_to_property', 'get_job_status', 'get_job_status']);
    } finally { await fake.close(); }
});

test('a job that outlasts the budget ends the tool call with JobPending, not an error', async () => {
    const fake = await fakeServer(async (method, params) => {
        if (method === 'run_simple_clash') return running('j2', method);
        await sleep(params.wait_ms); // the server long-polls
        return { ok: true, data: { job_id: 'j2', method: 'run_simple_clash', state: 'running', elapsed_ms: 30000 } };
    });
    try {
        const started = Date.now();
        await assert.rejects(callRpc(fake.url, 'run_simple_clash', {}, { budgetMs: 1600 }), err => {
            assert.ok(err instanceof JobPending);
            assert.equal(err.job.job_id, 'j2');
            return true;
        });
        assert.ok(Date.now() - started < 1600, 'the tool call must end within its budget');
    } finally { await fake.close(); }
});

test('a job that was cancelled or failed is an error with the job\'s code', async () => {
    const fake = await fakeServer(method => method === 'get_job_status'
        ? { ok: true, data: { job_id: 'j3', state: 'cancelled', result: { ok: false, error: { code: 'NVX_CANCELED', msg: 'Operation was canceled.' } } } }
        : running('j3', method));
    try {
        await assert.rejects(callRpc(fake.url, 'get_property_distribution_by_category'),
            err => err instanceof NavisRpcError && err.code === 'NVX_CANCELED');
    } finally { await fake.close(); }
});

test('a request refused because another job runs reports that job', async () => {
    const fake = await fakeServer(() => ({
        ok: false, data: { job_id: 'j4', method: 'run_simple_clash', state: 'running' },
        error: { code: 'NVX_BUSY', msg: 'Navisworks is busy with job j4' },
    }));
    try {
        await assert.rejects(callRpc(fake.url, 'get_units_and_tolerances'), err => {
            assert.ok(err instanceof NavisRpcError);
            assert.equal(err.code, 'NVX_BUSY');
            assert.equal(err.job.job_id, 'j4');
            assert.match(err.message, /busy with job j4/);
            return true;
        });
        assert.deepEqual(fake.calls, ['get_units_and_tolerances'], 'a refused request is not retried');
    } finally { await fake.close(); }
});

test('get_job_status for a finished job returns its status and data', async () => {
    const fake = await fakeServer(() => ({
        ok: true, data: { job_id: 'j5', method: 'run_simple_clash', state: 'done', result: { ok: true, data: { results: 12 } } },
    }));
    try {
        const { status, data } = await awaitJob(fake.url, 'j5');
        assert.equal(status.method, 'run_simple_clash');
        assert.deepEqual(data, { results: 12 });
    } finally { await fake.close(); }
});

test('no AI Modeler listening says how to start it', async () => {
    const fake = await fakeServer(() => ({ ok: true, data: {} }));
    const url = fake.url;
    await fake.close();
    await assert.rejects(postRpc(url, 'get_server_status'), err => {
        assert.equal(err.code, 'NOT_RUNNING');
        assert.match(err.message, /AI Modeler/);
        return true;
    });
});

test('a server that never answers times out with NO_ANSWER', async () => {
    const fake = await fakeServer(() => undefined);
    try {
        await assert.rejects(postRpc(fake.url, 'get_units_and_tolerances', {}, { timeoutMs: 300 }),
            err => err.code === 'NO_ANSWER' && /get_server_status/.test(err.message));
    } finally { await fake.close(); }
});

test('a request without the AI Modeler\'s current token is refused, and says why', async () => {
    const fake = await fakeServer(() => ({ ok: true, data: {} }));
    try {
        writeFileSync(process.env.NAVISWORKS_TOKEN_FILE, 'b'.repeat(64)); // a token from an earlier start
        await assert.rejects(postRpc(fake.url, 'get_server_status'), err => {
            assert.equal(err.code, 'UNAUTHORIZED');
            assert.match(err.message, /token/);
            return true;
        });
        writeFileSync(process.env.NAVISWORKS_TOKEN_FILE, TOKEN); // the AI Modeler restarted: read again, no client restart
        assert.deepEqual(await postRpc(fake.url, 'get_server_status'), { ok: true, data: {} });
    } finally {
        writeFileSync(process.env.NAVISWORKS_TOKEN_FILE, TOKEN);
        await fake.close();
    }
});

test('the agent is told the job id and not to resend', () => {
    const text = pendingText({ job_id: 'j9', method: 'list_items_to_property', elapsed_ms: 47000, items_visited: 120000 });
    assert.match(text, /job j9/);
    assert.match(text, /get_job_status/);
    assert.match(text, /120000 items/);
    assert.match(text, /Do not send the request again/);
});

// The waits nest: the server answers before the client's HTTP timeout, and the whole tool call ends
// before the agent host gives up on it (Claude Desktop: 60 s), so no answer is ever lost.
test('the time budgets nest from the server to the agent host', () => {
    const here = dirname(fileURLToPath(import.meta.url));
    const jobs = readFileSync(join(here, '..', '..', '..', 'MCP_Server', 'CEM_NavisIAModeler', 'Infrastructure', 'RpcJobs.cs'), 'utf8');
    const constant = name => Number(jobs.match(new RegExp(`const int ${name}\\s*=\\s*(\\d+)`))?.[1]);
    const dispatchWait = constant('DispatchWaitMs');
    const statusWait = constant('MaxStatusWaitMs');
    assert.ok(dispatchWait > 0 && statusWait > 0, 'RpcJobs.cs declares DispatchWaitMs and MaxStatusWaitMs');
    assert.ok(dispatchWait < HTTP_TIMEOUT_MS && statusWait < HTTP_TIMEOUT_MS);
    assert.ok(STATUS_WAIT_MS <= statusWait);
    assert.ok(dispatchWait + 2000 < TOOL_BUDGET_MS, 'room to follow a job after the first wait');
    assert.ok(TOOL_BUDGET_MS < 60000, 'under the agent host\'s request timeout');
});
