import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  RegionBossPlaybackBundle,
  RegionBossService,
} from '../../api/region-boss/region-boss.service';
import { RegionBossPlaybackService } from './region-boss-playback.service';

describe('RegionBossPlaybackService', () => {
  const combatant = {
    id: 'player',
    name: 'Ascendant',
    imagePath: '',
    health: 100,
    maxHealth: 100,
    barrier: 0,
    level: 60,
  };
  const bundle: RegionBossPlaybackBundle = {
    schemaVersion: 1,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 20,
    highestLevelDefeated: 1,
    currentBossLevel: 2,
    terminationReason: 'PartyDefeated',
    frames: [
      {
        sequence: 0,
        tick: 0,
        friendly: [combatant],
        hostile: [{ ...combatant, id: 'boss', name: 'The Mad King' }],
        entityStats: [],
        events: [],
        isFinal: false,
        context: { waveNumber: 1, furyStacks: 0, downed: [] },
      },
      {
        sequence: 1,
        tick: 20,
        friendly: [{ ...combatant, health: 0 }],
        hostile: [{ ...combatant, id: 'boss', name: 'The Mad King' }],
        entityStats: [],
        events: [],
        isFinal: true,
        context: { waveNumber: 2, furyStacks: 1, downed: [] },
      },
    ],
  };
  const compactBundle: RegionBossPlaybackBundle = {
    schemaVersion: 2,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 20,
    highestLevelDefeated: 1,
    currentBossLevel: 2,
    terminationReason: 'PartyDefeated',
    entities: [
      {
        index: 0,
        id: 'player',
        name: 'Ascendant',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 60,
        partyNumber: 1,
      },
      {
        index: 1,
        id: 'boss',
        name: 'The Mad King',
        imagePath: '',
        isFriendly: false,
        maxHealth: 100,
        level: 2,
        partyNumber: null,
      },
    ],
    abilities: [{ index: 0, entityIndex: 0, name: 'Strike' }],
    frames: [
      {
        sequence: 0,
        tick: 20,
        entityStates: [
          {
            entityIndex: 0,
            health: 0,
            barrier: 0,
            currentStagger: 0,
            maxStagger: 0,
            isStaggered: false,
            isStaggerRecovering: false,
          },
          {
            entityIndex: 1,
            health: 75,
            barrier: 0,
            currentStagger: 0,
            maxStagger: 0,
            isStaggered: false,
            isStaggerRecovering: false,
          },
        ],
        entityTotals: [
          {
            entityIndex: 0,
            damageDone: 25,
            damageTaken: 100,
            healingDone: 0,
            healingReceived: 0,
            healthRegenerated: 0,
            barrierGenerated: 0,
            damageBlocked: 0,
            threatGenerated: 25,
            staggerContributed: 0,
            staggerBreaks: 0,
          },
        ],
        abilityTotals: [
          {
            abilityIndex: 0,
            uses: 1,
            totalDamage: 25,
            totalHealing: 0,
            totalBarrier: 0,
            damageByType: [],
            totalThreat: 25,
            totalStagger: 0,
            staggerBreaks: 0,
          },
        ],
        isFinal: true,
        context: { waveNumber: 2, furyStacks: 1, downed: [] },
      },
    ],
  };
  const deltaBundle: RegionBossPlaybackBundle = {
    schemaVersion: 3,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 20,
    highestLevelDefeated: 1,
    currentBossLevel: 2,
    terminationReason: 'TimeExpired',
    entities: [
      {
        index: 0,
        id: 'player',
        name: 'Ascendant',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 60,
        partyNumber: 1,
      },
      {
        index: 1,
        id: 'summon',
        name: 'Summon',
        imagePath: '',
        isFriendly: true,
        maxHealth: 50,
        level: 60,
        partyNumber: 1,
      },
      {
        index: 2,
        id: 'boss-1',
        name: 'Boss 1',
        imagePath: '',
        isFriendly: false,
        maxHealth: 100,
        level: 1,
        partyNumber: null,
      },
      {
        index: 3,
        id: 'boss-2',
        name: 'Boss 2',
        imagePath: '',
        isFriendly: false,
        maxHealth: 100,
        level: 2,
        partyNumber: null,
      },
    ],
    abilities: [],
    frames: [
      {
        sequence: 0,
        tick: 0,
        isKeyframe: true,
        entityStates: [state(0, 100), state(1, 50), state(2, 100)],
        entityTotals: [],
        abilityTotals: [],
        isFinal: false,
        context: { waveNumber: 1, furyStacks: 0, downed: [] },
      },
      {
        sequence: 1,
        tick: 10,
        isKeyframe: false,
        entityStates: [state(0, 90), state(1, 0), state(2, 0), state(3, 100)],
        entityTotals: [],
        abilityTotals: [],
        isFinal: false,
        context: { waveNumber: 2, furyStacks: 0, downed: [] },
      },
      {
        sequence: 2,
        tick: 20,
        isKeyframe: false,
        entityStates: [state(0, 80), state(3, 50)],
        entityTotals: [],
        abilityTotals: [],
        isFinal: false,
        context: { waveNumber: 2, furyStacks: 0, downed: [] },
      },
    ],
  };

  let service: RegionBossPlaybackService;
  let regionBosses: jasmine.SpyObj<RegionBossService>;

  beforeEach(() => {
    regionBosses = jasmine.createSpyObj<RegionBossService>(
      'RegionBossService',
      ['getPlaybackBundle'],
    );
    regionBosses.getPlaybackBundle.and.returnValue(of(bundle));
    TestBed.configureTestingModule({
      providers: [{ provide: RegionBossService, useValue: regionBosses }],
    });
    service = TestBed.inject(RegionBossPlaybackService);
  });

  it('binary-seeks Region Boss checkpoints for the shared combat view', () => {
    expect(service.frameAtTick(bundle, 19).sequence).toBe(0);

    const frame = service.frameAtTick(bundle, 20);

    expect(frame.sequence).toBe(1);
    expect(frame.friendly[0].health).toBe(0);
    expect(frame.hostile[0].name).toBe('The Mad King');
    expect(frame.isFinal).toBeTrue();
  });

  it('reconstructs combat state and telemetry from compact playback frames', () => {
    const frame = service.frameAtTick(compactBundle, 20);

    expect(frame.friendly[0].health).toBe(0);
    expect(frame.hostile[0].name).toBe('The Mad King');
    expect(frame.entityStats[0].damageDone).toBe(25);
    expect(frame.entityStats[0].abilities[0].name).toBe('Strike');
    expect(frame.entityStats[0].abilities[0].totalDamage).toBe(25);
  });

  it('applies sparse frames without removing defeated bosses or expired summons', () => {
    const frame = service.frameAtTick(deltaBundle, 20);

    expect(frame.friendly.map((entity) => [entity.id, entity.health])).toEqual([
      ['player', 80],
      ['summon', 0],
    ]);
    expect(frame.hostile.map((entity) => [entity.id, entity.health])).toEqual([
      ['boss-1', 0],
      ['boss-2', 50],
    ]);
  });

  it('reuses the immutable playback bundle for the same run', () => {
    service.getBundle('run-id').subscribe();
    service.getBundle('run-id').subscribe();

    expect(regionBosses.getPlaybackBundle).toHaveBeenCalledTimes(1);
  });

  function state(entityIndex: number, health: number) {
    return {
      entityIndex,
      health,
      barrier: 0,
      currentStagger: 0,
      maxStagger: 0,
      isStaggered: false,
      isStaggerRecovering: false,
    };
  }
});
