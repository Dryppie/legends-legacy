import { Injectable, inject } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';
import { ColosseumService } from '../../api/colosseum/colosseum.service';
import {
  TournamentCombatFrame,
  TournamentPlaybackBundle,
  TournamentPlaybackFrame,
} from '../../../../shared/models/Dtos/colosseum/tournamentGrounds';
import {
  AbilityStats,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({ providedIn: 'root' })
export class TournamentPlaybackService {
  private readonly colosseum = inject(ColosseumService);
  private readonly bundles = new Map<
    string,
    Observable<TournamentPlaybackBundle>
  >();

  getBundle(
    tournamentId: string,
    matchId: string,
    etag: string,
  ): Observable<TournamentPlaybackBundle> {
    const key = `${matchId}:${etag}`;
    const cached = this.bundles.get(key);
    if (cached) return cached;

    for (const existingKey of this.bundles.keys()) {
      if (existingKey.startsWith(`${matchId}:`))
        this.bundles.delete(existingKey);
    }
    const request = this.colosseum
      .getTournamentMatchPlaybackBundle(tournamentId, matchId)
      .pipe(
        catchError((error: unknown) => {
          this.bundles.delete(key);
          return throwError(() => error);
        }),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    this.bundles.set(key, request);
    return request;
  }

  frameAtTick(
    bundle: TournamentPlaybackBundle,
    tick: number,
  ): TournamentCombatFrame {
    if (!bundle.frames.length) {
      throw new Error('Tournament playback bundle contains no frames.');
    }

    let low = 0;
    let high = bundle.frames.length - 1;
    while (low < high) {
      const middle = low + Math.floor((high - low + 1) / 2);
      if (bundle.frames[middle].tick <= tick) low = middle;
      else high = middle - 1;
    }
    return this.toCombatFrame(bundle, bundle.frames[low]);
  }

  private toCombatFrame(
    bundle: TournamentPlaybackBundle,
    frame: TournamentPlaybackFrame,
  ): TournamentCombatFrame {
    const stateByEntity = new Map(
      frame.entityStates.map((state) => [state.entityIndex, state]),
    );
    const totalsByEntity = new Map(
      frame.entityTotals.map((totals) => [totals.entityIndex, totals]),
    );
    const abilityTotals = new Map(
      frame.abilityTotals.map((totals) => [totals.abilityIndex, totals]),
    );
    const activeEntities = bundle.entities.filter((entity) =>
      stateByEntity.has(entity.index),
    );
    const entities = activeEntities.map((entity): SimpleCombatEntityDto => {
      const state = stateByEntity.get(entity.index)!;
      return {
        id: entity.id,
        name: entity.name,
        imagePath: entity.imagePath,
        level: entity.level,
        maxHealth: entity.maxHealth,
        health: state.health,
        barrier: state.barrier,
      };
    });
    const stats = activeEntities.map((entity): EntityStats => {
      const state = stateByEntity.get(entity.index)!;
      const totals = totalsByEntity.get(entity.index);
      const abilities = bundle.abilities
        .filter((ability) => ability.entityIndex === entity.index)
        .map((ability): AbilityStats => {
          const values = abilityTotals.get(ability.index);
          return {
            name: ability.name,
            uses: values?.uses ?? 0,
            totalDamage: values?.totalDamage ?? 0,
            totalHealing: values?.totalHealing ?? 0,
            totalBarrier: values?.totalBarrier ?? 0,
            hits: 0,
            crits: 0,
            summons: 0,
            stuns: 0,
            selfDamage: 0,
            alliedDamage: 0,
          };
        });
      return {
        entityId: entity.id,
        entityName: entity.name,
        team: entity.isFriendly ? 'Friendly' : 'Hostile',
        abilities,
        damageDone: totals?.damageDone ?? 0,
        damageTaken: totals?.damageTaken ?? 0,
        healingDone: totals?.healingDone ?? 0,
        healingReceived: totals?.healingReceived ?? 0,
        healthRegenerated: totals?.healthRegenerated ?? 0,
        barrierGenerated: totals?.barrierGenerated ?? 0,
        damageBlocked: totals?.damageBlocked ?? 0,
        healthRegenerationPotential: 0,
        healthRegenerationOverhealed: 0,
        healthRegenerationPulses: 0,
        selfDamageDone: 0,
        selfDamageTaken: 0,
        alliedDamageDone: 0,
        alliedDamageTaken: 0,
        health: state.health,
        maxHealth: entity.maxHealth,
        barrier: state.barrier,
      };
    });

    return {
      sequence: frame.sequence,
      tick: frame.tick,
      friendly: entities.filter((_, index) => activeEntities[index].isFriendly),
      hostile: entities.filter((_, index) => !activeEntities[index].isFriendly),
      entityStats: stats,
      events: [],
      isFinal: frame.isFinal,
      outcome: frame.outcome,
    };
  }
}
