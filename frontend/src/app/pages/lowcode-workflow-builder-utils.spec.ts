import {
  appendBuilderStep,
  buildMinimalStep,
  moveBuilderStep,
  moveBuilderStepToSlot,
  mutateWorkflowDefinitionSteps,
  parseBuilderStepSummaries,
  removeBuilderStepAt,
} from './lowcode-workflow-builder-utils';

describe('lowcode-workflow-builder-utils', () => {
  it('buildMinimalStep covers noop and delay', () => {
    expect(buildMinimalStep('noop')).toEqual({ type: 'noop' });
    expect(buildMinimalStep('delay')).toEqual({ type: 'delay', ms: 100 });
  });

  it('mutateWorkflowDefinitionSteps preserves extra top-level keys', () => {
    const raw = JSON.stringify({ foo: 1, steps: [{ type: 'noop' }] });
    const next = mutateWorkflowDefinitionSteps(raw, s => [...s, { type: 'delay', ms: 1 }]);
    const parsed = JSON.parse(next) as { foo: number; steps: unknown[] };
    expect(parsed.foo).toBe(1);
    expect(parsed.steps.length).toBe(2);
  });

  it('appendBuilderStep appends', () => {
    const raw = '{"steps":[]}';
    const next = appendBuilderStep(raw, 'noop');
    const p = JSON.parse(next) as { steps: { type: string }[] };
    expect(p.steps.length).toBe(1);
    expect(p.steps[0].type).toBe('noop');
  });

  it('removeBuilderStepAt removes by index', () => {
    const raw = '{"steps":[{"type":"a"},{"type":"b"}]}';
    const next = removeBuilderStepAt(raw, 0);
    const p = JSON.parse(next) as { steps: { type: string }[] };
    expect(p.steps.length).toBe(1);
    expect(p.steps[0].type).toBe('b');
  });

  it('moveBuilderStep reorders', () => {
    const raw = '{"steps":[{"type":"a"},{"type":"b"},{"type":"c"}]}';
    const next = moveBuilderStep(raw, 2, 0);
    const p = JSON.parse(next) as { steps: { type: string }[] };
    expect(p.steps.map(x => x.type).join(',')).toBe('c,a,b');
  });

  it('moveBuilderStepToSlot moves before slot (DnD semantics)', () => {
    const raw = '{"steps":[{"type":"a"},{"type":"b"},{"type":"c"},{"type":"d"}]}';
    const n = 4;
    const next = moveBuilderStepToSlot(raw, 1, 3, n);
    const p = JSON.parse(next) as { steps: { type: string }[] };
    expect(p.steps.map(x => x.type).join(',')).toBe('a,c,b,d');
  });

  it('moveBuilderStepToSlot can move to end slot', () => {
    const raw = '{"steps":[{"type":"a"},{"type":"b"},{"type":"c"}]}';
    const next = moveBuilderStepToSlot(raw, 0, 3, 3);
    const p = JSON.parse(next) as { steps: { type: string }[] };
    expect(p.steps.map(x => x.type).join(',')).toBe('b,c,a');
  });

  it('parseBuilderStepSummaries reads types', () => {
    const raw = '{"steps":[{"type":"noop"},{"x":1}]}';
    const s = parseBuilderStepSummaries(raw);
    expect(s.length).toBe(2);
    expect(s[0].type).toBe('noop');
    expect(s[1].type).toBe('?');
  });
});
