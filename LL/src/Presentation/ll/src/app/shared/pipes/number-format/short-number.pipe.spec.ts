import { ShortNumberPipe } from './short-number.pipe';

describe('ShortNumberPipe', () => {
  const pipe = new ShortNumberPipe();

  it('rounds abbreviated positive values down', () => {
    expect(pipe.transform(1_999)).toBe('1.9K');
    expect(pipe.transform(999_999)).toBe('999.9K');
    expect(pipe.transform(1_999_999)).toBe('1.9M');
  });

  it('does not abbreviate values below one thousand', () => {
    expect(pipe.transform(999)).toBe('999');
  });
});
