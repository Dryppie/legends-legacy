import { lootHistoryLocationLabel } from './loot-tracker.component';

describe('lootHistoryLocationLabel', () => {
  it('does not expose the internal quest id for starter equipment', () => {
    expect(
      lootHistoryLocationLabel({
        source: 'model-e:starter',
        location: 'quest.onboarding.first_weapon',
      }),
    ).toBe('Starter Equipment');
  });

  it('labels player transfers with the other player name', () => {
    expect(
      lootHistoryLocationLabel({
        source: 'player-transfer',
        location: 'Lilfeet',
      }),
    ).toBe('Trade - Lilfeet');
  });

  it('keeps a useful trade label when the counterpart is unavailable', () => {
    expect(
      lootHistoryLocationLabel({
        source: 'player-transfer',
        location: null,
      }),
    ).toBe('Trade');
  });

  it('identifies the cache or loot-producing item that was opened', () => {
    expect(
      lootHistoryLocationLabel({
        source: 'container-reward',
        location: 'Catalyst Selection Cache',
      }),
    ).toBe('Opened: Catalyst Selection Cache');
  });
});
