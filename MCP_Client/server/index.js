#!/usr/bin/env node

import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
    CallToolRequestSchema,
    ErrorCode,
    ListToolsRequestSchema,
    McpError,
} from '@modelcontextprotocol/sdk/types.js';
import { callRpc, awaitJob, JobPending, pendingText } from './navis-rpc.js'; // Cemengal: docs/decisions/0008



class NavisworksMCPServer {
    constructor() {
        this.server = new Server(
            { name: 'cem-navis-aimodeler', version: '1.0.0' }, // Cemengal: was waabe-navisworks-mcp
            { capabilities: { tools: {} } }
        );

        this.navisworksApiUrl = this.getNavisworksApiUrl();
        this.setupToolHandlers();
    }


    getNavisworksApiUrl() {
        const port = process.env.NAVISWORKS_API_PORT || '8765'; // Cemengal: the server's default too (was 1234)
        return `http://localhost:${port}`;
    }


    // Cemengal: through navis-rpc.js, which times the request out, follows a long one as a job, and
    // ends within the agent host's request timeout (docs/decisions/0008). Upstream's fetch had no
    // timeout and waited as long as Navisworks did.
    async rpc(method, params = {}) {
        return callRpc(this.navisworksApiUrl, method, params);
    }


    normalizeTokens(v) {
        if (Array.isArray(v)) return v.map(x => String(x ?? '').trim()).filter(Boolean);
        if (typeof v === 'string') {
            return v
                .split(/[,;\r\n]/g)
                .map(s => s.trim())
                .filter(Boolean);
        }
        return [];
    }


    toDelimitedString(v) {
        const arr = this.normalizeTokens(v);
        return arr.join(',');
    }


    setupToolHandlers() {

        this.server.setRequestHandler(ListToolsRequestSchema, async () => {
            return {
                tools: [
                    {
                        name: 'get_model_overview',
                        description: 'Provides an overview of the loaded models.',
                        inputSchema: { type: 'object', properties: {} }
                    },

                    {
                        name: 'get_element_count_by_category',
                        description: 'Counts elements of a category. scope="all" (default) or list of model canonical_id / model name (comma, semicolon, newline).',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                category: { type: 'string', description: 'e.g. IfcSpace, Doors, Windows' },
                                scope: { type: 'string', description: '"all" or list of IDs/names, separated by , ; or newlines', default: 'all' }
                            },
                            required: ['category']
                        }
                    },

                    {
                        name: 'get_property_distribution_by_category',
                        // Cemengal: the cost, so the agent can choose (docs/decisions/0008)
                        description: 'Overview of all models with property categories and counts. Reads every property of every item: on a large federated model it runs for minutes, as a job (see get_job_status).',
                        inputSchema: { type: 'object', properties: {} },
                        output_schema: {
                            type: 'object',
                            properties: {
                                category: { type: 'string' },
                                count: { type: 'integer' },
                                scope: { type: 'string' },
                                success: { type: 'boolean' },
                                message: { type: 'string' },
                                models: {
                                    type: 'array',
                                    items: {
                                        type: 'object',
                                        properties: {
                                            modelId: { type: 'string' },
                                            categories: {
                                                type: 'array',
                                                items: {
                                                    type: 'object',
                                                    properties: {
                                                        category: { type: 'string' },
                                                        properties: {
                                                            type: 'array',
                                                            items: {
                                                                type: 'object',
                                                                properties: {
                                                                    property: { type: 'string' },
                                                                    count: { type: 'integer' }
                                                                },
                                                                required: ['property', 'count']
                                                            }
                                                        }
                                                    },
                                                    required: ['category', 'properties']
                                                }
                                            }
                                        },
                                        required: ['modelId', 'categories']
                                    }
                                }
                            },
                            required: ['category', 'count', 'scope', 'success', 'models']
                        }
                    },

                    {
                        name: 'list_properties_for_item',
                        description: 'Determines properties for an item.',
                        inputSchema: {
                            type: 'object',
                            properties: { canonical_id: { type: 'string' } },
                            required: ['canonical_id']
                        }
                    },

                    {
                        name: 'list_items_to_property',
                        // Cemengal: scope is honoured and results are capped (docs/decisions/0008)
                        description: 'Lists items by category/property; optional model and value filters (substring, >=, <=, regex). modelFilter and scope accept IDs or model names; multiple tokens via , ; \\n. Only the scope\'s models are read: on a large model, give a scope. Returns at most maxResults items (default 200); truncated=true means there were more.',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                category: { type: 'string' },
                                property: { type: 'string' },
                                scope: { type: 'string', description: '"all" or list of model canonical_id / model name', default: 'all' },
                                modelFilter: {
                                    oneOf: [
                                        { type: 'string', description: 'A token or comma-separated list' },
                                        { type: 'array', items: { type: 'string' }, description: 'List of tokens' }
                                    ]
                                },
                                valueFilter: { type: 'string' },
                                ignoreCase: { type: 'boolean', default: true },
                                maxResults: { type: 'number', description: 'Default 200' }
                            },
                            required: ['category', 'property']
                        }
                    },

                    { name: 'clear_selection', description: 'Clears selection.', inputSchema: { type: 'object', properties: {} } },

                    { name: 'get_current_selection_snapshot', description: 'Current selection as list.', inputSchema: { type: 'object', properties: {} } },

                    {
                        name: 'apply_selection',
                        description: 'Sets selection. Input: canonical_id[].',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                canonical_id: { type: 'array', items: { type: 'string' } },
                                keepExistingSelection: { type: 'boolean', default: true }
                            },
                            required: ['canonical_id']
                        }
                    },
                    {
                        name: 'run_simple_clash',
                        // Cemengal: what a scope means now, and that a long run is a job (docs/decisions/0008)
                        description: 'Runs a simple hard clash between two scopes. Input: scopeA, scopeB, tolerance_m?, test_name? A scope is model canonical_ids (from get_model_overview), model file names or item ids; a model scope tests everything in the model. A long clash returns a job_id to follow with get_job_status.',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                scopeA: { type: 'string', description: '"all" or list of IDs/names (separated by , ; \\n)', default: 'all' },
                                scopeB: { type: 'string', description: '"all" or list of IDs/names (separated by , ; \\n)', default: 'all' },
                                tolerance_m: { type: 'number', description: 'Tolerance in meters', default: 0.01 },
                                test_name: { type: 'string', description: 'Display name of the test', default: 'MCP API Test' }
                            }
                        }
                    },
                    { name: 'get_units_and_tolerances', description: 'Length/area/volume units + tolerances.', inputSchema: { type: 'object', properties: {} } },

                    // Cemengal: the job tools (docs/decisions/0008). Navisworks runs one request at a
                    // time; these three answer at once, even while a job runs.
                    {
                        name: 'get_server_status',
                        description: 'Whether the AI Modeler is busy, with which job and for how long; whether Navisworks itself answers (ui_thread_responsive); the open document; the recent jobs. Answers at once, even while a job runs. Use it before assuming Navisworks has frozen.',
                        inputSchema: { type: 'object', properties: {} }
                    },
                    {
                        name: 'get_job_status',
                        description: 'Waits up to ~45 s for a job (named in a reply that said "still running") and returns its result when it ends, or its progress. Call it again while it is still running.',
                        inputSchema: {
                            type: 'object',
                            properties: { job_id: { type: 'string' } },
                            required: ['job_id']
                        }
                    },
                    {
                        name: 'cancel_job',
                        description: 'Stops a running job. A scan stops within a second; a clash test Navisworks is already computing stops when that test ends.',
                        inputSchema: {
                            type: 'object',
                            properties: { job_id: { type: 'string' } },
                            required: ['job_id']
                        }
                    },

                    // Cemengal: the escape hatch and the eyes (docs/decisions/0010), as CEM_RevitMCP's
                    // send_code_to_revit and take_screenshot.
                    {
                        name: 'send_code_to_navisworks',
                        description: 'Compiles and runs C# inside Navisworks, for what no other tool does. `code` is the body of '
                            + '`static object Execute(Document doc, string[] parameters, CancellationToken ct)`: doc is the active '
                            + 'Autodesk.Navisworks.Api.Document; in scope are System, System.Collections.Generic, System.Linq, System.Text, '
                            + 'System.Threading, Autodesk.Navisworks.Api, .Clash and .DocumentParts, plus the aliases ComApi '
                            + '(Autodesk.Navisworks.Api.Interop.ComApi) and ComApiBridge; using directives at the top of the code are '
                            + 'allowed. Statements only: local functions are fine, class declarations are not. '
                            + 'Return a value to get it back (numbers, strings, '
                            + 'lists, dictionaries, anonymous objects; Navisworks objects come back as text); Console.WriteLine output is '
                            + 'returned too. It runs on Navisworks\' UI thread and cannot pause: keep it short, scope loops to the models '
                            + 'you need, and check ct.IsCancellationRequested in long ones. Compile errors give line numbers in your code. '
                            + 'Prefer a dedicated tool when one exists.',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                code: { type: 'string', description: 'C# statements: the body of Execute' },
                                parameters: { type: 'array', items: { type: 'string' }, description: 'Passed to the code as parameters' }
                            },
                            required: ['code']
                        }
                    },
                    {
                        name: 'take_screenshot',
                        description: 'An image of Navisworks, to see the result of an action. mode "view" (default) renders the 3D view '
                            + 'as it is now, without the user interface; "window" captures the whole Navisworks window, panels and '
                            + 'dialogs included. max_size: the longest side in pixels (default 1600).',
                        inputSchema: {
                            type: 'object',
                            properties: {
                                mode: { type: 'string', enum: ['view', 'window'], default: 'view' },
                                max_size: { type: 'number', default: 1600 }
                            }
                        }
                    },
                ],
            };
        });


        this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
            const { name, arguments: args } = request.params;

            try {
                switch (name) {

                    case 'get_model_overview': return await this.t_model_overview();
                    case 'get_units_and_tolerances': return await this.t_simple('get_units_and_tolerances');
                    case 'get_property_distribution_by_category': return await this.t_property_distribution_by_category();

                    case 'list_items_to_property': {
                        const category = typeof args?.category === 'string' ? args.category.trim() : '';
                        const property = typeof args?.property === 'string' ? args.property.trim() : '';
                        if (!category) throw new McpError(ErrorCode.InvalidParams, 'category fehlt oder ist leer.');
                        if (!property) throw new McpError(ErrorCode.InvalidParams, 'property fehlt oder ist leer.');


                        const scopeRaw = (typeof args?.scope === 'string' && args.scope.trim()) ? args.scope.trim() : 'all';
                        const scope = scopeRaw === 'all' ? 'all' : this.toDelimitedString(scopeRaw);
                        const modelFilter = (args?.modelFilter !== undefined) ? this.toDelimitedString(args.modelFilter) : undefined;

                        const valueFilter = typeof args?.valueFilter === 'string' ? args.valueFilter : undefined;
                        const ignoreCase = typeof args?.ignoreCase === 'boolean' ? args.ignoreCase : true;
                        const maxResults = (typeof args?.maxResults === 'number' && isFinite(args.maxResults)) ? args.maxResults : undefined;

                        return await this.t_list_items_to_property({ category, property, scope, modelFilter, valueFilter, ignoreCase, maxResults });
                    }

                    case 'get_element_count_by_category': {
                        const cat = typeof args?.category === 'string' ? args.category.trim() : '';
                        if (!cat) throw new McpError(ErrorCode.InvalidParams, 'category fehlt oder ist leer.');
                        const scopeRaw = (typeof args?.scope === 'string' && args.scope.trim()) ? args.scope.trim() : 'all';
                        const scope = scopeRaw === 'all' ? 'all' : this.toDelimitedString(scopeRaw);
                        return await this.t_count_by_category(cat, scope);
                    }

                    case 'list_properties_for_item': {
                        const id = args?.item_id ?? args?.canonical_id; // fallback für altes schema
                        return await this.t_list_properties_for_item(id);
                    };


                    case 'clear_selection': return await this.t_simple('clear_selection');
                    case 'get_current_selection_snapshot': return await this.t_simple('get_current_selection_snapshot');

                    case 'apply_selection': {
                        const ids = Array.isArray(args?.canonical_id) ? args.canonical_id : [];
                        if (ids.length === 0 || !ids.every(v => typeof v === 'string')) {
                            throw new McpError(ErrorCode.InvalidParams, 'canonical_id[] (string) erforderlich.');
                        }
                        const keep = (typeof args?.keepExistingSelection === 'boolean') ? args.keepExistingSelection : true;
                        return await this.t_apply_selection(ids, keep);
                    };

                    case 'run_simple_clash': {
                        const scopeAraw = (typeof args?.scopeA === 'string') ? args.scopeA : 'all';
                        const scopeBraw = (typeof args?.scopeB === 'string') ? args.scopeB : 'all';
                        const tolerance_m = (typeof args?.tolerance_m === 'number' && isFinite(args.tolerance_m)) ? args.tolerance_m : 0.01;
                        const test_name = (typeof args?.test_name === 'string' && args.test_name.trim()) ? args.test_name.trim() : 'MCP API Test';

                        return await this.t_run_simple_clash(scopeAraw, scopeBraw, tolerance_m, test_name);
                    };

                    // Cemengal: the job tools (docs/decisions/0008)
                    case 'get_server_status': return await this.t_simple('get_server_status');
                    case 'get_job_status': return await this.t_job_status(this.requireJobId(args));
                    case 'cancel_job': return await this.t_cancel_job(this.requireJobId(args));

                    // Cemengal: docs/decisions/0010
                    case 'send_code_to_navisworks': {
                        const code = typeof args?.code === 'string' ? args.code : '';
                        if (!code.trim()) throw new McpError(ErrorCode.InvalidParams, 'code is missing or empty.');
                        const parameters = Array.isArray(args?.parameters) ? args.parameters.map(p => String(p ?? '')) : [];
                        return await this.t_send_code(code, parameters);
                    }
                    case 'take_screenshot': {
                        const mode = args?.mode === 'window' ? 'window' : 'view';
                        const max_size = (typeof args?.max_size === 'number' && isFinite(args.max_size)) ? args.max_size : 1600;
                        return await this.t_screenshot(mode, max_size);
                    }

                    default:
                        throw new McpError(ErrorCode.MethodNotFound, `Unknown tool: ${name}`);
                }
            } catch (error) {
                // Cemengal: a request still running in Navisworks is an answer, not an error: the agent
                // gets the job id to wait on (docs/decisions/0008).
                if (error instanceof JobPending) return { content: [{ type: 'text', text: pendingText(error.job) }] };
                if (error instanceof McpError) throw error;
                throw new McpError(ErrorCode.InternalError, `Error occurred while executing tool: ${error.message}`);
            }
        });
    }


    async t_list_items_to_property(a) {
        const dto = await this.rpc('list_items_to_property', {
            Category: a.category,
            Property: a.property,
            Scope: a.scope,
            ModelFilter: a.modelFilter,
            ValueFilter: a.valueFilter,
            IgnoreCase: a.ignoreCase,
            MaxResults: a.maxResults
        });
        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(dto, null, 2) + '\n```' }] };
    }

    async t_simple(method) {
        const data = await this.rpc(method, {});
        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(data, null, 2) + '\n```' }] };
    }

    async t_model_overview() {
        const overview = await this.rpc('get_model_overview', {});

        const modelsCount = overview?.ModelsCount ?? 0;
        const total = overview?.TotalElements ?? 0;
        const docTitle = overview?.DocumentTitle || 'Unbenannt';

        const details = Array.isArray(overview?.Models) ? overview.Models : [];
        const hist = overview?.categories_histogram || {};

        const modelLines = details.map((m, i) => {
            const model_name = m?.FileName || m?.DisplayName || '(unbenannt)';
            const typ = m?.SourceFileName || '';     // ".ifc", ".rvt", ...
            const cid = m?.canonical_id || '';
            const kids = Number.isFinite(m?.ChildrenCount) ? m.ChildrenCount : 0;
            const desc = Number.isFinite(m?.DescendantsCount) ? m.DescendantsCount : 0;
            const parent_canonical_id = m?.perent_canonical_id || '';
            return `- ${i + 1}. ${model_name}${typ ? ` ${typ}` : ''}${cid ? ` — canonical_id: ${cid}` : ''}${parent_canonical_id ? ` parent_canonical_id: ${parent_canonical_id}` : ''} (direct_children: ${kids}, node_count_including_self: ${desc})`;
        });

        const histLines = Object.keys(hist).map(k => `- ${k}: ${hist[k]}`);

        const jsonModels = details.map(m => ({
            canonical_id: m?.canonical_id ?? null,
            parent_canonical_id: m?.perent_canonical_id ?? 'xx',
            name: m?.FileName || m?.DisplayName || null,
            type: m?.SourceFileName || null,
            direct_children: Number.isFinite(m?.ChildrenCount) ? m.ChildrenCount : 0,
            parent_including_self: Number.isFinite(m?.DescendantsCount) ? m.DescendantsCount : 0,
        }));

        const jsonStr = JSON.stringify(
            { modelsCount, total, document: docTitle, Models: jsonModels },
            null,
            2
        );

        const txt = `\n\`\`\`json\n${jsonStr}\n\`\`\``;
        return { content: [{ type: 'text', text: txt }] };
    }

    async t_property_distribution_by_category() {
        const dto = await this.rpc('get_property_distribution_by_category', {});

        let details = null;
        try {
            details = typeof dto?.details === 'string' ? JSON.parse(dto.details) : (dto?.details ?? null);
        } catch {
            details = null;
        }

        const models = details
            ? Object.entries(details).map(([modelId, categories]) => ({
                modelId,
                categories: Object.entries(categories).map(([categoryName, props]) => ({
                    category: categoryName,
                    properties: Object.entries(props).map(([property, count]) => ({
                        property,
                        count: typeof count === 'number' ? count : Number(count) || 0,
                    })),
                })),
            }))
            : [];

        const payload = {
            category: dto?.category ?? '(all)',
            count: Number(dto?.count ?? 0),
            scope: dto?.scope ?? 'all',
            success: Boolean(dto?.success ?? true),
            models,
        };

        return {
            content: [
                { type: 'text', text: '```json\n' + JSON.stringify(payload, null, 2) + '\n```' },
            ],
        };
    }

    async t_count_by_category(category, scope = 'all') {
        if (!category) throw new McpError(ErrorCode.InvalidParams, 'category fehlt.');
        const dto = await this.rpc('get_element_count_by_category', { category, scope });
        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(dto, null, 2) + '\n```' }] };
    }

    async t_list_properties_for_item(item_id) {
        if (!item_id) throw new McpError(ErrorCode.InvalidParams, 'item_id fehlt.');
        const dto = await this.rpc('list_properties_for_item', { item_id });
        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(dto, null, 2) + '\n```' }] };
    }

    async t_apply_selection(canonicalIds = [], keepExistingSelection = true) {
        if (!Array.isArray(canonicalIds) || canonicalIds.length === 0) {
            throw new McpError(ErrorCode.InvalidParams, 'canonical_id[] erforderlich.');
        }

        const dto = await this.rpc('apply_selection', {
            canonical_id: canonicalIds,
            keepExistingSelection
        });

        return {
            content: [{ type: 'text', text: '```json\n' + JSON.stringify(dto, null, 2) + '\n```' }]
        };
    }

    async t_run_simple_clash(scopeAraw, scopeBraw, tolerance_m, test_name) {
        const scopeA = (scopeAraw && scopeAraw.trim() !== '' && scopeAraw.trim() !== 'all')
            ? this.toDelimitedString(scopeAraw)
            : 'all';
        const scopeB = (scopeBraw && scopeBraw.trim() !== '' && scopeBraw.trim() !== 'all')
            ? this.toDelimitedString(scopeBraw)
            : 'all';

        const dto = await this.rpc('run_simple_clash', {
            scopeA,
            scopeB,
            tolerance_m,
            test_name
        });

        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(dto, null, 2) + '\n```' }] };
    }

    // Cemengal: the job tools (docs/decisions/0008).
    requireJobId(args) {
        const id = typeof args?.job_id === 'string' ? args.job_id.trim() : '';
        if (!id) throw new McpError(ErrorCode.InvalidParams, 'job_id is missing or empty.');
        return id;
    }

    async t_job_status(job_id) {
        const { status, data } = await awaitJob(this.navisworksApiUrl, job_id);
        const secs = Math.round((status.elapsed_ms ?? 0) / 1000);
        const head = `Job ${status.job_id} (${status.method}) finished after ${secs} s.`;
        return { content: [{ type: 'text', text: head + '\n```json\n' + JSON.stringify(data, null, 2) + '\n```' }] };
    }

    async t_cancel_job(job_id) {
        const status = await this.rpc('cancel_job', { job_id });
        return { content: [{ type: 'text', text: '```json\n' + JSON.stringify(status, null, 2) + '\n```' }] };
    }

    // Cemengal: docs/decisions/0010.
    async t_send_code(code, parameters) {
        const dto = await this.rpc('send_code_to_navisworks', { code, parameters });
        let text = `Ran in ${dto?.run_ms ?? '?'} ms (compiled in ${dto?.compile_ms ?? '?'} ms). Result (${dto?.result_type || 'null'}):\n`
            + '```json\n' + (dto?.result_json ?? 'null') + '\n```';
        const output = (dto?.output ?? '').replace(/\s+$/, '');
        if (output) text += '\nConsole output:\n```\n' + output + '\n```';
        if (dto?.warnings?.length) text += '\nCompiler warnings:\n' + dto.warnings.join('\n');
        return { content: [{ type: 'text', text }] };
    }

    async t_screenshot(mode, max_size) {
        const dto = await this.rpc('take_screenshot', { mode, max_size });
        return {
            content: [
                { type: 'image', data: dto.png_base64, mimeType: 'image/png' },
                { type: 'text', text: `Navisworks ${dto.mode}, ${dto.width} x ${dto.height} px${dto.document_title ? `, ${dto.document_title}` : ''}.` },
            ],
        };
    }


    async run() {
        const transport = new StdioServerTransport();
        await this.server.connect(transport);

        process.on('SIGINT', async () => {
            await this.server.close();
            process.exit(0);
        });
    }
}


const server = new NavisworksMCPServer();
server.run().catch(console.error);
