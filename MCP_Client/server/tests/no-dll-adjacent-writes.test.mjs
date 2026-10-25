// Cemengal: the Navisworks menu lives in a shared ACC folder, so the add-in must never write next to
// its DLL (docs/decisions/0007). The manifest is served from memory and is not written to disk;
// settings go to the per-user home and the log to the local temp folder (0011), never derived from
// the DLL folder. file-writes.test.mjs accounts for every other write.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const read = (...parts) => readFileSync(join(repo, ...parts), 'utf8');
const server = (...parts) => read('MCP_Server', ...parts);

test('the manifest is not written to disk', () => {
    const builder = server('CEM_NavisIAModeler', 'Infrastructure', 'ManifestBuilder.cs');
    assert.doesNotMatch(builder, /TryWriteToDisk|GetDefaultContentDirectory|File\.Write/);
    const http = server('CEM_NavisIAModeler', 'Services', 'MCPServer.cs');
    assert.doesNotMatch(http, /TryWriteToDisk/);
});

test('the standalone bundle does not ship a written mcp_manifest.json', () => {
    assert.doesNotMatch(server('CEM_NavisIAModeler_Ribbon', 'CEM_NavisIAModeler_Ribbon.csproj'), /BundleContentFile[^>]*mcp_manifest\.json/);
});

test('settings resolve through CemUserHome and the log through CemTempFolder, never from the DLL folder', () => {
    const settings = server('CEM_NavisIAModeler', 'Services', 'SettingsManager.cs');
    const log = server('CEM_NavisIAModeler_Shared', 'LogHelper.cs');
    assert.match(settings, /CemUserHome\.NavisworksSettingsFile\(/);
    assert.match(log, /CemTempFolder\.AppendLog\(/); // root 0031, file-writes.test.mjs
    for (const [name, src] of [['SettingsManager', settings], ['LogHelper', log]]) {
        assert.doesNotMatch(src.replace(/\/\/.*$/gm, ''), /GetDirectoryName\(|SpecialFolder\.MyDocuments|ApplicationPlugins/, `${name} derives a path`);
    }
});
