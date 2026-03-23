import {
  buildContextVarSuggestionsFromDefinitionJson,
  buildContextVarSuggestionsFromSteps,
  buildMergedContextVarSuggestions,
  domainCommandOutputKeys,
} from './lowcode-workflow-context-suggestions';

describe('domainCommandOutputKeys', () => {
  it('maps known commands', () => {
    expect(domainCommandOutputKeys('entityRecord.createByEntityName')).toContain('entityRecordId');
    expect(domainCommandOutputKeys('entityRecord.upsertByEntityName')).toContain('action');
  });
});

describe('buildContextVarSuggestionsFromSteps', () => {
  it('includes domainCommand outputs and foreach helpers', () => {
    const steps = [
      { type: 'domainCommand', command: 'entityRecord.createByEntityName' },
      {
        type: 'foreach',
        items: '000.items',
        do: { type: 'map', mappings: { out: 'item.x' } },
      },
    ];
    const s = buildContextVarSuggestionsFromSteps(steps);
    expect(s).toContain('000.entityRecordId');
    expect(s).toContain('foreach.index');
    expect(s).toContain('001.000');
    expect(s).toContain('001.000.out');
  });

  it('includes switch branch inner outputs', () => {
    const steps = [
      { type: 'noop' },
      {
        type: 'switch',
        value: '000.x',
        cases: [{ when: 1, do: { type: 'map', mappings: { outA: '1' } } }],
        default: { type: 'set', output: { defOut: 'x' } },
      },
    ];
    const s = buildContextVarSuggestionsFromSteps(steps);
    expect(s).toContain('001.branch');
    expect(s).toContain('001.branch.outA');
    expect(s).toContain('001.branch.defOut');
  });
});

describe('buildMergedContextVarSuggestions', () => {
  it('always includes static foreach hints plus parsed steps', () => {
    const j = JSON.stringify({ steps: [{ type: 'noop' }] });
    const m = buildMergedContextVarSuggestions(j);
    expect(m).toContain('foreach.index');
    expect(m).toContain('foreach.item');
    expect(m).toContain('000');
  });
});

describe('buildContextVarSuggestionsFromDefinitionJson', () => {
  it('returns [] on invalid json', () => {
    expect(buildContextVarSuggestionsFromDefinitionJson('{')).toEqual([]);
  });
});
