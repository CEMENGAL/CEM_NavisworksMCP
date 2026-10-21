// Cemengal: send_code_to_navisworks and take_screenshot (docs/decisions/0010), read from the C#
// sources, since the C# side has no unit tests. The code tool compiles with Roslyn, which needs the
// bundle's binding redirects first, and must not persist the scripts it runs (as CEM_RevitMCP's 0023).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const server = join(here, '..', '..', '..', 'MCP_Server', 'CEM_NavisIAModeler');
const read = (...parts) => readFileSync(join(server, ...parts), 'utf8');
const live = source => source.split(/\r?\n/).filter(line => !line.trim().startsWith('//')).join('\n');

const controller = live(read('Controllers', 'CodeController.cs'));
const runner = live(read('Services', 'Code', 'CodeRunner.cs'));

test('the redirects are installed before Roslyn is touched', () => {
    assert.match(controller, /DependencyResolver\.Install\(\)/);
    assert.doesNotMatch(controller, /Microsoft\.CodeAnalysis/, 'the controller method must not make the JIT load Roslyn');
    assert.match(runner, /Microsoft\.CodeAnalysis/);
});

test('a script is compiled off the UI thread and run on it', () => {
    const compile = controller.indexOf('CodeRunner.Compile(');
    const run = controller.indexOf('UiThread.InvokeAsync(');
    assert.ok(compile > 0 && run > compile, 'compile first, then post the run to the UI thread');
});

test('the scripts themselves are never logged or written', () => {
    for (const [name, source] of [['CodeController', controller], ['CodeRunner', runner]]) {
        assert.doesNotMatch(source, /LogHelper\.\w+\([^;]*\{(args\.)?code\b/, `${name} logs the code`);
        assert.doesNotMatch(source, /File\.Write/, `${name} writes a file`);
    }
});

test('the screenshot returns a bounded PNG and writes nothing', () => {
    const capture = live(read('Services', 'Backends', 'ScreenCapture.cs'));
    assert.match(capture, /GenerateImage\(/);
    assert.match(capture, /ImageFormat\.Png/);
    assert.match(capture, /MaxSide/);
    assert.doesNotMatch(capture, /File\.Write|\.Save\(\s*"/);
});
