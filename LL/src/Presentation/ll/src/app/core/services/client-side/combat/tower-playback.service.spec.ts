import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import {
  TowerPlaybackBundle,
  WorldTowerService,
} from '../../api/world-tower/world-tower.service';
import { BattleOutcome } from '../../../../shared/models/Dtos/combatResultDto';
import { TowerPlaybackService } from './tower-playback.service';

describe('TowerPlaybackService', () => {
  const bundle: TowerPlaybackBundle = {
    schemaVersion: 2,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 10,
    entities: [
      {
        index: 0,
        id: 'player',
        name: 'Ascendant',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 60,
        partyNumber: 2,
      },
      {
        index: 1,
        id: 'guardian',
        name: 'Guardian',
        imagePath: '',
        isFriendly: false,
        maxHealth: 200,
        level: 60,
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
            targetedAttacks: 3,
            attentionSharePercent: 75,
          },
        ],
        abilityTotals: [
          {
            abilityIndex: 0,
            uses: 1,
            totalDamage: 20,
            damageByType: [
              { damageType: 'Physical', totalDamage: 12 },
              { damageType: 'Burn', totalDamage: 8 },
            ],
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

  let service: TowerPlaybackService;
  let tower: jasmine.SpyObj<WorldTowerService>;

  beforeEach(() => {
    tower = jasmine.createSpyObj<WorldTowerService>('WorldTowerService', [
      'getAttemptPlaybackBundle',
    ]);
    tower.getAttemptPlaybackBundle.and.returnValue(of(bundle));
    TestBed.configureTestingModule({
      providers: [{ provide: WorldTowerService, useValue: tower }],
    });
    service = TestBed.inject(TowerPlaybackService);
  });

  it('binary-seeks and reconstructs the shared combat frame', () => {
    expect(service.frameAtTick(bundle, 9).sequence).toBe(0);

    const frame = service.frameAtTick(bundle, 10);

    expect(frame.sequence).toBe(1);
    expect(frame.friendly[0].health).toBe(95);
    expect(frame.friendly[0].partyNumber).toBe(2);
    expect(frame.hostile[0].health).toBe(180);
    expect(frame.entityStats[0].damageDone).toBe(20);
    expect(frame.entityStats[0].threatGenerated).toBe(34);
    expect(frame.entityStats[0].targetedAttacks).toBe(3);
    expect(frame.entityStats[0].attentionSharePercent).toBe(75);
    expect(frame.entityStats[0].abilities[0].uses).toBe(1);
    expect(frame.entityStats[0].abilities[0].totalThreat).toBe(34);
    expect(frame.entityStats[0].abilities[0].damageByType).toEqual([
      { damageType: 'Physical', totalDamage: 12 },
      { damageType: 'Burn', totalDamage: 8 },
    ]);
    expect(frame.events).toEqual([]);
    expect(frame.outcome).toBe(BattleOutcome.Victory);
  });

  it('materializes sparse frames without dropping defeated or expired entities', () => {
    const sparseBundle: TowerPlaybackBundle = {
      ...bundle,
      schemaVersion: 5,
      totalTicks: 20,
      entities: [
        bundle.entities[0],
        { ...bundle.entities[1], index: 1 },
        {
          index: 2,
          id: 'summon',
          name: 'Summon',
          imagePath: '',
          isFriendly: true,
          maxHealth: 50,
          level: 60,
        },
      ],
      frames: [
        {
          sequence: 0,
          tick: 0,
          isKeyframe: true,
          entityStates: [
            { entityIndex: 0, health: 100, barrier: 0 },
            { entityIndex: 1, health: 200, barrier: 0 },
            { entityIndex: 2, health: 50, barrier: 0 },
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
            { entityIndex: 1, health: 100, barrier: 0 },
            { entityIndex: 2, health: 0, barrier: 0 },
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
          entityStates: [{ entityIndex: 1, health: 0, barrier: 0 }],
          entityTotals: [],
          abilityTotals: [],
          isFinal: true,
          outcome: BattleOutcome.Victory,
        },
      ],
    };

    const frame = service.frameAtTick(sparseBundle, 20);

    expect(frame.friendly.map((entity) => entity.id)).toEqual([
      'player',
      'summon',
    ]);
    expect(frame.friendly[1].health).toBe(0);
    expect(frame.hostile[0].health).toBe(0);
  });

  it('reuses the immutable bundle request for the same attempt and ETag', () => {
    service.getBundle('attempt', 'hash').subscribe();
    service.getBundle('attempt', 'hash').subscribe();

    expect(tower.getAttemptPlaybackBundle).toHaveBeenCalledTimes(1);
  });
});
