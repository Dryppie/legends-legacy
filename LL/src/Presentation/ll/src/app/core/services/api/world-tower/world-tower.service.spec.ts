import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { WorldTowerService } from './world-tower.service';

describe('WorldTowerService', () => {
  let service: WorldTowerService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', [
      'get',
      'post',
      'put',
    ]);
    api.get.and.returnValue(of({}));
    api.post.and.returnValue(of({}));
    api.put.and.returnValue(of({}));
    TestBed.configureTestingModule({
      providers: [WorldTowerService, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(WorldTowerService);
  });

  it('saves a complete party layout, including benched participants', () => {
    const assignments = [
      { characterId: 'leader-id', partySlot: 1 },
      { characterId: 'new-member-id', partySlot: null },
    ];

    service.updateRallyParties('rally-id', assignments).subscribe();

    expect(api.put).toHaveBeenCalledOnceWith(
      'world-tower/rallies/rally-id/parties',
      { assignments },
    );
  });

  it('maps all read operations to the Tower API', () => {
    service.getOverview().subscribe();
    service.getFloor(3).subscribe();
    service.getRally('rally-id').subscribe();
    service.getAttemptReport('attempt-id').subscribe();
    service.getAttemptCombatResult('attempt-id').subscribe();
    service.getAttemptPlayback('attempt-id').subscribe();
    service.getAttemptPlaybackBundle('attempt-id').subscribe();
    service.getHallOfFame().subscribe();
    service.getPersonalExpeditions().subscribe();

    expect(api.get.calls.allArgs()).toEqual([
      ['world-tower'],
      ['world-tower/floors/3'],
      ['world-tower/rallies/rally-id'],
      ['world-tower/attempts/attempt-id/report'],
      ['world-tower/attempts/attempt-id/combat-result'],
      ['world-tower/attempts/attempt-id/playback'],
      ['world-tower/attempts/attempt-id/playback/bundle'],
      ['world-tower/hall-of-fame'],
      ['world-tower/personal-expeditions'],
    ]);
  });

  it('maps rally and contribution mutations without a minimum power field', () => {
    service.createRally(2, 'Echo').subscribe();
    service.applyToRally('rally-id').subscribe();
    service.acceptApplication('rally-id', 'application-id').subscribe();
    service.declineApplication('rally-id', 'application-id').subscribe();
    service.leaveRally('rally-id').subscribe();
    service.fillDevelopmentTeam('rally-id').subscribe();
    service.startRally('rally-id').subscribe();
    service.contribute(4, 'ScoutWeakPoints', 3).subscribe();

    expect(api.post.calls.allArgs()).toEqual([
      ['world-tower/rallies', { floorNumber: 2, mode: 'Echo' }],
      ['world-tower/rallies/rally-id/applications'],
      ['world-tower/rallies/rally-id/applications/application-id/accept'],
      ['world-tower/rallies/rally-id/applications/application-id/decline'],
      ['world-tower/rallies/rally-id/leave'],
      ['world-tower/rallies/rally-id/development/fill'],
      ['world-tower/rallies/rally-id/start'],
      [
        'world-tower/floors/4/contributions',
        { kind: 'ScoutWeakPoints', amount: 3 },
      ],
    ]);
  });
});
