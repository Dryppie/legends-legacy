import { Location } from '@angular/common';
import { signal } from '@angular/core';
import {
  TestBed,
  discardPeriodicTasks,
  fakeAsync,
  flushMicrotasks,
} from '@angular/core/testing';
import { of, Subject } from 'rxjs';
import { TournamentGroundsViewStateService } from '../../../../../core/services/api/colosseum/tournament-grounds-view-state.service';
import {
  TournamentDetails,
  TournamentGroundsStatus,
  TournamentSummary,
} from '../../../../../shared/models/Dtos/colosseum/tournamentGrounds';
import { TournamentGroundsComponent } from './tournament-grounds.component';

describe('TournamentGroundsComponent initialization', () => {
  it('joins the audience before loading its snapshot and reconciling', fakeAsync(() => {
    TestBed.configureTestingModule({
      providers: [
        TournamentGroundsViewStateService,
        { provide: Location, useValue: { getState: () => ({}) } },
      ],
    });
    const order: string[] = [];
    let resolveSubscription!: () => void;
    const subscription = new Promise<void>((resolve) => {
      resolveSubscription = resolve;
    });
    const statusRequest = new Subject<TournamentGroundsStatus>();
    const colosseumService = {
      getTournamentGroundsStatus: jasmine.createSpy().and.callFake(() => {
        order.push('snapshot');
        return statusRequest.asObservable();
      }),
      getTournamentRewardTiers: jasmine.createSpy().and.returnValue(of([])),
      getTournamentHistory: jasmine.createSpy().and.returnValue(of([])),
      getTournamentHallOfFame: jasmine.createSpy().and.returnValue(of([])),
      getTournamentSeasonLeaderboard: jasmine
        .createSpy()
        .and.returnValue(of([])),
    };
    const stateSync = {
      register: jasmine.createSpy().and.returnValue(() => undefined),
      latestRevision: jasmine.createSpy().and.returnValue(0),
      acceptDomainVersion: jasmine.createSpy(),
      reconcile: jasmine.createSpy().and.callFake(() => {
        order.push('checkpoint');
        return Promise.resolve();
      }),
    };
    const realtime = {
      setTournamentGroundsSubscription: jasmine
        .createSpy()
        .and.callFake((active: boolean) => {
          if (!active) return Promise.resolve();
          order.push('subscribe');
          return subscription;
        }),
    };
    const events = {
      eventEnvelope: { TournamentGroundsUpdated: signal(null) },
    };
    const component = TestBed.runInInjectionContext(
      () =>
        new TournamentGroundsComponent(
          colosseumService as never,
          {} as never,
          events as never,
          stateSync as never,
          realtime as never,
        ),
    );

    component.ngOnInit();
    TestBed.flushEffects();

    expect(order).toEqual(['subscribe']);
    expect(colosseumService.getTournamentGroundsStatus).not.toHaveBeenCalled();

    resolveSubscription();
    flushMicrotasks();

    expect(order).toEqual(['subscribe', 'snapshot']);
    expect(stateSync.reconcile).not.toHaveBeenCalled();

    statusRequest.next({
      nowUtc: new Date().toISOString(),
      currentTournament: null,
      upcomingTournaments: [],
      recentTournaments: [],
      developmentToolsEnabled: false,
    } as TournamentGroundsStatus);
    statusRequest.complete();
    flushMicrotasks();

    expect(order).toEqual(['subscribe', 'snapshot', 'checkpoint']);
    expect(stateSync.reconcile).toHaveBeenCalledTimes(1);
    expect(stateSync.reconcile).toHaveBeenCalledWith({ afterCurrent: true });

    component.ngOnDestroy();
    flushMicrotasks();
    discardPeriodicTasks();
  }));

  it('does not let an older request or its detail requests overwrite a newer snapshot', () => {
    const firstStatus = new Subject<TournamentGroundsStatus>();
    const secondStatus = new Subject<TournamentGroundsStatus>();
    const firstDetails = new Subject<TournamentDetails>();
    const secondDetails = new Subject<TournamentDetails>();
    const harness = createRaceHarness(
      [firstStatus, secondStatus],
      [firstDetails, secondDetails],
    );
    const tournament = {
      id: 'tournament-1',
      status: 'InProgress',
    } as TournamentSummary;

    harness.component.refresh();
    firstStatus.next(createStatus('2026-08-24T10:00:00Z', tournament));
    harness.component.refresh();
    secondStatus.next(createStatus('2026-08-24T10:01:00Z', tournament));

    const newerDetails = {
      summary: tournament,
      marker: 'newer',
    } as unknown as TournamentDetails;
    const olderDetails = {
      summary: tournament,
      marker: 'older',
    } as unknown as TournamentDetails;
    secondDetails.next(newerDetails);
    firstDetails.next(olderDetails);

    expect(harness.component.status()?.nowUtc).toBe('2026-08-24T10:01:00Z');
    expect(harness.component.details()).toBe(newerDetails);
  });

  it('rejects a snapshot response started before a newer realtime revision', () => {
    const staleStatus = new Subject<TournamentGroundsStatus>();
    const harness = createRaceHarness([staleStatus]);

    harness.component.refresh();
    harness.eventEnvelope.set({
      event: 'TournamentGroundsUpdated',
      updateId: 'tournament-5',
      payload: {
        stateVersion: 5,
      },
    } as never);
    TestBed.flushEffects();
    staleStatus.next(createStatus('2026-08-24T10:00:00Z'));

    expect(harness.component.status()).toBeNull();
    expect(harness.stateSync.acceptDomainVersion).toHaveBeenCalledWith(
      'tournament',
      5,
      'tournament-5',
    );
  });
});

function createRaceHarness(
  statusRequests: Subject<TournamentGroundsStatus>[],
  detailRequests: Subject<TournamentDetails>[] = [],
) {
  TestBed.configureTestingModule({
    providers: [
      TournamentGroundsViewStateService,
      { provide: Location, useValue: { getState: () => ({}) } },
    ],
  });
  let statusIndex = 0;
  let detailIndex = 0;
  let currentRevision = 0;
  const eventEnvelope = signal(null as never);
  const colosseumService = {
    getTournamentGroundsStatus: jasmine
      .createSpy()
      .and.callFake(() => statusRequests[statusIndex++].asObservable()),
    getTournament: jasmine
      .createSpy()
      .and.callFake(() => detailRequests[detailIndex++].asObservable()),
    getTournamentBracket: jasmine
      .createSpy()
      .and.returnValue(of({ rounds: [] })),
    getTournamentRewards: jasmine.createSpy().and.returnValue(of([])),
    getTournamentHistory: jasmine.createSpy().and.returnValue(of([])),
    getTournamentHallOfFame: jasmine.createSpy().and.returnValue(of([])),
    getTournamentSeasonLeaderboard: jasmine.createSpy().and.returnValue(of([])),
  };
  const stateSync = {
    register: jasmine.createSpy().and.returnValue(() => undefined),
    latestRevision: jasmine.createSpy().and.callFake(() => currentRevision),
    acceptDomainVersion: jasmine
      .createSpy()
      .and.callFake((_scope: string, revision: number) => {
        currentRevision = Math.max(currentRevision, revision);
      }),
    reconcile: jasmine.createSpy().and.returnValue(Promise.resolve()),
  };
  const realtime = {
    setTournamentGroundsSubscription: jasmine
      .createSpy()
      .and.returnValue(Promise.resolve()),
  };
  const component = TestBed.runInInjectionContext(
    () =>
      new TournamentGroundsComponent(
        colosseumService as never,
        {} as never,
        {
          eventEnvelope: { TournamentGroundsUpdated: eventEnvelope },
        } as never,
        stateSync as never,
        realtime as never,
      ),
  );
  TestBed.flushEffects();

  return { component, eventEnvelope, stateSync };
}

function createStatus(
  nowUtc: string,
  currentTournament: TournamentSummary | null = null,
): TournamentGroundsStatus {
  return {
    nowUtc,
    currentTournament,
    upcomingTournaments: [],
    recentTournaments: [],
    developmentToolsEnabled: false,
  };
}
