import { CharacterActionDto } from '../../shared/models/Dtos/characterActionDto';
import { CharacterActionType } from '../../shared/models/enums/characterActionType';
import { actionLabel, intervalProgress } from './action-progress';

describe('action-progress', () => {
  const now = Date.parse('2026-10-01T12:00:00Z');
  const action = (over: Partial<CharacterActionDto>): CharacterActionDto =>
    ({
      characterActionType: CharacterActionType.Combat,
      isDeleted: false,
      nextResolutionAtUtc: new Date(now + 5000),
      updatedAt: new Date(now),
      ...over,
    }) as CharacterActionDto;

  it('says Idle without an action', () => {
    expect(actionLabel(null, now)).toBe('Idle');
  });

  it('names combat as the old sidebar did', () => {
    expect(actionLabel(action({}), now)).toBe('Engaged in Combat');
  });

  it('names the recovery after a combat has ended', () => {
    expect(actionLabel(action({ isDeleted: true, blockedUntilUtc: new Date(now + 3000) }), now)).toBe(
      'Combat ending - recovery',
    );
  });

  it('measures progress through the interval, clamped, with the time left', () => {
    expect(intervalProgress(now + 6000, 10000, now)).toEqual({ progress: 0.4, remaining: '00:06' });
    expect(intervalProgress(now - 1000, 10000, now)).toEqual({ progress: 1, remaining: '00:00' });
    expect(intervalProgress(now + 20000, 10000, now)).toEqual({ progress: 0, remaining: '00:20' });
    expect(intervalProgress(now + 95000, 100000, now).remaining).toBe('01:35');
  });
});
