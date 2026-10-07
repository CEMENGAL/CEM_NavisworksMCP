// Cemengal: guards on the RPC server's exposure (docs/decisions/0005). The server has no
// authentication, so the only things keeping other software out are the loopback binding and the
// absence of CORS: a wildcard Access-Control-Allow-Origin would let any web page open in the user's
// browser call /rpc. The MCP client is Node, not a browser, and needs no CORS at all.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const serverSource = readFileSync(
    join(here, '..', '..', '..', 'MCP_Server', 'CEM_NavisIAModeler', 'Services', 'MCPServer.cs'), 'utf8');

/** Source lines that are not comments. */
const liveLines = source => source.split(/\r?\n/).filter(line => !line.trim().startsWith('//'));

test('the server sends no CORS Allow-Origin header', () => {
    const offending = liveLines(serverSource).filter(line => /Access-Control-Allow-Origin/i.test(line));
    assert.deepEqual(offending, []);
});

// Without CORS a browser still SENDS a "simple" cross-origin POST (text/plain), it just cannot read
// the reply; the side effects (apply_selection, run_simple_clash) would still happen. Browsers always
// attach an Origin header to it; the Node client never does. So the server refuses such requests.
test('the server refuses requests that carry an Origin header', () => {
    const live = liveLines(serverSource).join('\n');
    assert.match(live, /Headers\[\s*"Origin"\s*\]/);
    assert.match(live, /StatusCode\s*=\s*403/);
});

test('the server listens on loopback only', () => {
    const prefixes = liveLines(serverSource)
        .filter(line => /Prefixes\.Add|\$"http:\/\//.test(line))
        .join('\n');
    assert.match(prefixes, /127\.0\.0\.1|localhost/);
    assert.doesNotMatch(prefixes, /http:\/\/(\+|\*|0\.0\.0\.0)[:/]/);
});
