import { DatePipe, NgFor, NgIf } from '@angular/common';
import { Component, OnInit, computed, effect, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Observable, finalize } from 'rxjs';
import { ColosseumService } from '../../../../../core/services/api/colosseum/colosseum.service';
import { ToastService } from '../../../../../core/services/client-side/components/toast/toast.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';
import { TournamentGroundsUpdated } from '../../../../../core/services/real-time/colosseum/tournament-grounds-updated';
import {
  TournamentBracket,
  TournamentDetails,
  TournamentGroundsStatus,
  TournamentHallOfFameEntry,
  TournamentHistoryEntry,
  TournamentMatch,
  TournamentParticipant,
  TournamentRewardGrant,
  TournamentSeasonLeaderboardEntry,
  TournamentSummary,
  TournamentTeam,
  TournamentTeamInvite,
} from '../../../../../shared/models/Dtos/colosseum/tournamentGrounds';

@Component({
  selector: 'app-tournament-grounds',
  standalone: true,
  imports: [DatePipe, NgFor, NgIf, RouterLink],
  templateUrl: './tournament-grounds.component.html',
})
export class TournamentGroundsComponent implements OnInit {
  readonly status = signal<TournamentGroundsStatus | null>(null);
  readonly details = signal<TournamentDetails | null>(null);
  readonly bracket = signal<TournamentBracket | null>(null);
  readonly rewards = signal<TournamentRewardGrant[]>([]);
  readonly history = signal<TournamentHistoryEntry[]>([]);
  readonly hallOfFame = signal<TournamentHallOfFameEntry[]>([]);
  readonly seasonLeaderboard = signal<TournamentSeasonLeaderboardEntry[]>([]);
  readonly latestRealtimeUpdate = signal<TournamentGroundsUpdated | null>(null);
  readonly loading = signal(false);
  readonly actionLoading = signal(false);
  readonly error = signal<string | null>(null);

  readonly current = computed(() => this.status()?.currentTournament ?? null);
  readonly upcoming = computed(() => this.status()?.upcomingTournaments ?? []);
  readonly recent = computed(() => this.status()?.recentTournaments ?? []);
  readonly teams = computed(() => this.details()?.teams ?? []);
  readonly playerTeam = computed(() => this.teams().find((team) => team.isPlayerTeam) ?? null);
  readonly openTeams = computed(() => this.teams().filter((team) => team.isOpen));
  readonly unassignedParticipants = computed(() =>
    this.details()?.participants.filter(
      (participant) =>
        !participant.teamId && participant.status !== 'Withdrawn',
    ) ?? [],
  );
  readonly playerPendingInvites = computed(() => {
    const participantId = this.current()?.playerParticipantId;
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
  private lastRealtimeUpdateId: string | null = null;

  constructor(
    private readonly colosseumService: ColosseumService,
    private readonly toastService: ToastService,
    private readonly eventService: GameEventService,
  ) {
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
        const currentTournamentId = this.current()?.id;
        if (!currentTournamentId || envelope.payload.tournamentId === currentTournamentId) {
          this.refresh();
        } else if (
          envelope.payload.event === 'TournamentCompleted' ||
          envelope.payload.event === 'TournamentRewardsAvailable'
        ) {
          this.loadArchives();
        }
      },
      { allowSignalWrites: true },
    );
  }

  ngOnInit(): void {
    this.refresh();
  }

  refresh(): void {
    this.loading.set(true);
    this.error.set(null);

    this.colosseumService
      .getTournamentGroundsStatus()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (status) => {
          this.status.set(status);
          const tournamentId = status.currentTournament?.id;
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
        },
        error: (err) =>
          this.error.set(err.message ?? 'Failed to load tournament grounds'),
      });
  }

  register(tournament: TournamentSummary): void {
    this.runAction(
      this.colosseumService.registerTournament(tournament.id),
      'Registration complete',
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
      () => this.refresh(),
    );
  }

  applyToTeam(tournament: TournamentSummary | null, team: TournamentTeam): void {
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
    team: { name: string; seed?: number | null; memberCount?: number | null } | null | undefined,
  ): string {
    if (!team) return 'Pending';
    const name = team.seed ? `#${team.seed} ${team.name}` : team.name;
    return team.memberCount ? `${name} (${team.memberCount}/3)` : name;
  }

  outcomeLabel(match: TournamentBracket['rounds'][number]['matches'][number]): string {
    if (match.status === 'Bye') return 'Advanced by bye';
    if (match.status !== 'Completed') return this.enumLabel(match.status);
    const winner =
      match.winnerTeamId === match.playerOne?.teamId
        ? match.playerOne
        : match.playerTwo;
    return winner ? `${winner.name} advanced` : this.enumLabel(match.outcome);
  }

  playerStatusLabel(tournament: TournamentSummary): string {
    if (!tournament.playerStatus) return 'Not entered';
    if (tournament.playerStatus === 'Champion') return 'Champion';
    if (tournament.playerFinalPlacement) {
      return `${this.enumLabel(tournament.playerStatus)} · Place ${tournament.playerFinalPlacement}`;
    }

    return this.enumLabel(tournament.playerStatus);
  }

  tournamentResultLabel(tournament: TournamentSummary): string {
    if (tournament.status === 'Cancelled') {
      return tournament.cancellationReason ?? 'Cancelled';
    }

    if (tournament.playerFinalPlacement === 1) return 'Champion';
    if (tournament.playerFinalPlacement) {
      return `Placed ${tournament.playerFinalPlacement}`;
    }

    return this.enumLabel(tournament.playerStatus ?? tournament.status);
  }

  rewardLabel(reward: TournamentRewardGrant): string {
    if (reward.placement === 1) return 'Champion reward';
    if (reward.placement) return `Place ${reward.placement}`;
    return 'Participation reward';
  }

  historyResultLabel(entry: TournamentHistoryEntry): string {
    if (entry.status === 'Cancelled') return entry.cancellationReason ?? 'Cancelled';
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
        return 'Rounds resolving';
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
      next: (bracket) => this.bracket.set(bracket),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament bracket'),
    });
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
      next: (seasonLeaderboard) => this.seasonLeaderboard.set(seasonLeaderboard),
      error: (err) =>
        this.error.set(err.message ?? 'Failed to load tournament season leaderboard'),
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
