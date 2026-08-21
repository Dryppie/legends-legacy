import { of } from 'rxjs';
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
});
