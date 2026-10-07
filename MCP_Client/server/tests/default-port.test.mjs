// Cemengal: the two halves must agree on the default port (docs/decisions/0006). Upstream's server
// suggested 8080 and its client 1234, so an out-of-the-box install could not connect; 8080 is also
// CEM_RevitMCP's Revit socket port. Every default-port literal on both sides must be the same value.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const read = (...parts) => readFileSync(join(repo, ...parts), 'utf8');

export const EXPECTED = 8765;

/** Every default port each file declares, as numbers. */
function defaults() {
    const found = [];
    const add = (where, re, source) => {
        const hits = [...source.matchAll(re)].map(m => Number(m[1]));
        assert.ok(hits.length > 0, `no default port found in ${where}`);
        hits.forEach(port => found.push({ where, port }));
    };
    add('client index.js', /NAVISWORKS_API_PORT\s*\|\|\s*'(\d+)'/g, read('MCP_Client', 'server', 'index.js'));
    add('client manifest.json default', /"default":\s*"(\d+)"/g, read('MCP_Client', 'manifest.json'));
    add('server SettingsManager <Port>', /<Port>(\d+)<\/Port>/g, read('MCP_Server', 'CEM_NavisIAModeler', 'Services', 'SettingsManager.cs'));
    add('server SettingsManager fallback', /return\s+(\d{4,5})\s*;/g, read('MCP_Server', 'CEM_NavisIAModeler', 'Services', 'SettingsManager.cs'));
    add('server MCPServiceConnection', /DefaultPort\s*=\s*(\d+)/g, read('MCP_Server', 'CEM_NavisIAModeler', 'Core', 'MCPServiceConnection.cs'));
    return found;
}

test('every default port on both sides is the same', () => {
    const wrong = defaults().filter(d => d.port !== EXPECTED);
    assert.deepEqual(wrong, []);
});

test('the default port is not CEM_RevitMCP\'s 8080', () => {
    assert.notEqual(EXPECTED, 8080);
});
