import { minifyWorkflowDefinitionJson, prettifyWorkflowDefinitionJson } from './lowcode-workflow-json-form';

describe('json form helpers', () => {
  it('prettify then minify round-trip', () => {
    const raw = '{"steps":[{"type":"noop"}]}';
    const pretty = prettifyWorkflowDefinitionJson(raw);
    expect(pretty).toContain('\n');
    expect(minifyWorkflowDefinitionJson(pretty)).toBe(raw);
  });
});
