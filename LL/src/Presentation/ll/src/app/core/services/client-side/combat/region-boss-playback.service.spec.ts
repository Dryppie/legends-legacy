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

  it('reuses the immutable playback bundle for the same run', () => {
    service.getBundle('run-id').subscribe();
    service.getBundle('run-id').subscribe();

    expect(regionBosses.getPlaybackBundle).toHaveBeenCalledTimes(1);
  });
});
