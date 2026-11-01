// Cemengal: logging never throws (QA-10, docs/decisions/0011). CEM_CSharpExtended's CemTempFolder refuses
// to write through a junction in %TEMP%\Cemengal (SEC-8): AppendLog answers false, but LogFile throws
// CemTempFolderUnsafeException (an IOException). LogHelper asks LogFile for the path when a write fails,
// so an unguarded call there made every log line throw, startup included. Read from the source, since
// the C# side has no unit tests.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const repo = join(here, '..', '..', '..');
const source = readFileSync(join(repo, 'MCP_Server', 'CEM_NavisIAModeler_Shared', 'LogHelper.cs'), 'utf8')
    .split(/\r?\n/).map(line => line.replace(/(^|[^:"])\/\/.*$/, '$1')).join('\n');

/** The body of the method declared by <signature>, braces balanced. */
function body(signature) {
    const start = source.indexOf(signature);
    assert.ok(start >= 0, `${signature} not found: update this test`);
    const open = source.indexOf('{', start);
    for (let i = open, depth = 0; i < source.length; i++) {
        if (source[i] === '{') depth++;
        else if (source[i] === '}' && --depth === 0) return source.slice(open + 1, i);
    }
    assert.fail(`${signature} has no closing brace`);
}

test('GetCurrentLogPath catches the refused junction and an invalid name instead of throwing', () => {
    const code = body('public static string GetCurrentLogPath()');
    assert.match(code, /try\s*\{[^}]*CemTempFolder\.LogFile\(/, 'CemTempFolder.LogFile is called inside a try');
    assert.match(code, /catch\s*\(\s*IOException\b/, 'an IOException (CemTempFolderUnsafeException included) is caught');
    assert.match(code, /catch\s*\(\s*ArgumentException\b/, 'an ArgumentException is caught');
});

test('HandleLogError writes nothing that can throw: it reads the path only through GetCurrentLogPath', () => {
    const code = body('private static void HandleLogError(');
    assert.doesNotMatch(code, /CemTempFolder\./, 'HandleLogError calls CemTempFolder directly');
});

test('LogStartup calls nothing outside a try', () => {
    const code = body('public static void LogStartup()');
    let outside = code;
    for (let i; (i = outside.search(/\btry\s*\{/)) >= 0;) {
        const open = outside.indexOf('{', i);
        let depth = 0, end = open;
        for (; end < outside.length; end++) {
            if (outside[end] === '{') depth++;
            else if (outside[end] === '}' && --depth === 0) break;
        }
        const rest = outside.slice(end + 1).replace(/^\s*(catch(\s*\([^)]*\))?\s*\{[^{}]*\}\s*)+/, '');
        outside = outside.slice(0, i) + rest;
    }
    assert.doesNotMatch(outside, /\w+\s*\(/, `LogStartup calls outside a try: ${outside.trim()}`);
});
