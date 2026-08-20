import { SimpleChange } from '@angular/core';
import {
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { CombatEntityStatsComponent } from './combat-entity-stats.component';

describe('CombatEntityStatsComponent', () => {
  let component: CombatEntityStatsComponent;

  beforeEach(() => {
    component = new CombatEntityStatsComponent();
  });

  it('collapses summons of the same owner and type into an aggregate row', () => {
    component.enemyTeam = [
      entity('broodkeeper', 'Morrowmaw, Broodkeeper', 500, 500),
      entity('broodkeeper:summon:broodling:first', 'Broodling', 25, 50),
      entity('broodkeeper:summon:broodling:second', 'Broodling', 0, 50),
    ];
    component.entityStats = [
      stats('broodkeeper', 'Morrowmaw, Broodkeeper', 100),
      stats('broodkeeper:summon:broodling:first', 'Broodling', 17),
      stats('broodkeeper:summon:broodling:second', 'Broodling', 23),
    ];

    refresh(component);

    const group = component.enemyParticipants.find(
      (participant) => participant.isSummonGroup,
    );
    expect(group).toBeDefined();
    expect(group?.name).toBe('Broodling');
    expect(group?.summonCount).toBe(2);
    expect(group?.standingCount).toBe(1);
    expect(group?.summonerName).toBe('Morrowmaw, Broodkeeper');
    expect(group?.entity.health).toBe(25);
    expect(group?.entity.maxHealth).toBe(100);
    expect(component.statsFor(group!.id)?.damageDone).toBe(40);
    expect(component.visibleParticipants).toHaveSize(2);

    component.activateParticipant(group!);

    expect(component.visibleParticipants).toHaveSize(4);
    expect(group?.members?.every((member) => member.name === 'Broodling')).toBe(
      true,
    );

    component.activateParticipant(group!);

    expect(component.visibleParticipants).toHaveSize(2);
  });

  it('does not group ordinary combatants that happen to share a name', () => {
    component.playerTeam = [
      entity('first-twin', 'Mirror Image', 50, 50),
      entity('second-twin', 'Mirror Image', 50, 50),
    ];
    component.entityStats = [
      stats('first-twin', 'Mirror Image', 10, 'Friendly'),
      stats('second-twin', 'Mirror Image', 20, 'Friendly'),
    ];

    refresh(component);

    expect(component.playerParticipants).toHaveSize(2);
    expect(
      component.playerParticipants.some(
        (participant) => participant.isSummonGroup,
      ),
    ).toBeFalse();
  });

  it('uses supplied team names with arena-side fallbacks', () => {
    component.playerTeamName = 'The Performers';
    component.enemyTeamName = 'Exalt the Sun';

    expect(component.teamDisplayName('Friendly')).toBe('The Performers');
    expect(component.teamDisplayName('Hostile')).toBe('Exalt the Sun');

    component.playerTeamName = ' ';
    component.enemyTeamName = null;

    expect(component.teamDisplayName('Friendly')).toBe('Ally');
    expect(component.teamDisplayName('Hostile')).toBe('Enemy');
  });

  it('groups party combatants and their summons under distinct party headers', () => {
    component.currentCharacterId = 'party-one-player';
    component.playerTeam = [
      entity('party-one-player', 'Party One Player', 100, 100, 1),
      entity('party-one-ally', 'Party One Ally', 100, 100, 1),
      entity('party-one-player:summon:wisp:first', 'Wisp', 25, 25, 1),
      entity('party-two-player', 'Party Two Player', 100, 100, 2),
    ];
    component.enemyTeam = [entity('enemy', 'Enemy', 100, 100)];
    component.entityStats = [
      stats('party-one-player', 'Party One Player', 100, 'Friendly'),
      stats('party-one-ally', 'Party One Ally', 50, 'Friendly'),
      stats('party-one-player:summon:wisp:first', 'Wisp', 25, 'Friendly'),
      stats('party-two-player', 'Party Two Player', 200, 'Friendly'),
      stats('enemy', 'Enemy', 75),
    ];

    refresh(component);

    expect(component.hasPartyLayout).toBeTrue();
    expect(component.participantGroups.map((group) => group.label)).toEqual([
      'Party 1',
      'Party 2',
      'Enemy',
    ]);
    expect(component.participantGroups[0].damageDone).toBe(175);
    expect(component.participantGroups[0].isCurrentParty).toBeTrue();
    expect(component.participantGroups[1].damageDone).toBe(200);
    expect(
      component.participantSideLabel(
        component.participantGroups[0].participants.find(
          (participant) => participant.isSummonGroup,
        )!,
      ),
    ).toBe('P1 · Minion');

    const partyOne = component.participantGroups[0];
    expect(component.isParticipantGroupCollapsed(partyOne)).toBeFalse();
    expect(component.visibleParticipantsForGroup(partyOne)).toHaveSize(3);

    component.toggleParticipantGroup(partyOne);

    expect(component.isParticipantGroupCollapsed(partyOne)).toBeTrue();
    expect(component.visibleParticipantsForGroup(partyOne)).toEqual([]);
    expect(component.selectedEntityId).toBe('party-two-player');

    component.toggleParticipantGroup(partyOne);

    expect(component.isParticipantGroupCollapsed(partyOne)).toBeFalse();
    expect(component.visibleParticipantsForGroup(partyOne)).toHaveSize(3);
  });

  it('orders damage breakdowns consistently and scales segments to the largest ability', () => {
    component.playerTeam = [entity('player', 'Player', 100, 100)];
    component.entityStats = [stats('player', 'Player', 100, 'Friendly')];
    component.entityStats[0].abilities = [
      {
        ...component.entityStats[0].abilities[0],
        totalDamage: 100,
        damageByType: [
          { damageType: 'Burn', totalDamage: 30 },
          { damageType: 'Physical', totalDamage: 70 },
        ],
      },
    ];

    refresh(component);

    const ability = component.selectedStats!.abilities[0];
    expect(
      component.damageBreakdown(ability).map((entry) => entry.damageType),
    ).toEqual(['Physical', 'Burn']);
    expect(
      component.damageTypeBarPercentage({
        damageType: 'Physical',
        totalDamage: 70,
      }),
    ).toBe(70);
  });

  it('tracks updated ability snapshots by name', () => {
    const first = stats('player', 'Player', 10, 'Friendly').abilities[0];
    const updated = { ...first, totalDamage: 20 };

    expect(component.trackAbility(0, first)).toBe('Attack');
    expect(component.trackAbility(0, updated)).toBe('Attack');
  });

  it('shows threat generation as a total, rate, and sortable ability output', () => {
    component.playerTeam = [entity('player', 'Player', 100, 100)];
    component.combatDurationTicks = 50;
    const playerStats = stats('player', 'Player', 100, 'Friendly');
    playerStats.threatGenerated = 250;
    playerStats.abilities = [
      { ...playerStats.abilities[0], name: 'Low Threat', totalThreat: 40 },
      { ...playerStats.abilities[0], name: 'High Threat', totalThreat: 120 },
    ];
    component.entityStats = [playerStats];

    refresh(component);
    component.sortAbilities('threat');

    expect(component.threatPerSecond(component.selectedStats)).toBe(50);
    expect(component.threatBarPercentage(component.sortedAbilities[0])).toBe(
      100,
    );
    expect(component.sortedAbilities.map((ability) => ability.name)).toEqual([
      'High Threat',
      'Low Threat',
    ]);
  });
});

function refresh(component: CombatEntityStatsComponent): void {
  component.ngOnChanges({
    entityStats: new SimpleChange(undefined, component.entityStats, true),
  });
}

function entity(
  id: string,
  name: string,
  health: number,
  maxHealth: number,
  partyNumber?: number,
): SimpleCombatEntityDto {
  return {
    id,
    name,
    health,
    maxHealth,
    barrier: 0,
    level: 1,
    imagePath: '',
    partyNumber,
  };
}

function stats(
  entityId: string,
  entityName: string,
  damageDone: number,
  team = 'Hostile',
): EntityStats {
  return {
    entityId,
    entityName,
    damageDone,
    damageTaken: 0,
    healingDone: 0,
    healingReceived: 0,
    healthRegenerated: 0,
    healthRegenerationPotential: 0,
    healthRegenerationOverhealed: 0,
    healthRegenerationPulses: 0,
    selfDamageDone: 0,
    selfDamageTaken: 0,
    alliedDamageDone: 0,
    alliedDamageTaken: 0,
    team,
    barrierGenerated: 0,
    damageBlocked: 0,
    abilities: [
      {
        name: 'Attack',
        totalDamage: damageDone,
        damageByType: [],
        totalHealing: 0,
        uses: 1,
        hits: 1,
        crits: 0,
        summons: 0,
        stuns: 0,
        selfDamage: 0,
        alliedDamage: 0,
        totalBarrier: 0,
      },
    ],
  };
}
