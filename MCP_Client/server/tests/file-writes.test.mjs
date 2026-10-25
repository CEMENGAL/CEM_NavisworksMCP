// Cemengal: every file the AI Modeler and this client write, accounted for (root decision 0031,
// docs/decisions/0011). The menus load from a shared ACC folder that Desktop Connector syncs to every
// colleague, so nothing of ours writes under ACC or next to its own DLL; logs and temporary files go to
// CEM_CSharpExtended's CemTempFolder (%TEMP%\Cemengal\NavisworksMCP, pruned on its own); per-user
// settings and state stay in CemUserHome (%APPDATA%). Read from the sources, since the C# side has no
// unit tests: a new file write fails here until it is classified.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, relative, sep } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const read = path => readFileSync(path, 'utf8');
const name = path => relative(repo, path).split(sep).join('/');

/** Every file under a folder with one of the extensions, build output and packages left out. */
function filesUnder(folder, extensions) {
    const found = [];
    for (const entry of readdirSync(folder, { withFileTypes: true })) {
        if (['bin', 'obj', 'node_modules', 'tests', '.vs'].includes(entry.name)) continue;
        const path = join(folder, entry.name);
        if (entry.isDirectory()) found.push(...filesUnder(path, extensions));
        else if (extensions.some(ext => entry.name.endsWith(ext))) found.push(path);
    }
    return found;
}

/** The code without its line comments (C#, JS) and XML comments (csproj, props, targets). */
const live = source => source
    .replace(/<!--[\s\S]*?-->/g, '')
    .split(/\r?\n/).map(line => line.replace(/(^|[^:"])\/\/.*$/, '$1')).join('\n');

const csharp = filesUnder(join(repo, 'MCP_Server'), ['.cs']).map(path => ({ path, code: live(read(path)) }));
const msbuild = filesUnder(join(repo, 'MCP_Server'), ['.csproj', '.props', '.targets', '.xml', '.config']);
const client = filesUnder(join(repo, 'MCP_Client', 'server'), ['.js']);

// A raw file-system write in C#. Save/Emit count unless they write into a MemoryStream (see below).
const CS_WRITE = /\bFile\.(WriteAll\w*|AppendAll\w*|AppendText|Create\w*|Copy|Move|Replace|Delete|Open|OpenWrite|SetAttributes)\s*\(|\bnew\s+(FileStream|StreamWriter)\s*\(|\bDirectory\.(CreateDirectory|Delete|Move)\s*\(/;

/** The `x.Save(arg` / `x.Emit(arg` calls whose first argument is not a MemoryStream of the same file. */
function saveToDisk(code) {
    const calls = [...code.matchAll(/\.(Save|Emit)\(\s*([A-Za-z_]\w*|"[^"]*")/g)].map(m => m[2]);
    return calls.filter(arg => !new RegExp(`\\b${arg}\\s*=\\s*new\\s+MemoryStream\\(`).test(code));
}

const writers = csharp.filter(({ code }) => CS_WRITE.test(code) || saveToDisk(code).length > 0);

test('no shipped source names an ACC (Desktop Connector) path', () => {
    const sources = [...csharp.map(f => f.path), ...msbuild, ...client, join(repo, 'MCP_Client', 'manifest.json')];
    for (const path of sources) {
        assert.doesNotMatch(live(read(path)), /ACCDocs|\b[A-Za-z]:[\\/]+DC[\\/]/i, `${name(path)} names an ACC path`);
    }
});

test('the C# files that write are exactly the per-user settings and the access token', () => {
    assert.deepEqual(writers.map(f => name(f.path)).sort(), [
        'MCP_Server/CEM_NavisIAModeler/Infrastructure/AccessToken.cs',
        'MCP_Server/CEM_NavisIAModeler/Services/SettingsManager.cs',
    ], 'a new file write: classify it (log, temp artefact, settings) and route it through CemTempFolder or CemUserHome');
});

test('those writers resolve their path in CemUserHome, never from the DLL folder or the temp folder', () => {
    for (const { path, code } of writers) {
        assert.match(code, /CemUserHome\.NavisworksSettingsFile\(/, `${name(path)} writes outside CemUserHome`);
        assert.doesNotMatch(code, /Assembly\.\w+\(\)\.(Location|CodeBase)|\.Location\b|BaseDirectory|GetDirectoryName\(|GetTempPath|SpecialFolder|GetEnvironmentVariable/,
            `${name(path)} derives a write path itself`);
    }
});

test('the screenshot and the compiled script are written into memory only', () => {
    for (const file of ['ScreenCapture.cs', 'CodeRunner.cs']) {
        const { path, code } = csharp.find(f => f.path.endsWith(sep + file));
        assert.match(code, /\.(Save|Emit)\(/, `${file} no longer saves or emits: update this test`);
        assert.deepEqual(saveToDisk(code), [], `${name(path)} saves to disk`);
    }
});

test('temp files only through CemTempFolder: no hand-made temp path in the AI Modeler', () => {
    for (const { path, code } of csharp) {
        assert.doesNotMatch(code, /GetTempPath|SpecialFolder\.(LocalApplicationData|MyDocuments)|%TEMP%|"TEMP"|"TMP"/, `${name(path)} builds a temp path itself`);
    }
});

test('the log is a daily file in %TEMP%\\Cemengal\\NavisworksMCP, through CemTempFolder', () => {
    const { code } = csharp.find(f => f.path.endsWith(sep + 'LogHelper.cs'));
    assert.match(code, /CemTempFolder\.AppendLog\(\s*App\s*,\s*Name\s*,/);
    assert.match(code, /App\s*=\s*"NavisworksMCP"/);
    assert.match(code, /Name\s*=\s*"CEM_NavisIAModeler"/);
    assert.match(code, /CemTempFolder\.LogFile\(\s*App\s*,\s*Name\s*\)/, 'GetCurrentLogPath names the same file');
    assert.doesNotMatch(code, /CemUserHome\.NavisworksFile\(/, 'a log is not per-user state: it does not go to roaming AppData');
});

test('settings never fall back to temp: without CemUserHome they stay in memory', () => {
    const { code } = csharp.find(f => f.path.endsWith(sep + 'SettingsManager.cs'));
    const resolve = code.slice(code.indexOf('static string GetSettingsPath()'), code.indexOf('static void CreateDefaultSettings('));
    assert.match(resolve, /catch\s*\(Exception ex\)[\s\S]*return null;/, 'GetSettingsPath answers null when CemUserHome fails');
    assert.match(code, /SettingsPath == null/, 'the getters and setters handle the in-memory case');
});

test('the Node client writes no file', () => {
    const READS = new Set(['readFileSync', 'readFile', 'existsSync', 'statSync', 'stat', 'readdirSync', 'readdir']);
    for (const path of client) {
        const code = live(read(path));
        assert.doesNotMatch(code, /import\s+(\*\s+as\s+)?\w+\s+from\s+['"](node:)?fs(\/promises)?['"]|require\(\s*['"](node:)?fs/, `${name(path)} imports the whole fs module`);
        for (const m of code.matchAll(/import\s*\{([^}]*)\}\s*from\s*['"](node:)?fs(\/promises)?['"]/g)) {
            for (const fn of m[1].split(',').map(s => s.trim().split(/\s+as\s+/)[0]).filter(Boolean)) {
                assert.ok(READS.has(fn), `${name(path)} imports ${fn} from fs`);
            }
        }
    }
});

test('the standalone bundle ships CEM_CSharpExtended.dll beside the AI Modeler', () => {
    const ribbon = read(join(repo, 'MCP_Server', 'CEM_NavisIAModeler_Ribbon', 'CEM_NavisIAModeler_Ribbon.csproj'));
    assert.match(ribbon, /<BundleContentFile Include="[^"]*\$\(TargetDir\)CEM_CSharpExtended\.dll/);
});
