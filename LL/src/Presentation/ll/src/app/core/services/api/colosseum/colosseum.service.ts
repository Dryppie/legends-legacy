import { Injectable } from '@angular/core';
import { ApiService, VersionedMutationResult } from '../api.service';
import { catchError, Observable, throwError } from 'rxjs';
import { CombatService } from '../../client-side/combat/combat.service';
import { CombatResultDto } from '../../../../shared/models/Dtos/combatResultDto';
import { ColosseumMatchResult } from '../../../../shared/models/Dtos/colosseum/colosseumMatchResult';
import { ArenaTicketStatus } from '../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ArenaOpponentPreview } from '../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { LeaderboardEntry } from '../../../../shared/models/Dtos/leaderboard/leaderboardEntry';
import {
  ArenaDefenseStatus,
  ColosseumStatus,
} from '../../../../shared/models/Dtos/colosseum/colosseumStatus';
import { StartArenaBattleResponse } from '../../../../shared/models/Dtos/colosseum/startArenaBattleResponse';
import {
  ChampionMarket,
  ChampionMarketPurchaseResponse,
} from '../../../../shared/models/Dtos/colosseum/championMarket';
import {
  ClaimTournamentRewardsResponse,
  CreateTournamentTeamResponse,
  RegisterTournamentResponse,
  StartDevelopmentTournamentResponse,
  TournamentBracket,
  TournamentDetails,
  TournamentGroundsStatus,
  TournamentHallOfFameEntry,
  TournamentHistoryEntry,
  TournamentRewardGrant,
  TournamentRewardTier,
  TournamentSeasonLeaderboardEntry,
  TournamentPlaybackManifest,
  TournamentPlaybackBundle,
  TournamentTeamActionResponse,
  WithdrawTournamentResponse,
} from '../../../../shared/models/Dtos/colosseum/tournamentGrounds';

const COLOSSEUM_RESPONSE_HANDLED_SCOPES = ['colosseum'] as const;

@Injectable({
  providedIn: 'root',
})
export class ColosseumService {
  constructor(
    private apiService: ApiService,
    private combatService: CombatService,
  ) {}

  public getArenaOpponents(): Observable<ArenaOpponentPreview[]> {
    return this.apiService.get('colosseum/opponents').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena opponents'));
      }),
    );
  }

  getStatus(): Observable<ColosseumStatus> {
    return this.apiService.get('colosseum/status').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get colosseum status'));
      }),
    );
  }

  updateDefenseSnapshot(): Observable<
    VersionedMutationResult<ArenaDefenseStatus>
  > {
    return this.apiService
      .postVersioned<ArenaDefenseStatus>(
        'colosseum/defense-snapshot',
        {},
        {
          stateSyncScopesHandledByResponse: COLOSSEUM_RESPONSE_HANDLED_SCOPES,
        },
      )
      .pipe(
        catchError(() => {
          return throwError(() => new Error('Failed to update arena defense'));
        }),
      );
  }

  getChampionMarket(): Observable<ChampionMarket> {
    return this.apiService.get('colosseum/market').pipe(
      catchError(() => {
        return throwError(() => new Error("Failed to get champion's market"));
      }),
    );
  }

  purchaseChampionMarketItem(
    itemId: string,
    quantity = 1,
  ): Observable<VersionedMutationResult<ChampionMarketPurchaseResponse>> {
    return this.apiService
      .postVersioned<ChampionMarketPurchaseResponse>(
        'colosseum/market/purchase',
        {
          itemId,
          quantity,
        },
        {
          stateSyncScopesHandledByResponse: COLOSSEUM_RESPONSE_HANDLED_SCOPES,
        },
      )
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.message ?? "Failed to purchase champion's market item",
              ),
          );
        }),
      );
  }

  getArenaTicketStatus(): Observable<ArenaTicketStatus> {
    return this.apiService.get('colosseum/getArenaTicketStatus').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena tickets'));
      }),
    );
  }

  getColosseumRankings(): Observable<LeaderboardEntry[]> {
    return this.apiService.get('colosseum/getRankings').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena rankings'));
      }),
    );
  }

  getColosseumMatchResults(): Observable<ColosseumMatchResult[]> {
    return this.apiService.get('colosseum/getColosseumMatchResults').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get arena match results'));
      }),
    );
  }

  public startArenaBattle(
    opponentId: string,
  ): Observable<StartArenaBattleResponse> {
    return this.apiService.post('colosseum/battle', { opponentId }).pipe(
      catchError((err) => {
        return throwError(
          () => new Error(err.message ?? 'Failed to start match'),
        );
      }),
    );
  }

  skipColosseumMatch() {
    this.combatService.skipCurrentColosseum();
  }

  getTournamentGroundsStatus(): Observable<TournamentGroundsStatus> {
    return this.apiService.get('colosseum/tournaments/status').pipe(
      catchError(() => {
        return throwError(
          () => new Error('Failed to get tournament grounds status'),
        );
      }),
    );
  }

  startDevelopmentTournament(): Observable<StartDevelopmentTournamentResponse> {
    return this.apiService
      .post('colosseum/tournaments/development/start', {})
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.errorMessage ??
                  err.message ??
                  'Failed to start development tournament',
              ),
          );
        }),
      );
  }

  getTournament(tournamentId: string): Observable<TournamentDetails> {
    return this.apiService.get(`colosseum/tournaments/${tournamentId}`).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get tournament details'));
      }),
    );
  }

  getTournamentHistory(): Observable<TournamentHistoryEntry[]> {
    return this.apiService.get('colosseum/tournaments/history').pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get tournament history'));
      }),
    );
  }

  getTournamentHallOfFame(): Observable<TournamentHallOfFameEntry[]> {
    return this.apiService.get('colosseum/tournaments/hall-of-fame').pipe(
      catchError(() => {
        return throwError(
          () => new Error('Failed to get tournament Hall of Fame'),
        );
      }),
    );
  }

  getTournamentSeasonLeaderboard(): Observable<
    TournamentSeasonLeaderboardEntry[]
  > {
    return this.apiService.get('colosseum/tournaments/season-leaderboard').pipe(
      catchError(() => {
        return throwError(
          () => new Error('Failed to get tournament season leaderboard'),
        );
      }),
    );
  }

  getTournamentBracket(tournamentId: string): Observable<TournamentBracket> {
    return this.apiService
      .get(`colosseum/tournaments/${tournamentId}/bracket`)
      .pipe(
        catchError(() => {
          return throwError(
            () => new Error('Failed to get tournament bracket'),
          );
        }),
      );
  }

  getTournamentMatchReplay(
    tournamentId: string,
    matchId: string,
  ): Observable<CombatResultDto> {
    return this.apiService
      .get(`colosseum/tournaments/${tournamentId}/matches/${matchId}/replay`)
      .pipe(
        catchError(() => {
          return throwError(() => new Error('Failed to get tournament replay'));
        }),
      );
  }

  getTournamentMatchPlayback(
    tournamentId: string,
    matchId: string,
  ): Observable<TournamentPlaybackManifest> {
    return this.apiService.get(
      `colosseum/tournaments/${tournamentId}/matches/${matchId}/playback`,
    );
  }

  getTournamentMatchPlaybackBundle(
    tournamentId: string,
    matchId: string,
  ): Observable<TournamentPlaybackBundle> {
    return this.apiService.get(
      `colosseum/tournaments/${tournamentId}/matches/${matchId}/playback/bundle`,
    );
  }

  startTournamentReplay(replay: CombatResultDto): void {
    this.combatService.startColosseumMatchSimulation(replay);
  }

  registerTournament(
    tournamentId: string,
  ): Observable<RegisterTournamentResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/register`, {})
      .pipe(
        catchError((err) => {
          return throwError(
            () => new Error(err.message ?? 'Failed to register for tournament'),
          );
        }),
      );
  }

  updateTournamentLoadout(
    tournamentId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/loadout`, {})
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(err.message ?? 'Failed to update tournament loadout'),
          );
        }),
      );
  }

  withdrawTournament(
    tournamentId: string,
  ): Observable<WithdrawTournamentResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/withdraw`, {})
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(err.message ?? 'Failed to withdraw from tournament'),
          );
        }),
      );
  }

  createTournamentTeam(
    tournamentId: string,
    name: string,
  ): Observable<CreateTournamentTeamResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/teams`, { name })
      .pipe(
        catchError((err) => {
          return throwError(
            () => new Error(err.message ?? 'Failed to create tournament team'),
          );
        }),
      );
  }

  inviteTournamentTeamMember(
    tournamentId: string,
    teamId: string,
    invitedParticipantId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/teams/${teamId}/invite`, {
        invitedParticipantId,
      })
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.message ?? 'Failed to invite tournament team member',
              ),
          );
        }),
      );
  }

  acceptTournamentTeamInvite(
    inviteId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(`colosseum/tournaments/team-invites/${inviteId}/accept`, {})
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.message ?? 'Failed to accept tournament team invite',
              ),
          );
        }),
      );
  }

  applyToTournamentTeam(
    tournamentId: string,
    teamId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(`colosseum/tournaments/${tournamentId}/teams/${teamId}/apply`, {})
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(err.message ?? 'Failed to apply to tournament team'),
          );
        }),
      );
  }

  acceptTournamentTeamApplication(
    applicationId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(
        `colosseum/tournaments/team-applications/${applicationId}/accept`,
        {},
      )
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(
                err.message ?? 'Failed to accept tournament team application',
              ),
          );
        }),
      );
  }

  kickTournamentTeamMember(
    tournamentId: string,
    teamId: string,
    participantId: string,
  ): Observable<TournamentTeamActionResponse> {
    return this.apiService
      .post(
        `colosseum/tournaments/${tournamentId}/teams/${teamId}/members/${participantId}/kick`,
        {},
      )
      .pipe(
        catchError((err) => {
          return throwError(
            () =>
              new Error(err.message ?? 'Failed to kick tournament team member'),
          );
        }),
      );
  }

  getTournamentRewards(
    tournamentId?: string,
  ): Observable<TournamentRewardGrant[]> {
    const path = tournamentId
      ? `colosseum/tournaments/${tournamentId}/rewards`
      : 'colosseum/tournaments/rewards';
    return this.apiService.get(path).pipe(
      catchError(() => {
        return throwError(() => new Error('Failed to get tournament rewards'));
      }),
    );
  }

  getTournamentRewardTiers(): Observable<TournamentRewardTier[]> {
    return this.apiService.get('colosseum/tournaments/reward-tiers').pipe(
      catchError(() => {
        return throwError(
          () => new Error('Failed to get tournament placement rewards'),
        );
      }),
    );
  }

  claimTournamentRewards(
    tournamentId?: string,
  ): Observable<ClaimTournamentRewardsResponse> {
    const path = tournamentId
      ? `colosseum/tournaments/${tournamentId}/rewards/claim`
      : 'colosseum/tournaments/rewards/claim';
    return this.apiService.post(path, {}).pipe(
      catchError((err) => {
        return throwError(
          () => new Error(err.message ?? 'Failed to claim tournament rewards'),
        );
      }),
    );
  }
}
