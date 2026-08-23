import { Injectable } from '@angular/core';
import {
  RaidPlaybackBundle,
  RaidPlaybackFrame,
} from '../../api/raid/raid.service';
import { TowerCombatFrame } from '../../api/world-tower/world-tower.service';
import {
  AbilityStats,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../../shared/models/Dtos/combatResultDto';

export interface RaidCombatFrame extends TowerCombatFrame {
  waveNumber: number | null;
}

export interface RaidPlaybackPosition {
  combatTick: number;
  isWaveTransitionHold: boolean;
}

@Injectable({ providedIn: 'root' })
export class RaidPlaybackService {
  private readonly transitionTicksByBundle = new WeakMap<
    RaidPlaybackBundle,
    readonly number[]
  >();

  frameAtTick(
    bundle: RaidPlaybackBundle,
    tick: number,
    showDefeatedPreviousWave = false,
  ): RaidCombatFrame {
    if (!bundle.frames.length) {
      throw new Error('Raid playback bundle contains no frames.');
    }

    let low = 0;
    let high = bundle.frames.length - 1;
    while (low < high) {
      const middle = low + Math.floor((high - low + 1) / 2);
      if (bundle.frames[middle].tick <= tick) low = middle;
      else high = middle - 1;
    }
    return this.toCombatFrame(
      bundle,
      this.materializeFrame(bundle, low),
      showDefeatedPreviousWave,
    );
  }

  private materializeFrame(
    bundle: RaidPlaybackBundle,
    targetIndex: number,
  ): RaidPlaybackFrame {
    const target = bundle.frames[targetIndex];
    if (bundle.schemaVersion < 5 || target.isKeyframe) return target;

    let keyframeIndex = targetIndex;
    while (keyframeIndex > 0 && !bundle.frames[keyframeIndex].isKeyframe) {
      keyframeIndex--;
    }

    const entityStates = new Map<
      number,
      RaidPlaybackFrame['entityStates'][number]
    >();
    const entityTotals = new Map<
      number,
      RaidPlaybackFrame['entityTotals'][number]
    >();
    const abilityTotals = new Map<
      number,
      RaidPlaybackFrame['abilityTotals'][number]
    >();
    for (let index = keyframeIndex; index <= targetIndex; index++) {
      const frame = bundle.frames[index];
      frame.entityStates.forEach((state) =>
        entityStates.set(state.entityIndex, state),
      );
      frame.entityTotals.forEach((totals) =>
        entityTotals.set(totals.entityIndex, totals),
      );
      frame.abilityTotals.forEach((totals) =>
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

  playbackDurationMilliseconds(
    bundle: RaidPlaybackBundle,
    waveTransitionHoldMilliseconds: number,
  ): number {
    const combatDuration =
      (bundle.totalTicks / Math.max(1, bundle.ticksPerSecond)) * 1000;
    return (
      combatDuration +
      this.rearguardWaveTransitionTicks(bundle).length *
        Math.max(0, waveTransitionHoldMilliseconds)
    );
  }

  combatTickAtPlaybackElapsed(
    bundle: RaidPlaybackBundle,
    playbackElapsedMilliseconds: number,
    waveTransitionHoldMilliseconds: number,
  ): number {
    return this.playbackPositionAtElapsed(
      bundle,
      playbackElapsedMilliseconds,
      waveTransitionHoldMilliseconds,
    ).combatTick;
  }

  playbackPositionAtElapsed(
    bundle: RaidPlaybackBundle,
    playbackElapsedMilliseconds: number,
    waveTransitionHoldMilliseconds: number,
  ): RaidPlaybackPosition {
    const ticksPerSecond = Math.max(1, bundle.ticksPerSecond);
    const holdMilliseconds = Math.max(0, waveTransitionHoldMilliseconds);
    let remainingPlaybackMilliseconds = Math.max(
      0,
      playbackElapsedMilliseconds,
    );
    let previousCombatMilliseconds = 0;

    for (const transitionTick of this.rearguardWaveTransitionTicks(bundle)) {
      const transitionCombatMilliseconds =
        (transitionTick / ticksPerSecond) * 1000;
      const combatSegmentMilliseconds = Math.max(
        0,
        transitionCombatMilliseconds - previousCombatMilliseconds,
      );
      if (remainingPlaybackMilliseconds < combatSegmentMilliseconds) {
        return {
          combatTick: Math.min(
            bundle.totalTicks,
            Math.floor(
              ((previousCombatMilliseconds + remainingPlaybackMilliseconds) /
                1000) *
                ticksPerSecond,
            ),
          ),
          isWaveTransitionHold: false,
        };
      }

      remainingPlaybackMilliseconds -= combatSegmentMilliseconds;
      if (remainingPlaybackMilliseconds < holdMilliseconds)
        return {
          combatTick: transitionTick,
          isWaveTransitionHold: true,
        };

      remainingPlaybackMilliseconds -= holdMilliseconds;
      previousCombatMilliseconds = transitionCombatMilliseconds;
    }

    return {
      combatTick: Math.min(
        bundle.totalTicks,
        Math.floor(
          ((previousCombatMilliseconds + remainingPlaybackMilliseconds) /
            1000) *
            ticksPerSecond,
        ),
      ),
      isWaveTransitionHold: false,
    };
  }

  private toCombatFrame(
    bundle: RaidPlaybackBundle,
    frame: RaidPlaybackFrame,
    showDefeatedPreviousWave: boolean,
  ): RaidCombatFrame {
    const stateByEntity = new Map(
      frame.entityStates.map((state) => [state.entityIndex, state]),
    );
    const totalsByEntity = new Map(
      frame.entityTotals.map((totals) => [totals.entityIndex, totals]),
    );
    const abilityTotals = new Map(
      frame.abilityTotals.map((totals) => [totals.abilityIndex, totals]),
    );
    const spawnedEntities = bundle.entities.filter((entity) =>
      stateByEntity.has(entity.index),
    );
    const currentWaveNumber = this.currentRearguardWave(spawnedEntities);
    const waveNumber =
      showDefeatedPreviousWave && currentWaveNumber !== null
        ? (this.previousRearguardWave(spawnedEntities, currentWaveNumber) ??
          currentWaveNumber)
        : currentWaveNumber;
    const activeEntities =
      waveNumber === null
        ? spawnedEntities
        : spawnedEntities.filter((entity) => {
            if (entity.isFriendly) return true;
            const entityWave = this.rearguardWaveNumber(entity.id);
            return entityWave === null || entityWave === waveNumber;
          });
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
        partyNumber: entity.partyNumber,
        currentStagger: state.currentStagger ?? 0,
        maxStagger: state.maxStagger ?? 0,
        isStaggered: state.isStaggered ?? false,
        isStaggerRecovering: state.isStaggerRecovering ?? false,
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
            damageByType: values?.damageByType ?? [],
            totalHealing: values?.totalHealing ?? 0,
            totalBarrier: values?.totalBarrier ?? 0,
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
        team: entity.isFriendly ? 'Friendly' : 'Hostile',
        abilities,
        damageDone: totals?.damageDone ?? 0,
        damageTaken: totals?.damageTaken ?? 0,
        healingDone: totals?.healingDone ?? 0,
        healingReceived: totals?.healingReceived ?? 0,
        healthRegenerated: totals?.healthRegenerated ?? 0,
        barrierGenerated: totals?.barrierGenerated ?? 0,
        damageBlocked: totals?.damageBlocked ?? 0,
        threatGenerated: totals?.threatGenerated ?? 0,
        staggerContributed: totals?.staggerContributed ?? 0,
        staggerBreaks: totals?.staggerBreaks ?? 0,
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
      waveNumber,
    };
  }

  private currentRearguardWave(
    entities: RaidPlaybackBundle['entities'],
  ): number | null {
    const waves = entities
      .filter((entity) => !entity.isFriendly)
      .map((entity) => this.rearguardWaveNumber(entity.id))
      .filter((wave): wave is number => wave !== null);
    return waves.length ? Math.max(...waves) : null;
  }

  private previousRearguardWave(
    entities: RaidPlaybackBundle['entities'],
    currentWave: number,
  ): number | null {
    const previousWaves = entities
      .filter((entity) => !entity.isFriendly)
      .map((entity) => this.rearguardWaveNumber(entity.id))
      .filter((wave): wave is number => wave !== null && wave < currentWave);
    return previousWaves.length ? Math.max(...previousWaves) : null;
  }

  private rearguardWaveTransitionTicks(
    bundle: RaidPlaybackBundle,
  ): readonly number[] {
    const cached = this.transitionTicksByBundle.get(bundle);
    if (cached) return cached;

    const entityByIndex = new Map(
      bundle.entities.map((entity) => [entity.index, entity]),
    );
    const transitionTicks: number[] = [];
    let currentWave: number | null = null;
    for (let index = 0; index < bundle.frames.length; index++) {
      const frame = this.materializeFrame(bundle, index);
      const frameWave = this.currentRearguardWave(
        frame.entityStates
          .map((state) => entityByIndex.get(state.entityIndex))
          .filter((entity): entity is RaidPlaybackBundle['entities'][number] =>
            Boolean(entity),
          ),
      );
      if (frameWave === null) continue;
      if (currentWave !== null && frameWave > currentWave)
        transitionTicks.push(frame.tick);
      currentWave = Math.max(currentWave ?? frameWave, frameWave);
    }

    this.transitionTicksByBundle.set(bundle, transitionTicks);
    return transitionTicks;
  }

  private rearguardWaveNumber(entityId: string): number | null {
    const match = /^rearguard-wave-(\d+)-/i.exec(entityId);
    if (!match) return null;
    const wave = Number.parseInt(match[1], 10);
    return Number.isSafeInteger(wave) && wave > 0 ? wave : null;
  }
}
