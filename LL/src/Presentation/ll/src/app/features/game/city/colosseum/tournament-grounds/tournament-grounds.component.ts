import {
  DatePipe,
  Location,
  NgFor,
  NgIf,
  NgTemplateOutlet,
} from '@angular/common';
import {
  Component,
  OnDestroy,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, finalize, tap } from 'rxjs';
import { ColosseumService } from '../../../../../core/services/api/colosseum/colosseum.service';
import { ToastService } from '../../../../../core/services/client-side/components/toast/toast.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';
import { TournamentGroundsUpdated } from '../../../../../core/services/real-time/colosseum/tournament-grounds-updated';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { TournamentGroundsViewStateService } from '../../../../../core/services/api/colosseum/tournament-grounds-view-state.service';
import { StateSyncCoordinator } from '../../../../../core/services/real-time/game-realtime/state-sync-coordinator.service';
import {
  TournamentBracket,
  TournamentHallOfFameEntry,
  TournamentHistoryEntry,
  TournamentMatch,
  TournamentParticipant,
  TournamentRewardGrant,
  TournamentRewardTier,
  TournamentRound,
  TournamentSeasonLeaderboardEntry,
  TournamentSummary,
  TournamentTeam,
  TournamentTeamInvite,
} from '../../../../../shared/models/Dtos/colosseum/tournamentGrounds';

@Component({
  selector: 'app-tournament-grounds',
  imports: [
    DatePipe,
    NgFor,
    NgIf,
    NgTemplateOutlet,
    RouterLink,
    CharacterTagComponent,
  ],
  templateUrl: './tournament-grounds.component.html',
  styleUrl: './tournament-grounds.component.scss',
})
export class TournamentGroundsComponent implements OnInit, OnDestroy {
  private readonly viewState = inject(TournamentGroundsViewStateService);
  private readonly location = inject(Location);
  readonly status = this.viewState.status;
  readonly details = this.viewState.details;
  readonly bracket = this.viewState.bracket;
  readonly rewards = this.viewState.rewards;
  readonly history = this.viewState.history;
  readonly hallOfFame = this.viewState.hallOfFame;
  readonly seasonLeaderboard = this.viewState.seasonLeaderboard;
  readonly latestRealtimeUpdate = signal<TournamentGroundsUpdated | null>(null);
  readonly loading = signal(false);
  readonly actionLoading = signal(false);
  readonly error = signal<string | null>(null);
  readonly clock = signal(Date.now());
  readonly invitePickerOpen = signal(false);
  readonly selectedTeamId = signal<string | null>(null);
  readonly rewardTiers = signal<TournamentRewardTier[]>([]);
  readonly rewardTiersOpen = signal(false);
  readonly selectedRoundNumber = this.viewState.selectedRoundNumber;

  readonly current = computed(() => this.status()?.currentTournament ?? null);
  readonly displayedTournament = computed(
    () => this.details()?.summary ?? this.current(),
  );
  readonly showingPreviousTournament = computed(() => {
    const currentId = this.current()?.id;
    const displayedId = this.displayedTournament()?.id;
    return Boolean(currentId && displayedId && currentId !== displayedId);
  });
  readonly developmentToolsEnabled = computed(
    () => this.status()?.developmentToolsEnabled ?? false,
  );
  readonly canStartDevelopmentTournament = computed(() => {
    if (!this.developmentToolsEnabled()) return false;

    const status = this.current()?.status;
    return !status || status === 'Scheduled' || status === 'RegistrationOpen';
  });
  readonly teams = computed(() => this.details()?.teams ?? []);
  readonly selectedTeam = computed(() => {
    const selectedTeamId = this.selectedTeamId();
    if (!selectedTeamId) return null;

    const detailsTeam = this.teams().find(
      (team) => team.teamId === selectedTeamId,
    );
    if (detailsTeam) return detailsTeam;

    return (
      (this.bracket()?.rounds ?? [])
        .flatMap((round) =>
          round.matches.flatMap((match) => [
            match.playerOne,
            match.playerTwo,
          ]),
        )
        .find((team) => team?.teamId === selectedTeamId) ?? null
    );
  });
  readonly registrationOpenSlots = computed(() => {
    const tournament = this.displayedTournament();
    if (!tournament) return [];

    const firstOpenSlot = this.teams().length + 1;
    const openSlotCount = Math.max(
      0,
      tournament.maxParticipants - this.teams().length,
    );

    return Array.from(
      { length: openSlotCount },
      (_, index) => firstOpenSlot + index,
    );
  });
  readonly playerTeam = computed(
    () => this.teams().find((team) => team.isPlayerTeam) ?? null,
  );
  readonly openTeams = computed(() =>
    this.teams().filter((team) => team.isOpen),
  );
  readonly unassignedParticipants = computed(
    () =>
      this.details()?.participants.filter(
        (participant) =>
          !participant.teamId && participant.status !== 'Withdrawn',
      ) ?? [],
  );
  readonly playerPendingInvites = computed(() => {
    const participantId = this.displayedTournament()?.playerParticipantId;
    if (!participantId) return [];

    return this.teams().flatMap((team) =>
      team.invites
        .filter((invite) => invite.invitedParticipantId === participantId)
        .map((invite) => ({ team, invite })),
    );
  });
  readonly unclaimedRewards = computed(() =>
    this.rewards().filter((reward) => reward.status === 'Unclaimed'),
  );
  readonly claimedRewards = computed(() =>
    this.rewards().filter((reward) => reward.status === 'Claimed'),
  );
  readonly playerMatches = computed(() => {
    const teamId = this.playerTeam()?.teamId;
    if (!teamId) return [];

    return (this.bracket()?.rounds ?? []).flatMap((round) =>
      round.matches
        .filter(
          (match) =>
            match.playerOne?.teamId === teamId ||
            match.playerTwo?.teamId === teamId,
        )
        .map((match) => ({ round, match })),
    );
  });
  readonly activePlayerMatch = computed(
    () =>
      this.playerMatches().find(
        ({ match }) => match.status !== 'Completed' && match.status !== 'Bye',
      ) ?? null,
  );
  readonly currentRound = computed(() => {
    const rounds = this.bracket()?.rounds ?? [];
    const resolvingRound = rounds.find(
      (round) =>
        round.status === 'InProgress' ||
        round.status === 'Resolving' ||
        round.matches.some((match) => match.status === 'Resolving'),
    );
    if (resolvingRound) return resolvingRound;

    const dueRound = [...rounds]
      .reverse()
      .find(
        (round) =>
          round.status !== 'Completed' &&
          Date.parse(round.startsAtUtc) <= this.clock(),
      );
    if (dueRound) return dueRound;

    return (
      rounds.find((round) => round.status !== 'Completed') ??
      [...rounds].reverse().find((round) => round.status === 'Completed') ??
      null
    );
  });
  readonly selectedRound = computed(() => {
    const rounds = this.bracket()?.rounds ?? [];
    const roundNumber = this.selectedRoundNumber();
    return (
      rounds.find((round) => round.roundNumber === roundNumber) ??
      this.currentRound() ??
      rounds[0] ??
      null
    );
  });
  readonly totalBattleCount = computed(() =>
    (this.bracket()?.rounds ?? []).reduce(
      (total, round) => total + round.matches.length,
      0,
    ),
  );
  readonly playerRecord = computed(() => {
    const teamId = this.playerTeam()?.teamId;
    return this.playerMatches().reduce(
      (record, { match }) => {
        if (match.status !== 'Completed') return record;
        if (match.winnerTeamId === teamId) record.wins += 1;
        else record.losses += 1;
        return record;
      },
      { wins: 0, losses: 0 },
    );
  });
  readonly currentRoundAction = computed(() => {
    const round = this.currentRound();
    if (!round) return null;

    const resolvingEndsAt = round.matches
      .filter(
        (match) =>
          match.status === 'Resolving' && Boolean(match.playbackEndsAtUtc),
      )
      .map((match) => match.playbackEndsAtUtc!)
      .sort((left, right) => Date.parse(right) - Date.parse(left))[0];
    if (resolvingEndsAt) {
      return { at: resolvingEndsAt, kind: 'resolving' as const };
    }

    const nextBattleAt = this.nextScheduledMatchAt(round);
    return nextBattleAt
      ? { at: nextBattleAt, kind: 'scheduled' as const }
      : null;
  });
  readonly nextActionAt = computed(() => {
    const tournament = this.current();
    if (!tournament) return null;

    switch (tournament.status) {
      case 'Scheduled':
        return tournament.registrationStartsAtUtc;
      case 'RegistrationOpen':
        return tournament.registrationEndsAtUtc;
      case 'RegistrationClosed':
      case 'BracketGenerated':
        return tournament.startsAtUtc;
      case 'InProgress':
        return (
          this.currentRoundAction()?.at ??
          this.latestRealtimeUpdate()?.nextActionAtUtc ??
          null
        );
      default:
        return null;
    }
  });
  readonly countdown = computed(() => {
    const target = this.nextActionAt();
    if (!target) return null;

    const remainingSeconds = Math.max(
      0,
      Math.floor((Date.parse(target) - this.clock()) / 1000),
    );
    const hours = Math.floor(remainingSeconds / 3600);
    const minutes = Math.floor((remainingSeconds % 3600) / 60);
    const seconds = remainingSeconds % 60;

    return hours > 0
      ? `${hours}:${minutes.toString().padStart(2, '0')}:${seconds
          .toString()
          .padStart(2, '0')}`
      : `${minutes.toString().padStart(2, '0')}:${seconds
          .toString()
          .padStart(2, '0')}`;
  });
  private lastRealtimeUpdateId: string | null = null;
  private clockHandle: ReturnType<typeof setInterval> | null = null;
  private lastAutoSelectedRoundNumber: number | null = null;
  private unregisterStateSync: (() => void) | null = null;

  constructor(
    private readonly colosseumService: ColosseumService,
    private readonly toastService: ToastService,
    private readonly eventService: GameEventService,
    stateSync: StateSyncCoordinator,
  ) {
    this.unregisterStateSync = stateSync.register(
      'tournament',
      'tournament-grounds',
      () => this.synchronize(),
    );
    this.lastRealtimeUpdateId =
      this.eventService.eventEnvelope.TournamentGroundsUpdated()?.updateId ??
      null;
    effect(
      () => {
        const envelope =
          this.eventService.eventEnvelope.TournamentGroundsUpdated();
        if (
          !envelope?.updateId ||
          envelope.updateId === this.lastRealtimeUpdateId
        ) {
          return;
        }

        this.lastRealtimeUpdateId = envelope.updateId;
        this.latestRealtimeUpdate.set(envelope.payload);
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.clockHandle = setInterval(
      () =>
        this.clock.set(Date.now() + this.viewState.serverClockOffsetMs),
      1000,
    );
    this.loadRewardTiers();

    if (!this.shouldRestoreViewState()) {
      this.refresh();
    }
  }

  ngOnDestroy(): void {
    if (this.clockHandle) clearInterval(this.clockHandle);
    this.unregisterStateSync?.();
  }

  refresh(): void {
    this.synchronize().subscribe({ error: () => undefined });
  }

  private synchronize(): Observable<unknown> {
    this.loading.set(true);
    this.error.set(null);

    return this.colosseumService
      .getTournamentGroundsStatus()
      .pipe(
        tap({
          next: (status) => this.applyTournamentStatus(status),
          error: (err) =>
            this.error.set(err.message ?? 'Failed to load tournament grounds'),
        }),
        finalize(() => this.loading.set(false)),
      );
  }

  private applyTournamentStatus(
    status: NonNullable<ReturnType<typeof this.status>>,
  ): void {
    const serverNow = Date.parse(status.nowUtc);
    if (!Number.isNaN(serverNow)) {
      this.viewState.serverClockOffsetMs = serverNow - Date.now();
      this.clock.set(serverNow);
    }
    this.status.set(status);
    this.viewState.markSnapshotLoaded();
    const currentTournament = status.currentTournament;
    const previousTournament = status.recentTournaments.find(
      (tournament) => tournament.status === 'Completed',
    );
    const displayedTournament =
      currentTournament?.status === 'Scheduled'
        ? (previousTournament ?? currentTournament)
        : (currentTournament ?? previousTournament);
    const tournamentId = displayedTournament?.id;
    if (tournamentId) {
      this.loadDetails(tournamentId);
      this.loadBracket(tournamentId);
      this.loadRewards(tournamentId);
      this.loadArchives();
    } else {
      this.details.set(null);
      this.bracket.set(null);
      this.rewards.set([]);
      this.loadArchives();
    }
  }

  private shouldRestoreViewState(): boolean {
    const navigationState = this.location.getState() as {
      preserveTournamentGrounds?: boolean;
    };
    return (
      navigationState.preserveTournamentGrounds === true &&
      this.viewState.hasSnapshot
    );
  }

  register(tournament: TournamentSummary): void {
    this.runAction(
      this.colosseumService.registerTournament(tournament.id),
      'Registration complete',
      () => this.refresh(),
    );
  }

  startDevelopmentTournament(): void {
    this.runAction(
      this.colosseumService.startDevelopmentTournament(),
      'Test tournament started',
      () => this.refresh(),
    );
  }

  withdraw(tournament: TournamentSummary): void {
    this.runAction(
      this.colosseumService.withdrawTournament(tournament.id),
      'Registration withdrawn',
      () => this.refresh(),
    );
  }

  updateLoadout(tournament: TournamentSummary): void {
    this.runAction(
      this.colosseumService.updateTournamentLoadout(tournament.id),
      'Tournament loadout updated',
      () => this.refresh(),
    );
  }

  createTeam(tournament: TournamentSummary | null, name: string): void {
    if (!tournament) return;

    this.runAction(
      this.colosseumService.createTournamentTeam(tournament.id, name),
      'Team created',
      () => this.refresh(),
    );
  }

  inviteToTeam(
    tournament: TournamentSummary,
    team: TournamentTeam,
    participant: TournamentParticipant,
  ): void {
    this.runAction(
      this.colosseumService.inviteTournamentTeamMember(
        tournament.id,
        team.teamId,
        participant.participantId,
      ),
      'Invite sent',
      () => {
        this.invitePickerOpen.set(false);
        this.refresh();
      },
    );
  }

  applyToTeam(
    tournament: TournamentSummary | null,
    team: TournamentTeam,
  ): void {
    if (!tournament) return;

    this.runAction(
      this.colosseumService.applyToTournamentTeam(tournament.id, team.teamId),
      'Application sent',
      () => this.refresh(),
    );
  }

  acceptInvite(invite: TournamentTeamInvite): void {
    this.runAction(
      this.colosseumService.acceptTournamentTeamInvite(invite.inviteId),
      'Invite accepted',
      () => this.refresh(),
    );
  }

  acceptApplication(applicationId: string): void {
    this.runAction(
      this.colosseumService.acceptTournamentTeamApplication(applicationId),
      'Application accepted',
      () => this.refresh(),
    );
  }

  kickMember(
    tournament: TournamentSummary,
    team: TournamentTeam,
    participant: TournamentParticipant,
  ): void {
    this.runAction(
      this.colosseumService.kickTournamentTeamMember(
        tournament.id,
        team.teamId,
        participant.participantId,
      ),
      'Team member removed',
      () => this.refresh(),
    );
  }

  claim(tournament: TournamentSummary): void {
    this.runAction(
      this.colosseumService.claimTournamentRewards(tournament.id),
      'Rewards claimed',
      () => {
        this.loadRewards(tournament.id);
        this.refresh();
      },
    );
  }

  claimDisplayedRewards(): void {
    const tournament = this.displayedTournament();
    if (tournament) this.claim(tournament);
  }

  replay(tournament: TournamentSummary, match: TournamentMatch): void {
    if (!match.battleHistoryId) return;

    this.actionLoading.set(true);
    this.error.set(null);
    this.colosseumService
      .getTournamentMatchReplay(tournament.id, match.id)
      .pipe(finalize(() => this.actionLoading.set(false)))
      .subscribe({
        next: (replay) => this.colosseumService.startTournamentReplay(replay),
        error: (err: Error) => {
          const message = err.message ?? 'Failed to load tournament replay';
          this.error.set(message);
          this.toastService.showToast('Replay unavailable', message, false);
        },
      });
  }

  teamLabel(
    team:
      | { name: string; seed?: number | null; memberCount?: number | null }
      | null
      | undefined,
  ): string {
    if (!team) return 'Pending';
    const name = team.seed ? `#${team.seed} ${team.name}` : team.name;
    return team.memberCount ? `${name} (${team.memberCount}/3)` : name;
  }

  outcomeLabel(
    match: TournamentBracket['rounds'][number]['matches'][number],
  ): string {
    if (match.status === 'Bye') return 'Advanced by bye';
    if (match.status === 'Resolving') return 'Live now';
    if (match.status === 'Ready' && match.scheduledAtUtc) {
      return `Starts ${this.shortTime(match.scheduledAtUtc)}`;
    }
    if (match.status !== 'Completed') return this.enumLabel(match.status);
    const winner =
      match.winnerTeamId === match.playerOne?.teamId
        ? match.playerOne
        : match.playerTwo;
    if (winner && match.outcome === 'DrawAdvancedByDamage') {
      return `${winner.name} advanced on damage`;
    }
    if (winner && match.outcome === 'DrawAdvancedBySeed') {
      return `${winner.name} advanced on seed tiebreak`;
    }
    return winner ? `${winner.name} advanced` : this.enumLabel(match.outcome);
  }

  opponentFor(
    match: TournamentMatch | null | undefined,
  ): TournamentTeam | null {
    if (!match) return null;
    const teamId = this.playerTeam()?.teamId;
    if (!teamId) return null;
    return match.playerOne?.teamId === teamId
      ? (match.playerTwo ?? null)
      : (match.playerOne ?? null);
  }

  isPlayerMatch(match: TournamentMatch): boolean {
    const teamId = this.playerTeam()?.teamId;
    return Boolean(
      teamId &&
        (match.playerOne?.teamId === teamId ||
          match.playerTwo?.teamId === teamId),
    );
  }

  isPlayerWinner(match: TournamentMatch): boolean {
    return Boolean(
      this.playerTeam()?.teamId &&
        match.winnerTeamId === this.playerTeam()?.teamId,
    );
  }

  seedNumber(seed: number | null | undefined, fallback: number): string {
    return (seed ?? fallback).toString().padStart(2, '0');
  }

  toggleSelectedTeam(team: TournamentTeam): void {
    this.selectedTeamId.update((teamId) =>
      teamId === team.teamId ? null : team.teamId,
    );
  }

  roundNavigationLabel(round: TournamentRound): string {
    const teamCount = round.matches.length * 2;
    if (teamCount === 2) return 'Final';
    if (teamCount === 4) return 'SF';
    if (teamCount === 8) return 'QF';
    return `R${teamCount}`;
  }

  roundDisplayName(round: TournamentRound): string {
    const teamCount = round.matches.length * 2;
    if (teamCount === 2) return 'Final';
    if (teamCount === 4) return 'Semi-finals';
    if (teamCount === 8) return 'Quarter-finals';
    return `Round of ${teamCount}`;
  }

  roundTimingLabel(round: TournamentRound): string {
    if (round.status === 'Completed') return 'Complete';
    if (this.currentRound()?.id === round.id) {
      if (round.matches.some((match) => match.status === 'Resolving')) {
        return 'Resolving now';
      }

      const nextBattleAt = this.nextScheduledMatchAt(round);
      return nextBattleAt
        ? `Next battle ${this.shortTime(nextBattleAt)}`
        : 'Current round';
    }

    return this.shortTime(round.startsAtUtc);
  }

  private nextScheduledMatchAt(round: TournamentRound): string | null {
    return (
      round.matches
        .filter(
          (match) =>
            match.status !== 'Completed' &&
            match.status !== 'Bye' &&
            match.status !== 'Resolving' &&
            Boolean(match.scheduledAtUtc),
        )
        .map((match) => match.scheduledAtUtc!)
        .sort((left, right) => Date.parse(left) - Date.parse(right))[0] ?? null
    );
  }

  private shortTime(value: string): string {
    return new Date(value).toLocaleTimeString([], {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  navigateRound(offset: number): void {
    const rounds = this.bracket()?.rounds ?? [];
    const selected = this.selectedRound();
    if (!selected) return;

    const currentIndex = rounds.findIndex((round) => round.id === selected.id);
    const nextRound = rounds[currentIndex + offset];
    if (nextRound) this.selectedRoundNumber.set(nextRound.roundNumber);
  }

  canNavigateRound(offset: number): boolean {
    const rounds = this.bracket()?.rounds ?? [];
    const selected = this.selectedRound();
    if (!selected) return false;
    const currentIndex = rounds.findIndex((round) => round.id === selected.id);
    return Boolean(rounds[currentIndex + offset]);
  }

  heroDescription(tournament: TournamentSummary): string {
    if (tournament.status === 'RegistrationOpen') {
      const remaining = Math.max(
        0,
        tournament.maxParticipants - tournament.registeredParticipantCount,
      );
      return `${remaining} places remain. Seeding locks when registration closes.`;
    }
    if (tournament.status === 'InProgress') {
      const round = this.currentRound();
      return round
        ? `${this.roundDisplayName(round)} is underway. Matches resolve automatically on the tournament clock.`
        : 'The tournament is underway. Matches resolve automatically.';
    }
    if (tournament.status === 'Completed') {
      return tournament.playerFinalPlacement === 1
        ? 'The grounds are claimed. Your rewards are ready below.'
        : 'The tournament is complete. Review the bracket and your earned rewards.';
    }
    if (tournament.status === 'Cancelled') {
      return tournament.cancellationReason ?? 'This tournament was cancelled.';
    }
    return 'Teams are seeded into a single-elimination bracket when registration closes.';
  }

  entryCardEyebrow(tournament: TournamentSummary): string {
    if (tournament.status === 'Completed') return 'Your run';
    if (this.activePlayerMatch()) return 'Next match';
    return 'Your entry';
  }

  entryCardTitle(tournament: TournamentSummary): string {
    const opponent = this.opponentFor(this.activePlayerMatch()?.match);
    if (opponent) return `vs ${opponent.name}`;
    if (tournament.playerFinalPlacement === 1) return 'Champion';
    if (tournament.playerFinalPlacement) {
      return `Placed #${tournament.playerFinalPlacement}`;
    }
    return this.playerStatusLabel(tournament);
  }

  entryCardCopy(tournament: TournamentSummary): string {
    const active = this.activePlayerMatch();
    if (active) {
      return `${active.round.name}. The match resolves automatically at the scheduled time.`;
    }
    if (!tournament.isRegistered) {
      return (
        tournament.cannotRegisterReason ??
        'Register to reserve your place in the next bracket.'
      );
    }
    if (tournament.status === 'RegistrationOpen') {
      return 'Your current equipment and essences are saved with this entry.';
    }
    const record = this.playerRecord();
    return `Tournament record ${record.wins}-${record.losses}.`;
  }

  playerStatusLabel(tournament: TournamentSummary): string {
    if (!tournament.playerStatus) return 'Not entered';
    if (tournament.playerStatus === 'Champion') return 'Champion';
    if (tournament.playerFinalPlacement) {
      return `${this.enumLabel(tournament.playerStatus)} · Place ${tournament.playerFinalPlacement}`;
    }

    return this.enumLabel(tournament.playerStatus);
  }

  rewardLabel(reward: TournamentRewardGrant): string {
    if (reward.placement === 1) return 'Champion reward';
    if (reward.placement) return `Place ${reward.placement}`;
    return 'Participation reward';
  }

  rewardTierPlacementLabel(
    tier: TournamentRewardTier,
    index: number,
  ): string {
    const previousMax =
      index > 0 ? (this.rewardTiers()[index - 1].maxPlacement ?? 0) : 0;
    const minimum = previousMax + 1;
    const placement =
      tier.maxPlacement == null
        ? `#${minimum}+`
        : tier.maxPlacement === minimum
          ? `#${minimum}`
          : `#${minimum}–${tier.maxPlacement}`;
    return `${placement} · ${this.enumLabel(tier.key)}`;
  }

  historyResultLabel(entry: TournamentHistoryEntry): string {
    if (entry.status === 'Cancelled')
      return entry.cancellationReason ?? 'Cancelled';
    if (entry.finalPlacement === 1) return 'Champion';
    if (entry.finalPlacement) return `Placed ${entry.finalPlacement}`;
    return this.enumLabel(entry.participantStatus);
  }

  championSeedLabel(entry: TournamentHallOfFameEntry): string {
    return entry.championSeed ? `Seed #${entry.championSeed}` : 'Unseeded';
  }

  leaderboardPlacementLabel(entry: TournamentSeasonLeaderboardEntry): string {
    if (!entry.bestPlacement) return 'No placement';
    if (entry.bestPlacement === 1) return 'Champion';
    return `Best place ${entry.bestPlacement}`;
  }

  nextStateLabel(tournament: TournamentSummary): string {
    switch (tournament.status) {
      case 'Scheduled':
        return 'Registration opens';
      case 'RegistrationOpen':
        return 'Registration closes';
      case 'RegistrationClosed':
      case 'BracketGenerated':
        return 'Tournament starts';
      case 'InProgress':
        return this.currentRoundAction()?.kind === 'scheduled'
          ? 'Next battle starts'
          : 'Battles resolving';
      case 'Completed':
        return 'Completed';
      case 'Cancelled':
        return 'Cancelled';
      default:
        return this.enumLabel(tournament.status);
    }
  }

  enumLabel(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .replace(/_/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }

  private loadBracket(tournamentId: string): void {
    this.colosseumService.getTournamentBracket(tournamentId).subscribe({
      next: (bracket) => {
        this.bracket.set(bracket);
        const activeRoundNumber = this.preferredRoundNumber(bracket);
        if (
          this.selectedRoundNumber() === null ||
          activeRoundNumber !== this.lastAutoSelectedRoundNumber
        ) {
          this.selectedRoundNumber.set(activeRoundNumber);
        }
        this.lastAutoSelectedRoundNumber = activeRoundNumber;
      },
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament bracket'),
    });
  }

  private preferredRoundNumber(bracket: TournamentBracket): number | null {
    const resolvingRound = bracket.rounds.find(
      (round) =>
        round.status === 'InProgress' ||
        round.status === 'Resolving' ||
        round.matches.some((match) => match.status === 'Resolving'),
    );
    if (resolvingRound) return resolvingRound.roundNumber;

    const dueRound = [...bracket.rounds]
      .reverse()
      .find(
        (round) =>
          round.status !== 'Completed' &&
          Date.parse(round.startsAtUtc) <= this.clock(),
      );
    if (dueRound) return dueRound.roundNumber;

    return (
      bracket.rounds.find((round) => round.status !== 'Completed')
        ?.roundNumber ??
      bracket.rounds[bracket.rounds.length - 1]?.roundNumber ??
      null
    );
  }

  private loadDetails(tournamentId: string): void {
    this.colosseumService.getTournament(tournamentId).subscribe({
      next: (details) => this.details.set(details),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament teams'),
    });
  }

  private loadRewards(tournamentId: string): void {
    this.colosseumService.getTournamentRewards(tournamentId).subscribe({
      next: (rewards) => this.rewards.set(rewards),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament rewards'),
    });
  }

  private loadRewardTiers(): void {
    this.colosseumService.getTournamentRewardTiers().subscribe({
      next: (tiers) => this.rewardTiers.set(tiers),
      error: (err) =>
        this.error.set(
          err.message ?? 'Failed to load tournament placement rewards',
        ),
    });
  }

  private loadArchives(): void {
    this.colosseumService.getTournamentHistory().subscribe({
      next: (history) => this.history.set(history),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament history'),
    });

    this.colosseumService.getTournamentHallOfFame().subscribe({
      next: (hallOfFame) => this.hallOfFame.set(hallOfFame),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament Hall of Fame'),
    });

    this.colosseumService.getTournamentSeasonLeaderboard().subscribe({
      next: (seasonLeaderboard) =>
        this.seasonLeaderboard.set(seasonLeaderboard),
      error: (err) =>
        this.error.set(
          err.message ?? 'Failed to load tournament season leaderboard',
        ),
    });
  }

  private runAction<T>(
    request: Observable<T>,
    successTitle: string,
    onSuccess: () => void,
  ): void {
    this.actionLoading.set(true);
    this.error.set(null);

    request.pipe(finalize(() => this.actionLoading.set(false))).subscribe({
      next: () => {
        this.toastService.showToast(successTitle, '', true);
        onSuccess();
      },
      error: (err: Error) => {
        const message = err.message ?? 'Tournament action failed';
        this.error.set(message);
        this.toastService.showToast('Tournament action failed', message, false);
      },
    });
  }
}
