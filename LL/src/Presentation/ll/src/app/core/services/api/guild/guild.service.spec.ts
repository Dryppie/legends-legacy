import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { GuildService } from './guild.service';

describe('GuildService response ownership', () => {
  let api: jasmine.SpyObj<ApiService>;
  let service: GuildService;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', ['postVersioned']);
    api.postVersioned.and.returnValue(of({ data: {}, domainVersions: {} }));
    service = new GuildService(api);
  });

  it('marks complete building responses as authoritative for buildings only', () => {
    service.constructBuilding('GuildHall' as never).subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'guild/constructBuilding',
      'GuildHall',
      { stateSyncScopesHandledByResponse: ['guild-buildings'] },
    );
  });

  it('marks complete mission responses as authoritative for missions only', () => {
    service.claimOrderReward('order-id').subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'guild/claimOrderReward',
      'order-id',
      { stateSyncScopesHandledByResponse: ['guild-missions'] },
    );
  });

  it('keeps adjacent character domains reconciled after a shop purchase', () => {
    service.purchaseShopItem('item-key').subscribe();

    expect(api.postVersioned).toHaveBeenCalledOnceWith(
      'guild/purchaseShopItem',
      'item-key',
      { stateSyncScopesHandledByResponse: ['guild-shop', 'inventory'] },
    );
  });
});
