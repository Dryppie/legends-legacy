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
): SimpleCombatEntityDto {
  return {
    id,
    name,
    health,
    maxHealth,
    barrier: 0,
    level: 1,
    imagePath: '',
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
