import { TestBed } from '@angular/core/testing';
import { BattleOutcome } from '../../../../shared/models/Dtos/combatResultDto';
import { RaidPlaybackBundle } from '../../api/raid/raid.service';
import { RaidPlaybackService } from './raid-playback.service';

describe('RaidPlaybackService', () => {
  const bundle: RaidPlaybackBundle = {
    schemaVersion: 3,
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
            threatGenerated: 34,
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
            totalThreat: 34,
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
    expect(frame.entityStats[0].threatGenerated).toBe(34);
    expect(frame.entityStats[0].abilities[0].uses).toBe(1);
    expect(frame.entityStats[0].abilities[0].totalThreat).toBe(34);
    expect(frame.outcome).toBe(BattleOutcome.Victory);
  });

  it('replaces defeated Rearguard enemies with the current wave', () => {
    const service = TestBed.inject(RaidPlaybackService);
    const waveBundle: RaidPlaybackBundle = {
      ...bundle,
      totalTicks: 20,
      entities: [
        bundle.entities[0],
        {
          index: 1,
          id: 'rearguard-wave-1-0',
          name: 'Ant Worker',
          imagePath: '',
          isFriendly: false,
          maxHealth: 100,
          level: 1,
        },
        {
          index: 2,
          id: 'rearguard-wave-2-0',
          name: 'Fire Ant',
          imagePath: '',
          isFriendly: false,
          maxHealth: 120,
          level: 1,
        },
      ],
      abilities: [],
      frames: [
        {
          sequence: 0,
          tick: 0,
          entityStates: [
            { entityIndex: 0, health: 100, barrier: 0 },
            { entityIndex: 1, health: 100, barrier: 0 },
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
            { entityIndex: 0, health: 95, barrier: 0 },
            { entityIndex: 1, health: 0, barrier: 0 },
            { entityIndex: 2, health: 30, barrier: 0 },
          ],
          entityTotals: [],
          abilityTotals: [],
          isFinal: false,
          outcome: null,
        },
        {
          sequence: 2,
          tick: 20,
          entityStates: [
            { entityIndex: 0, health: 90, barrier: 0 },
            { entityIndex: 1, health: 0, barrier: 0 },
            { entityIndex: 2, health: 0, barrier: 0 },
          ],
          entityTotals: [],
          abilityTotals: [],
          isFinal: true,
          outcome: BattleOutcome.Victory,
        },
      ],
    };

    const frame = service.frameAtTick(waveBundle, 10);

    expect(frame.waveNumber).toBe(2);
    expect(frame.hostile.map((entity) => entity.id)).toEqual([
      'rearguard-wave-2-0',
    ]);
    expect(frame.hostile[0].health).toBe(30);
    const defeatedWave = service.frameAtTick(waveBundle, 10, true);
    expect(defeatedWave.waveNumber).toBe(1);
    expect(defeatedWave.hostile.map((entity) => entity.id)).toEqual([
      'rearguard-wave-1-0',
    ]);
    expect(defeatedWave.hostile[0].health).toBe(0);
    expect(service.playbackDurationMilliseconds(waveBundle, 1000)).toBe(3000);
    expect(service.combatTickAtPlaybackElapsed(waveBundle, 1000, 1000)).toBe(
      10,
    );
    expect(service.combatTickAtPlaybackElapsed(waveBundle, 1900, 1000)).toBe(
      10,
    );
    expect(service.combatTickAtPlaybackElapsed(waveBundle, 2100, 1000)).toBe(
      11,
    );
    expect(
      service.playbackPositionAtElapsed(waveBundle, 1900, 1000)
        .isWaveTransitionHold,
    ).toBeTrue();
    expect(
      service.playbackPositionAtElapsed(waveBundle, 2100, 1000)
        .isWaveTransitionHold,
    ).toBeFalse();
  });

  it('materializes sparse wave frames without dropping defeated enemies or summons', () => {
    const service = TestBed.inject(RaidPlaybackService);
    const sparseBundle: RaidPlaybackBundle = {
      ...bundle,
      schemaVersion: 5,
      totalTicks: 20,
      entities: [
        bundle.entities[0],
        {
          index: 1,
          id: 'rearguard-wave-1-0',
          name: 'Ant Worker',
          imagePath: '',
          isFriendly: false,
          maxHealth: 100,
          level: 1,
        },
        {
          index: 2,
          id: 'rearguard-wave-2-0',
          name: 'Fire Ant',
          imagePath: '',
          isFriendly: false,
          maxHealth: 120,
          level: 1,
        },
        {
          index: 3,
          id: 'summon',
          name: 'Summon',
          imagePath: '',
          isFriendly: true,
          maxHealth: 50,
          level: 1,
        },
      ],
      abilities: [],
      frames: [
        {
          sequence: 0,
          tick: 0,
          isKeyframe: true,
          entityStates: [
            { entityIndex: 0, health: 100, barrier: 0 },
            { entityIndex: 1, health: 100, barrier: 0 },
            { entityIndex: 3, health: 50, barrier: 0 },
          ],
          entityTotals: [],
          abilityTotals: [],
          isFinal: false,
          outcome: null,
        },
        {
          sequence: 1,
          tick: 10,
          isKeyframe: false,
          entityStates: [
            { entityIndex: 1, health: 0, barrier: 0 },
            { entityIndex: 2, health: 120, barrier: 0 },
            { entityIndex: 3, health: 0, barrier: 0 },
          ],
          entityTotals: [],
          abilityTotals: [],
          isFinal: false,
          outcome: null,
        },
        {
          sequence: 2,
          tick: 20,
          isKeyframe: false,
          entityStates: [{ entityIndex: 0, health: 90, barrier: 0 }],
          entityTotals: [],
          abilityTotals: [],
          isFinal: false,
          outcome: null,
        },
      ],
    };

    const currentWave = service.frameAtTick(sparseBundle, 20);
    const defeatedWave = service.frameAtTick(sparseBundle, 20, true);

    expect(currentWave.waveNumber).toBe(2);
    expect(currentWave.hostile[0].id).toBe('rearguard-wave-2-0');
    expect(
      currentWave.friendly.find((entity) => entity.id === 'summon')?.health,
    ).toBe(0);
    expect(defeatedWave.hostile[0].id).toBe('rearguard-wave-1-0');
    expect(defeatedWave.hostile[0].health).toBe(0);
  });
});
