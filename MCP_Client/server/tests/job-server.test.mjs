// Cemengal: guards on the server's half of the job protocol (docs/decisions/0008). On 2026-10-21 one
// slow query kept Navisworks busy for three hours: every request queued on its UI thread behind the
// one before, a 10 s timeout stopped work only at coarse points, and a cancelled call was re-run off
// the UI thread. These checks read the C# sources, since the C# side has no unit tests.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const server = join(here, '..', '..', '..', 'MCP_Server', 'CEM_NavisIAModeler');
const read = (...parts) => readFileSync(join(server, ...parts), 'utf8');

/** Source without // comments, so a comment that names a pattern does not count. */
const live = source => source.split(/\r?\n/).filter(line => !line.trim().startsWith('//')).join('\n');

test('only the job tools skip the one-job-at-a-time rule', () => {
    const block = read('Infrastructure', 'RpcJobs.cs').match(/Immediate\s*=[^;]*\{([^}]*)\}/s)?.[1] ?? '';
    const immediate = [...block.matchAll(/"([a-z_]+)"/g)].map(m => m[1]).sort();
    assert.deepEqual(immediate, ['cancel_job', 'get_job_status', 'get_server_status']);
});

test('every request is dispatched through RpcJobs', () => {
    const dispatch = live(read('Infrastructure', 'RpcRouter.cs'));
    assert.match(dispatch, /RpcJobs\.Immediate\.Contains\(req\.method\)/);
    assert.match(dispatch, /RpcJobs\.Run\(req\.method,/);
});

test('no controller gives its work a fixed timeout instead of the job\'s token', () => {
    const dir = join(server, 'Controllers');
    const offenders = readdirSync(dir).filter(f => f.endsWith('.cs'))
        .filter(f => /DefaultTimeoutMs/.test(live(readFileSync(join(dir, f), 'utf8'))));
    assert.deepEqual(offenders, []);
});

test('a call that fails on the UI thread is not re-run off it', () => {
    const backend = live(read('Services', 'Backends', 'ReflectionBackend.cs'));
    const uiCatch = backend.match(/catch \(Exception ex\)\s*\{([\s\S]*?)\n\s*\}/)?.[1] ?? '';
    assert.match(uiCatch, /UI-path failed/);
    assert.match(uiCatch, /throw;/);
});

test('stopping the AI Modeler cancels its running job', () => {
    assert.match(live(read('Services', 'MCPServer.cs')), /RpcJobs\.CancelRunning\(/);
});
