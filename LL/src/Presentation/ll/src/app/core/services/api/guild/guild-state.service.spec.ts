import { GuildMissionOverview } from '../../../../shared/models/Dtos/guild/guildMission';
import { normalizeGuildMissionOverview } from './guild-state.service';

describe('normalizeGuildMissionOverview', () => {
  it('rejects mission state returned for a previous guild', () => {
    const missions = createOverview('old-guild');

    expect(normalizeGuildMissionOverview(missions, 'new-guild')).toBeNull();
  });

  it('removes incomplete personal orders before notifications and rendering', () => {
    const missions = createOverview('current-guild');
    missions.personalOrders.push({ canClaimReward: true } as never);

    const normalized = normalizeGuildMissionOverview(missions, 'current-guild');

    expect(normalized?.personalOrders.length).toBe(1);
    expect(normalized?.personalOrders[0].definition.name).toBe('Scout');
  });
});

function createOverview(guildId: string): GuildMissionOverview {
  return {
    guildId,
    guildXp: 0,
    guildLevel: 1,
    nextDailyResetAt: '2026-08-11T00:00:00Z',
    nextWeeklyResetAt: '2026-08-17T00:00:00Z',
    canSelectMission: false,
    weeklyOptions: [],
    activeMission: null,
    myWeeklyContribution: null,
    personalOrders: [
      {
        id: 'valid-order',
        definition: {
          id: 'scout',
          key: 'scout',
          name: 'Scout',
          description: 'Scout five rooms.',
          category: 'Dungeon',
          metric: 'DungeonRoomsCleared',
          baseTarget: 5,
        },
        periodKey: '2026-08-10',
        targetAmount: 5,
        currentAmount: 0,
        status: 'Active',
        canClaimReward: false,
        reward: { guildFavor: 50, guildXp: 20, guildSupplies: 10 },
        generatedAt: '2026-08-10T00:00:00Z',
      },
    ],
    contributionSummary: {
      dailyPeriodKey: '2026-08-10',
      weeklyPeriodKey: '2026-W33',
      dailyContributionScore: 0,
      weeklyContributionScore: 0,
      guildFavorEarned: 0,
      guildXpGenerated: 0,
      guildSuppliesGenerated: 0,
      ordersCompleted: 0,
    },
    contributionLeaderboard: [],
  };
}
