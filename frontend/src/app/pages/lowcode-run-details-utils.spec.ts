import { stepConfigsDiffer } from './lowcode-run-details-utils';

describe('lowcode-run-details-utils', () => {
  it('stepConfigsDiffer is false when both empty', () => {
    expect(stepConfigsDiffer(null, null)).toBe(false);
    expect(stepConfigsDiffer('', '   ')).toBe(false);
  });

  it('stepConfigsDiffer is true when one side empty', () => {
    expect(stepConfigsDiffer('{}', null)).toBe(true);
    expect(stepConfigsDiffer(null, '{"a":1}')).toBe(true);
  });

  it('stepConfigsDiffer compares trimmed strings', () => {
    expect(stepConfigsDiffer(' {"x":1} ', '{"x":1}')).toBe(false);
    expect(stepConfigsDiffer('{"x":1}', '{"x":2}')).toBe(true);
  });
});
