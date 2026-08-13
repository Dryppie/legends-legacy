import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ColosseumService } from '../../api/colosseum/colosseum.service';
import { TournamentPlaybackBundle } from '../../../../shared/models/Dtos/colosseum/tournamentGrounds';
import { TournamentPlaybackService } from './tournament-playback.service';

describe('TournamentPlaybackService', () => {
  let service: TournamentPlaybackService;
  let colosseum: jasmine.SpyObj<ColosseumService>;

  const bundle: TournamentPlaybackBundle = {
    schemaVersion: 2,
    ticksPerSecond: 10,
    ticksPerFrame: 10,
    totalTicks: 20,
    entities: [
      {
        index: 0,
        id: 'friendly',
        name: 'Friendly',
        imagePath: '',
        isFriendly: true,
        maxHealth: 100,
        level: 1,
      },
      {
        index: 1,
        id: 'hostile',
        name: 'Hostile',
        imagePath: '',
        isFriendly: false,
        maxHealth: 100,
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
      },
      {
        sequence: 1,
        tick: 20,
        entityStates: [
          { entityIndex: 0, health: 100, barrier: 0 },
          { entityIndex: 1, health: 0, barrier: 0 },
        ],
        entityTotals: [],
        abilityTotals: [],
        isFinal: true,
      },
    ],
  };

  beforeEach(() => {
    colosseum = jasmine.createSpyObj<ColosseumService>('ColosseumService', [
      'getTournamentMatchPlaybackBundle',
    ]);
    colosseum.getTournamentMatchPlaybackBundle.and.returnValue(of(bundle));
    TestBed.configureTestingModule({
      providers: [
        TournamentPlaybackService,
        { provide: ColosseumService, useValue: colosseum },
      ],
    });
    service = TestBed.inject(TournamentPlaybackService);
  });

  it('binary-seeks frames and reconstructs combat teams', () => {
    const initial = service.frameAtTick(bundle, 19);
    const final = service.frameAtTick(bundle, 20);

    expect(initial.sequence).toBe(0);
    expect(final.sequence).toBe(1);
    expect(final.friendly[0].id).toBe('friendly');
    expect(final.hostile[0].health).toBe(0);
  });

  it('downloads an immutable bundle only once for the same ETag', () => {
    service.getBundle('tournament', 'match', 'hash').subscribe();
    service.getBundle('tournament', 'match', 'hash').subscribe();

    expect(colosseum.getTournamentMatchPlaybackBundle).toHaveBeenCalledTimes(1);
  });
});
