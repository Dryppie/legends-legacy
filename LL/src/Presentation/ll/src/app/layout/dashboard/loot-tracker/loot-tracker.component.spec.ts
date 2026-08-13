import { lootHistoryLocationLabel } from './loot-tracker.component';

describe('lootHistoryLocationLabel', () => {
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
});
