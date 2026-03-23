import { groupLintWarningsByCode } from './lowcode-workflow-lint-utils';

describe('groupLintWarningsByCode', () => {
  it('returns empty array for null/undefined/empty', () => {
    expect(groupLintWarningsByCode(null)).toEqual([]);
    expect(groupLintWarningsByCode(undefined)).toEqual([]);
    expect(groupLintWarningsByCode([])).toEqual([]);
  });

  it('groups by code and sorts codes', () => {
    const g = groupLintWarningsByCode([
      { code: 'b', message: 'm2' },
      { code: 'a', message: 'm1' },
      { code: 'a', message: 'm3' },
    ]);
    expect(g.map(x => x.code)).toEqual(['a', 'b']);
    expect(g.find(x => x.code === 'a')).toEqual({ code: 'a', count: 2, messages: ['m1', 'm3'] });
    expect(g.find(x => x.code === 'b')).toEqual({ code: 'b', count: 1, messages: ['m2'] });
  });
});
