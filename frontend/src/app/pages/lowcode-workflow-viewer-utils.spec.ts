import {
  buildWorkflowViewerStepCards,
  findCaretIndexForWorkflowStep,
} from './lowcode-workflow-viewer-utils';

describe('buildWorkflowViewerStepCards', () => {
  it('returns empty for non-array', () => {
    expect(buildWorkflowViewerStepCards(null)).toEqual([]);
    expect(buildWorkflowViewerStepCards({})).toEqual([]);
  });

  it('maps delay, require, domainCommand, foreach, switch', () => {
    const steps = [
      { type: 'delay', ms: 250, timeoutMs: 50 },
      { type: 'require', path: '000.entityRecordId' },
      { type: 'domainCommand', command: 'entityRecord.updateById', recordId: '${000.x}' },
      { type: 'foreach', items: '000.items', do: { type: 'map', mappings: { a: 'b' } } },
      { type: 'switch', value: '000.kind', cases: [{ when: 'a', do: { type: 'noop' } }], default: { type: 'noop' } },
    ];
    const cards = buildWorkflowViewerStepCards(steps);
    expect(cards.map(c => c.stepKey)).toEqual(['000', '001', '002', '003', '004']);
    expect(cards[0].title).toBe('delay');
    expect(cards[0].subtitle).toContain('250');
    expect(cards[1].subtitle).toContain('path:');
    expect(cards[2].subtitle).toContain('entityRecord.updateById');
    expect(cards[3].branchPreview).toContain('map');
    expect(cards[4].branchPreview).toContain('case');
  });

  it('shows retry summary on step cards', () => {
    const steps = [{ type: 'noop', retry: { maxAttempts: 3, delayMs: 100, backoffFactor: 2, maxDelayMs: 500 } }];
    const cards = buildWorkflowViewerStepCards(steps);
    expect(cards[0].subtitle).toContain('retry');
    expect(cards[0].subtitle).toContain('max 3');
    expect(cards[0].subtitle).toContain('100 ms');
  });
});

describe('findCaretIndexForWorkflowStep', () => {
  it('returns -1 for invalid JSON', () => {
    expect(findCaretIndexForWorkflowStep('{', 0)).toBe(-1);
  });

  it('finds substring index for a step object', () => {
    // Must match exact JSON.stringify output so indexOf finds the step blob.
    const defObj = {
      steps: [
        { type: 'noop' },
        { type: 'delay', ms: 99 },
      ],
    };
    const def = JSON.stringify(defObj);
    const i = findCaretIndexForWorkflowStep(def, 1);
    expect(i).toBeGreaterThanOrEqual(0);
    expect(def.slice(i, i + 24)).toContain('delay');
  });
});
