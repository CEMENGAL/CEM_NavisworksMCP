// Cemengal: the AI Modeler's local access token (docs/decisions/0009). The server listens on loopback
// only and refuses browsers (0005), but before 0009 any program on the machine, another user's
// session included, could call /rpc; with send_code_to_navisworks that means running code as the
// user. At each start the server writes a random token to the user's own settings folder and refuses
// every request but the health check without it; the client reads it from there.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';
import { defaultTokenFile } from '../navis-rpc.js';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const read = (...parts) => readFileSync(join(repo, ...parts), 'utf8');
const live = source => source.split(/\r?\n/).filter(line => !line.trim().startsWith('//')).join('\n');

const tokenSource = live(read('MCP_Server', 'CEM_NavisIAModeler', 'Infrastructure', 'AccessToken.cs'));
const httpSource = live(read('MCP_Server', 'CEM_NavisIAModeler', 'Services', 'MCPServer.cs'));

test('both halves use the same token file, in the user\'s settings folder', () => {
    const fileName = tokenSource.match(/FileName\s*=\s*"([^"]+)"/)?.[1];
    assert.ok(fileName, 'AccessToken.cs declares FileName');
    assert.match(tokenSource, /CemUserHome\.NavisworksSettingsFile\(\s*FileName\s*\)/);

    const clientPath = defaultTokenFile({ APPDATA: 'C:\\Users\\u\\AppData\\Roaming' });
    assert.equal(clientPath.replace(/\//g, '\\'),
        `C:\\Users\\u\\AppData\\Roaming\\Autodesk\\ApplicationPlugins\\CEM_NavisworksAPI.bundle\\Settings\\${fileName}`);
});

test('the token is random, written at start and removed at stop', () => {
    assert.match(tokenSource, /RNGCryptoServiceProvider|RandomNumberGenerator/);
    assert.match(httpSource, /AccessToken\.Issue\(/);
    assert.match(httpSource, /AccessToken\.Revoke\(/);
});

test('every request but the health check needs the token, checked before anything is routed', () => {
    const check = httpSource.indexOf('AccessToken.IsAuthorized(');
    assert.ok(check > 0, 'MCPServer checks the token');
    for (const later of ['path == "/manifest"', 'path == "/mcp_manifest.json"', '_router.Dispatch(']) {
        assert.ok(httpSource.indexOf(later) > check, `${later} comes after the token check`);
    }
    assert.ok(httpSource.indexOf('path == "/health"') < check, 'the health check stays open');
    assert.match(httpSource, /StatusCode\s*=\s*401|WriteJsonAsync\(\s*res\s*,\s*401/);
});

test('the token is compared in constant time', () => {
    assert.match(tokenSource, /FixedTimeEquals|diff\s*\|=/);
});
