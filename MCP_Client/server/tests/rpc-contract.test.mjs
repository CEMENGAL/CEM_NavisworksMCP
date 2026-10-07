// Cemengal: the RPC contract between the two halves of the MCP (docs/concepts/rpc-contract.md).
// The Node client (index.js) calls methods by name; the C# server routes them in RpcMap.cs and
// advertises them in mcp_manifest.json. A name that drifts on one side only fails at run time,
// inside Navisworks, so these tests read the three files and compare the names.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const read = (...parts) => readFileSync(join(repo, ...parts), 'utf8');

/** Methods the Node client calls: this.rpc('name', ...) or this.t_simple('name'). */
export function clientMethods(source) {
    return new Set([...source.matchAll(/\b(?:rpc|t_simple)\(\s*['"]([a-z0-9_.]+)['"]/g)].map(m => m[1]));
}

/** MCP tools the Node client lists: { name: 'tool', ... }. Each is named after its RPC method. */
export function clientTools(source) {
    return new Set([...source.matchAll(/\bname:\s*['"]([a-z0-9_]+)['"]/g)].map(m => m[1]));
}

/** Routes the C# server registers: routes["name"] = ..., ignoring commented-out lines. */
export function serverRoutes(source) {
    const live = source.split(/\r?\n/).filter(line => !line.trim().startsWith('//')).join('\n');
    return new Set([...live.matchAll(/routes\[\s*"([a-z0-9_.]+)"\s*\]\s*=/g)].map(m => m[1]));
}

/** Methods the manifest advertises, across every group of its "rpc" object. */
export function manifestMethods(json) {
    return new Set(Object.values(JSON.parse(json).rpc ?? {}).flat());
}

const clientSource = read('MCP_Client', 'server', 'index.js');
const client = clientMethods(clientSource);
const tools = clientTools(clientSource);
const server = serverRoutes(read('MCP_Server', 'CEM_NavisIAModeler', 'Mapping', 'RpcMap.cs'));
const manifest = manifestMethods(read('MCP_Server', 'CEM_NavisIAModeler', 'mcp_manifest.json'));

const missing = (from, to) => [...from].filter(name => !to.has(name)).sort();

test('the parsers find the methods on every side', () => {
    assert.ok(client.size > 0, 'no rpc(...) calls found in index.js');
    assert.ok(server.size > 0, 'no routes[...] found in RpcMap.cs');
    assert.ok(manifest.size > 0, 'no rpc methods found in mcp_manifest.json');
});

test('every method the client calls is routed by the server', () => {
    assert.deepEqual(missing(client, server), []);
});

test('every MCP tool the client lists is a routed method of the same name', () => {
    assert.ok(tools.size > 0, 'no tools found in index.js');
    assert.deepEqual(missing(tools, server), []);
});

test('every method the manifest advertises is routed by the server', () => {
    assert.deepEqual(missing(manifest, server), []);
});

test('every routed method is advertised by the manifest', () => {
    assert.deepEqual(missing(server, manifest), []);
});

test('serverRoutes ignores commented-out routes', () => {
    const routes = serverRoutes('routes["a"] = x;\n   // routes["b"] = y;\nroutes["c"] = z;');
    assert.deepEqual([...routes].sort(), ['a', 'c']);
});

test('clientMethods accepts both quote styles and the t_simple helper', () => {
    const methods = clientMethods(`await this.rpc('a', {}); await this.rpc("b"); this.t_simple('c');`);
    assert.deepEqual([...methods].sort(), ['a', 'b', 'c']);
});

test('clientTools skips the hyphenated server name', () => {
    const names = clientTools(`{ name: 'waabe-navisworks-mcp' } { name: 'get_x' }`);
    assert.deepEqual([...names], ['get_x']);
});

// The other tests only read index.js as text; this one makes sure it still parses as a module.
test('the client parses', async () => {
    const { execFileSync } = await import('node:child_process');
    execFileSync(process.execPath, ['--check', join(repo, 'MCP_Client', 'server', 'index.js')], { stdio: 'pipe' });
});
