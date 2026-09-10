import { Observable, of } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CombatService } from '../../client-side/combat/combat.service';
import { ApiService } from '../api.service';
import { ColosseumService } from './colosseum.service';

describe('ColosseumService response ownership', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: ColosseumService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['postVersioned']);
    api.postVersioned.and.returnValue(
      of({ data: {}, domainVersions: { colosseum: 1 } }),
    );
    service = new ColosseumService(api, {} as CombatService);
  });

  it('marks a defense snapshot response as owning Colosseum state', () => {
    service.updateDefenseSnapshot().subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'colosseum/defense-snapshot',
      {},
      { stateSyncScopesHandledByResponse: ['colosseum'] },
    );
  });

  it("marks a Champion's Market response as owning Colosseum state", () => {
    service.purchaseChampionMarketItem('weekly-cache', 2).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'colosseum/market/purchase',
      { itemId: 'weekly-cache', quantity: 2 },
      { stateSyncScopesHandledByResponse: ['colosseum'] },
    );
  });

  it('marks an arena battle response as owning attacker arena and character state', () => {
    service.startArenaBattle('opponent-1').subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'colosseum/battle',
      { opponentId: 'opponent-1' },
      {
        stateSyncScopesHandledByResponse: ['colosseum', 'character'],
      },
    );
  });
});

describe('ColosseumService arena error messages', () => {
  let api: ApiService;
  let http: HttpTestingController;
  let service: ColosseumService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(ApiService);
    http = TestBed.inject(HttpTestingController);
    service = new ColosseumService(api, {} as CombatService);
  });

  afterEach(() => http.verify());

  for (const action of ['battle', 'defense-snapshot'] as const) {
    it(`shows the API's configuration guidance when ${action} returns 409 OK`, () => {
      const detail =
        'Choose an available refinement from this Combat Style (unlocked at level 3).';
      let receivedError: Error | undefined;
      const request: Observable<unknown> =
        action === 'battle'
          ? service.startArenaBattle('opponent-1')
          : service.updateDefenseSnapshot();

      request.subscribe({
        next: () => fail('Expected the invalid loadout to reject the request'),
        error: (error: Error) => (receivedError = error),
      });
      http.expectOne(`${api.apiUrl}colosseum/${action}`).flush(
        {
          status: 409,
          title: 'Choose a valid Combat Style configuration',
          detail,
          errorCode: 'combat_style_configuration_invalid',
        },
        { status: 409, statusText: 'OK' },
      );

      expect(receivedError?.message).toBe(detail);
    });
  }
});
