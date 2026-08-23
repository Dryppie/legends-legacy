import { Injectable, inject } from '@angular/core';
import { Observable, catchError, shareReplay, throwError } from 'rxjs';
import {
  RegionBossPlaybackBundle,
  RegionBossPlaybackAbilityTotals,
  RegionBossPlaybackEntityState,
  RegionBossPlaybackEntityTotals,
  RegionBossPlaybackFrame,
  RegionBossService,
} from '../../api/region-boss/region-boss.service';
import { TowerCombatFrame } from '../../api/world-tower/world-tower.service';
import {
  AbilityStats,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../../shared/models/Dtos/combatResultDto';

@Injectable({ providedIn: 'root' })
export class RegionBossPlaybackService {
  private readonly regionBosses = inject(RegionBossService);
  private readonly bundles = new Map<
    string,
    Observable<RegionBossPlaybackBundle>
  >();

  getBundle(runId: string): Observable<RegionBossPlaybackBundle> {
    const cached = this.bundles.get(runId);
    if (cached) return cached;

    const request = this.regionBosses.getPlaybackBundle(runId).pipe(
      catchError((error: unknown) => {
        this.bundles.delete(runId);
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.bundles.set(runId, request);
    return request;
  }

  frameAtTick(
    bundle: RegionBossPlaybackBundle,
    tick: number,
  ): TowerCombatFrame {
    if (!bundle.frames.length) {
      throw new Error('Region Boss playback bundle contains no frames.');
    }

    let low = 0;
    let high = bundle.frames.length - 1;
    while (low < high) {
      const middle = low + Math.floor((high - low + 1) / 2);
      if (bundle.frames[middle].tick <= tick) low = middle;
      else high = middle - 1;
    }

    return this.toCombatFrame(bundle, this.materializeFrame(bundle, low));
  }

  private materializeFrame(
    bundle: RegionBossPlaybackBundle,
    targetIndex: number,
  ): RegionBossPlaybackFrame {
    const target = bundle.frames[targetIndex];
    if (bundle.schemaVersion < 3 || target.isKeyframe) return target;

    let keyframeIndex = targetIndex;
    while (keyframeIndex > 0 && !bundle.frames[keyframeIndex].isKeyframe) {
      keyframeIndex--;
    }

    const entityStates = new Map<number, RegionBossPlaybackEntityState>();
    const entityTotals = new Map<number, RegionBossPlaybackEntityTotals>();
    const abilityTotals = new Map<number, RegionBossPlaybackAbilityTotals>();
    for (let index = keyframeIndex; index <= targetIndex; index++) {
      const frame = bundle.frames[index];
      frame.entityStates?.forEach((state) =>
        entityStates.set(state.entityIndex, state),
      );
      frame.entityTotals?.forEach((totals) =>
        entityTotals.set(totals.entityIndex, totals),
      );
      frame.abilityTotals?.forEach((totals) =>
        abilityTotals.set(totals.abilityIndex, totals),
      );
    }

    return {
      ...target,
      entityStates: [...entityStates.values()].sort(
        (left, right) => left.entityIndex - right.entityIndex,
      ),
      entityTotals: [...entityTotals.values()].sort(
        (left, right) => left.entityIndex - right.entityIndex,
      ),
      abilityTotals: [...abilityTotals.values()].sort(
        (left, right) => left.abilityIndex - right.abilityIndex,
      ),
    };
  }

  private toCombatFrame(
    bundle: RegionBossPlaybackBundle,
    frame: RegionBossPlaybackFrame,
  ): TowerCombatFrame {
    if (
      !bundle.entities ||
      !bundle.abilities ||
      !frame.entityStates ||
      !frame.entityTotals ||
      !frame.abilityTotals
    ) {
      return {
        sequence: frame.sequence,
        tick: frame.tick,
        friendly: frame.friendly ?? [],
        hostile: frame.hostile ?? [],
        entityStats: frame.entityStats ?? [],
        events: [],
        isFinal: frame.isFinal,
        outcome: null,
      };
    }

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
        health: state.health,
        maxHealth: entity.maxHealth,
        barrier: state.barrier,
        level: entity.level,
        partyNumber: entity.partyNumber,
        currentStagger: state.currentStagger,
        maxStagger: state.maxStagger,
        isStaggered: state.isStaggered,
        isStaggerRecovering: state.isStaggerRecovering,
      };
    });
    const stats = activeEntities.map((entity): EntityStats => {
      const state = stateByEntity.get(entity.index)!;
      const totals = totalsByEntity.get(entity.index);
      const abilities = bundle
        .abilities!.filter((ability) => ability.entityIndex === entity.index)
        .map((ability): AbilityStats => {
          const values = abilityTotals.get(ability.index);
          return {
            name: ability.name,
            uses: values?.uses ?? 0,
            totalDamage: values?.totalDamage ?? 0,
            totalHealing: values?.totalHealing ?? 0,
            totalBarrier: values?.totalBarrier ?? 0,
            damageByType: values?.damageByType ?? [],
            totalThreat: values?.totalThreat ?? 0,
            hits: 0,
            crits: 0,
            summons: 0,
            stuns: 0,
            selfDamage: 0,
            alliedDamage: 0,
            totalStagger: values?.totalStagger ?? 0,
            staggerBreaks: values?.staggerBreaks ?? 0,
          };
        });
      return {
        entityId: entity.id,
        entityName: entity.name,
        abilities,
        damageDone: totals?.damageDone ?? 0,
        damageTaken: totals?.damageTaken ?? 0,
        healingDone: totals?.healingDone ?? 0,
        healingReceived: totals?.healingReceived ?? 0,
        healthRegenerated: totals?.healthRegenerated ?? 0,
        healthRegenerationPotential: 0,
        healthRegenerationOverhealed: 0,
        healthRegenerationPulses: 0,
        selfDamageDone: 0,
        selfDamageTaken: 0,
        alliedDamageDone: 0,
        alliedDamageTaken: 0,
        team: entity.isFriendly ? 'Friendly' : 'Hostile',
        barrierGenerated: totals?.barrierGenerated ?? 0,
        damageBlocked: totals?.damageBlocked ?? 0,
        threatGenerated: totals?.threatGenerated ?? 0,
        staggerContributed: totals?.staggerContributed ?? 0,
        staggerBreaks: totals?.staggerBreaks ?? 0,
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
      outcome: null,
    };
  }
}
