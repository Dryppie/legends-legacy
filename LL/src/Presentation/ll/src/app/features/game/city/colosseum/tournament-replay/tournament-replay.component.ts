import { DatePipe, NgIf } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ColosseumService } from '../../../../../core/services/api/colosseum/colosseum.service';
import { BattleType } from '../../../../../core/state/combat-state/combatState';
import { CombatStateService } from '../../../../../core/state/combat-state/combat-state.service';
import { CombatComponent } from '../../../../../shared/components/combat/combat.component';
import { CombatResultDto } from '../../../../../shared/models/Dtos/combatResultDto';
import {
  TournamentBracket,
  TournamentDetails,
  TournamentMatch,
} from '../../../../../shared/models/Dtos/colosseum/tournamentGrounds';

@Component({
  selector: 'app-tournament-replay',
  standalone: true,
  imports: [CombatComponent, DatePipe, NgIf, RouterLink],
  templateUrl: './tournament-replay.component.html',
})
export class TournamentReplayComponent implements OnInit {
  readonly battleType = BattleType.Colosseum;
  readonly tournamentId = signal<string | null>(null);
  readonly matchId = signal<string | null>(null);
  readonly details = signal<TournamentDetails | null>(null);
  readonly bracket = signal<TournamentBracket | null>(null);
  readonly replay = signal<CombatResultDto | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly round = computed(() => {
    const bracket = this.bracket();
    const matchId = this.matchId();
    return bracket?.rounds.find((round) =>
      round.matches.some((match) => match.id === matchId),
    ) ?? null;
  });

  readonly match = computed<TournamentMatch | null>(() => {
    const matchId = this.matchId();
    return this.round()?.matches.find((match) => match.id === matchId) ?? null;
  });

  constructor(
    private readonly route: ActivatedRoute,
    private readonly colosseumService: ColosseumService,
    public readonly combatStateService: CombatStateService,
  ) {}

  ngOnInit(): void {
    const tournamentId = this.route.snapshot.paramMap.get('tournamentId');
    const matchId = this.route.snapshot.paramMap.get('matchId');
    this.tournamentId.set(tournamentId);
    this.matchId.set(matchId);

    if (!tournamentId || !matchId) {
      this.error.set('Replay link is missing tournament or match information.');
      return;
    }

    this.load(tournamentId, matchId);
  }

  startReplay(): void {
    const replay = this.replay();
    if (!replay) return;

    this.colosseumService.startTournamentReplay({ ...replay });
  }

  skipBattle(): void {
    this.colosseumService.skipColosseumMatch();
  }

  teamLabel(
    team: { name: string; seed?: number | null; memberCount?: number | null } | null | undefined,
  ): string {
    if (!team) return 'Pending';
    const name = team.seed ? `#${team.seed} ${team.name}` : team.name;
    return team.memberCount ? `${name} (${team.memberCount}/3)` : name;
  }

  outcomeLabel(match: TournamentMatch | null): string {
    if (!match) return 'Replay';
    if (match.status === 'Bye') return 'Advanced by bye';
    if (match.status !== 'Completed') return this.enumLabel(match.status);

    const winner =
      match.winnerTeamId === match.playerOne?.teamId
        ? match.playerOne
        : match.playerTwo;
    return winner ? `${winner.name} advanced` : this.enumLabel(match.outcome);
  }

  enumLabel(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .replace(/_/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }

  private load(tournamentId: string, matchId: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.colosseumService.getTournament(tournamentId).subscribe({
      next: (details) => this.details.set(details),
      error: (err: Error) =>
        this.error.set(err.message ?? 'Failed to load tournament details'),
    });

    this.colosseumService.getTournamentBracket(tournamentId).subscribe({
      next: (bracket) => this.bracket.set(bracket),
      error: (err: Error) =>
        this.error.set(err.message ?? 'Failed to load tournament bracket'),
    });

    this.colosseumService
      .getTournamentMatchReplay(tournamentId, matchId)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (replay) => {
          this.replay.set(replay);
          this.colosseumService.startTournamentReplay({ ...replay });
        },
        error: (err: Error) =>
          this.error.set(err.message ?? 'Failed to load tournament replay'),
      });
  }
}
