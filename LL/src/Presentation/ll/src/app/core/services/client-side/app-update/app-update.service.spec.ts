import { isNewerAppVersion } from './app-update.service';

describe('isNewerAppVersion', () => {
  it('does not advertise an older timestamp manifest as an update', () => {
    expect(
      isNewerAppVersion(
        '2026-08-14T12:08:29.843Z',
        '2026-08-14T12:57:09.935Z',
      ),
    ).toBeFalse();
  });

  it('advertises a newer timestamp manifest', () => {
    expect(
      isNewerAppVersion(
        '2026-08-14T13:00:00.000Z',
        '2026-08-14T12:57:09.935Z',
      ),
    ).toBeTrue();
  });

  it('does not advertise the currently running version', () => {
    expect(isNewerAppVersion('build-42', 'build-42')).toBeFalse();
  });

  it('retains mismatch detection for custom build identifiers', () => {
    expect(isNewerAppVersion('build-43', 'build-42')).toBeTrue();
  });
});
