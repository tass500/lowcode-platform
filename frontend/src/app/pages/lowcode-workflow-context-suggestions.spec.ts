import {
  buildContextVarSuggestionsFromDefinitionJson,
  buildContextVarSuggestionsFromSteps,
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
});

describe('buildContextVarSuggestionsFromDefinitionJson', () => {
  it('returns [] on invalid json', () => {
    expect(buildContextVarSuggestionsFromDefinitionJson('{')).toEqual([]);
  });
});
