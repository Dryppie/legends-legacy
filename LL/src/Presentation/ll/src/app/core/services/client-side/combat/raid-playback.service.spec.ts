import { TestBed } from '@angular/core/testing';
import { BattleOutcome } from '../../../../shared/models/Dtos/combatResultDto';
import { RaidPlaybackBundle } from '../../api/raid/raid.service';
import { RaidPlaybackService } from './raid-playback.service';

describe('RaidPlaybackService', () => {
  const bundle: RaidPlaybackBundle = {
    schemaVersion: 2,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 10,
    entities: [
      {
        index: 0,
        id: 'wing-member',
        name: 'Wing Member',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 25,
      },
      {
        index: 1,
        id: 'ward-objective',
        name: 'Ward Objective',
        imagePath: '',
        isFriendly: false,
        maxHealth: 200,
        level: 25,
      },
    ],
    abilities: [{ index: 0, entityIndex: 0, name: 'Basic Attack' }],
    frames: [
      {
        sequence: 0,
        tick: 0,
        entityStates: [
          { entityIndex: 0, health: 100, barrier: 0 },
          { entityIndex: 1, health: 200, barrier: 0 },
        ],
        entityTotals: [],
        abilityTotals: [],
        isFinal: false,
        outcome: null,
      },
      {
        sequence: 1,
        tick: 10,
        entityStates: [
          { entityIndex: 0, health: 95, barrier: 10 },
          { entityIndex: 1, health: 180, barrier: 0 },
        ],
        entityTotals: [
          {
            entityIndex: 0,
            damageDone: 20,
            damageTaken: 5,
            healingDone: 0,
            healingReceived: 0,
            healthRegenerated: 0,
            barrierGenerated: 10,
            damageBlocked: 0,
          },
        ],
        abilityTotals: [
          {
            abilityIndex: 0,
            uses: 1,
            totalDamage: 20,
            damageByType: [{ damageType: 'Physical', totalDamage: 20 }],
            totalHealing: 0,
            totalBarrier: 0,
          },
        ],
        isFinal: true,
        outcome: BattleOutcome.Victory,
      },
    ],
  };

  it('binary-seeks and reconstructs a shared combat frame', () => {
    const service = TestBed.inject(RaidPlaybackService);

    expect(service.frameAtTick(bundle, 9).sequence).toBe(0);
    const frame = service.frameAtTick(bundle, 10);

    expect(frame.sequence).toBe(1);
    expect(frame.friendly[0].health).toBe(95);
    expect(frame.hostile[0].id).toBe('ward-objective');
    expect(frame.entityStats[0].damageDone).toBe(20);
    expect(frame.entityStats[0].abilities[0].uses).toBe(1);
    expect(frame.outcome).toBe(BattleOutcome.Victory);
  });
});
