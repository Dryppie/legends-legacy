import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../api.service';
import { RaidService } from './raid.service';

describe('RaidService', () => {
  let service: RaidService;
  let api: jasmine.SpyObj<ApiService>;

  beforeEach(() => {
    api = jasmine.createSpyObj<ApiService>('ApiService', [
      'get',
      'post',
      'put',
    ]);
    api.get.and.returnValue(of(null));
    api.post.and.returnValue(of({}));
    api.put.and.returnValue(of({}));
    TestBed.configureTestingModule({
      providers: [RaidService, { provide: ApiService, useValue: api }],
    });
    service = TestBed.inject(RaidService);
  });

  it('saves assigned and benched raid participants as one layout', () => {
    const assignments = [
      { characterId: 'leader-id', lane: 'Vanguard' as const, wingSlotIndex: 0 },
      { characterId: 'member-id', lane: null, wingSlotIndex: null },
    ];

    service.updateParties('raid-id', assignments).subscribe();

    expect(api.put).toHaveBeenCalledOnceWith('raids/raid-id/parties', {
      assignments,
    });
  });

  it('maps local raid creation to the development endpoint', () => {
    service.createDevelopment('raid-boss.hives-abyss', 2).subscribe();

    expect(api.post).toHaveBeenCalledOnceWith(
      'raids/bosses/raid-boss.hives-abyss/development/create',
      { plusLevel: 2 },
    );
  });

  it('maps signup approval to the leader decision endpoint', () => {
    service.approveSignup('raid-id', 'member-id').subscribe();

    expect(api.post).toHaveBeenCalledOnceWith('raids/raid-id/signups/approve', {
      characterId: 'member-id',
    });
  });

  it('maps signup removal to the leader decision endpoint', () => {
    service.removeSignup('raid-id', 'member-id').subscribe();

    expect(api.post).toHaveBeenCalledOnceWith('raids/raid-id/signups/remove', {
      characterId: 'member-id',
    });
  });

  it('loads personal raid history for a boss', () => {
    service.getHistory('raid-boss.hives-abyss', 12).subscribe();

    expect(api.get).toHaveBeenCalledOnceWith(
      'raids/history?raidBossId=raid-boss.hives-abyss&take=12',
    );
  });

  it('loads personal raid history across all bosses', () => {
    service.getHistory(undefined, 20).subscribe();

    expect(api.get).toHaveBeenCalledOnceWith('raids/history?take=20');
  });

  it('tracks a joined raid and clears it after leaving', () => {
    api.post.and.returnValue(
      of({
        id: 'raid-id',
        status: 'Mustering',
        signups: [{ isCurrentCharacter: true }],
        joinRequests: [],
      }),
    );

    service.join('raid-id').subscribe();
    expect(service.activeRaidId()).toBe('raid-id');
    expect(service.activeRaidChatId()).toBe('raid-id');

    service.leave('raid-id').subscribe();
    expect(service.activeRaidId()).toBeNull();
    expect(service.activeRaidChatId()).toBeNull();
  });

  it('tracks the raid while the current character is awaiting approval', () => {
    api.post.and.returnValue(
      of({
        id: 'raid-id',
        status: 'Mustering',
        signups: [],
        joinRequests: [{ isCurrentCharacter: true }],
      }),
    );

    service.join('raid-id').subscribe();

    expect(service.activeRaidId()).toBe('raid-id');
    expect(service.activeRaidChatId()).toBeNull();
  });
});
