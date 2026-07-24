import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import {
  AbilityStats,
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';
import { RegularButtonComponent } from '../../custom-components/buttons/regular-button/regular-button.component';

type CombatTeamName = 'Friendly' | 'Hostile';
type AbilitySortColumn = 'uses' | 'damage' | 'healing' | 'barrier';
type SortDirection = 'asc' | 'desc';
type StatsParticipant = {
  id: string;
  name: string;
  team: CombatTeamName;
  entity: SimpleCombatEntityDto;
};

@Component({
    selector: 'app-combat-entity-stats',
    imports: [NgIf, NgFor, NgClass, DecimalPipe, RegularButtonComponent],
    templateUrl: './combat-entity-stats.component.html'
})
export class CombatEntityStatsComponent implements OnChanges {
  @Input() playerTeam: SimpleCombatEntityDto[] = [];
  @Input() enemyTeam: SimpleCombatEntityDto[] = [];
  @Input() entityStats!: EntityStats[];

  selectedStats: EntityStats | null = null;
  selectedEntityId: string = '';
  selectedEntityName: string = '';
  abilitySortColumn: AbilitySortColumn = 'damage';
  abilitySortDirection: SortDirection = 'desc';

  playerParticipants: StatsParticipant[] = [];
  enemyParticipants: StatsParticipant[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['entityStats'] ||
      changes['playerTeam'] ||
      changes['enemyTeam']
    ) {
      this.playerParticipants = this.buildParticipants(
        'Friendly',
        this.playerTeam,
      );
      this.enemyParticipants = this.buildParticipants(
        'Hostile',
        this.enemyTeam,
      );
      this.refreshSelection();
    }
  }

  selectEntity(id: string) {
    this.selectedStats =
      this.entityStats?.find((stats) => stats.entityId === id) ??
      this.createEmptyStats(id);
    const entity = [...this.playerParticipants, ...this.enemyParticipants].find(
      (participant) => participant.id === id,
    );
    this.selectedEntityId = entity?.id ?? '';
    this.selectedEntityName = entity?.name ?? '';
  }

  statsFor(id: string): EntityStats | null {
    return this.entityStats?.find((stats) => stats.entityId === id) ?? null;
  }

  healthPercentage(entity: SimpleCombatEntityDto): number {
    if (entity.maxHealth <= 0) return 0;
    return Math.max(0, Math.min(100, (entity.health / entity.maxHealth) * 100));
  }

  barrierPercentage(entity: SimpleCombatEntityDto): number {
    if (entity.maxHealth <= 0) return 0;
    const availableWidth = 100 - this.healthPercentage(entity);
    return Math.max(
      0,
      Math.min(availableWidth, (entity.barrier / entity.maxHealth) * 100),
    );
  }

  barrierStartPercentage(entity: SimpleCombatEntityDto): number {
    return this.healthPercentage(entity);
  }

  sortAbilities(column: AbilitySortColumn): void {
    if (this.abilitySortColumn === column) {
      this.abilitySortDirection =
        this.abilitySortDirection === 'desc' ? 'asc' : 'desc';
      return;
    }

    this.abilitySortColumn = column;
    this.abilitySortDirection = 'desc';
  }

  sortIndicator(column: AbilitySortColumn): string {
    if (this.abilitySortColumn !== column) return '';
    return this.abilitySortDirection === 'desc' ? '↓' : '↑';
  }

  sortAriaLabel(column: AbilitySortColumn): string {
    const nextDirection =
      this.abilitySortColumn === column && this.abilitySortDirection === 'desc'
        ? 'ascending'
        : 'descending';
    return 'Sort by ' + column + ' ' + nextDirection;
  }

  averagePrimaryOutput(ability: AbilityStats): number {
    if (ability.uses <= 0) return 0;
    return this.primaryOutputTotal(ability) / ability.uses;
  }

  abilityBarPercentage(ability: AbilityStats): number {
    const maximum = Math.max(
      0,
      ...(this.selectedStats?.abilities ?? []).map((item) =>
        this.primaryOutputTotal(item),
      ),
    );
    if (maximum <= 0) return 0;
    return (this.primaryOutputTotal(ability) / maximum) * 100;
  }

  abilityBarClass(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'bg-primary/65';
    if (ability.totalHealing > 0) return 'bg-success/65';
    if (ability.totalBarrier > 0) return 'bg-[#8ecbff]/55';
    return 'bg-white/10';
  }

  abilityCategory(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'Attack';
    if (ability.totalHealing > 0) return 'Healing';
    if (ability.totalBarrier > 0) return 'Barrier';
    return 'Utility';
  }

  abilityCategoryClass(ability: AbilityStats): string {
    if (ability.totalDamage > 0) return 'border-primary/60 text-primary';
    if (ability.totalHealing > 0) return 'border-success/60 ll-text-success';
    if (ability.totalBarrier > 0) return 'border-[#8ecbff]/60 ll-text-info';
    return 'border-white/20 text-secondary';
  }

  get sortedAbilities(): AbilityStats[] {
    const direction = this.abilitySortDirection === 'asc' ? 1 : -1;
    return [...(this.selectedStats?.abilities ?? [])].sort((left, right) => {
      const difference =
        this.abilitySortValue(left) - this.abilitySortValue(right);
      return difference === 0
        ? left.name.localeCompare(right.name)
        : difference * direction;
    });
  }

  isDefeated(entity: SimpleCombatEntityDto): boolean {
    return entity.health <= 0;
  }

  private refreshSelection(): void {
    const participants = [
      ...this.playerParticipants,
      ...this.enemyParticipants,
    ];
    if (
      this.selectedEntityId &&
      participants.some(
        (participant) => participant.id === this.selectedEntityId,
      )
    ) {
      this.selectEntity(this.selectedEntityId);
      return;
    }

    const firstParticipant = participants[0];
    if (firstParticipant) {
      this.selectEntity(firstParticipant.id);
      return;
    }

    this.selectedEntityId = '';
    this.selectedEntityName = '';
    this.selectedStats = null;
  }

  private buildParticipants(
    team: CombatTeamName,
    visibleTeam: SimpleCombatEntityDto[],
  ): StatsParticipant[] {
    const participants = new Map<string, StatsParticipant>();

    for (const entity of visibleTeam) {
      if (!entity.id) continue;
      participants.set(entity.id, {
        id: entity.id,
        name: entity.name,
        team,
        entity,
      });
    }

    for (const stats of this.entityStats ?? []) {
      if (!stats.entityId || !this.isStatsForTeam(stats, team, visibleTeam))
        continue;

      if (!participants.has(stats.entityId)) {
        const entity: SimpleCombatEntityDto = {
          id: stats.entityId,
          name: stats.entityName || stats.entityId,
          imagePath: '',
          health: 0,
          maxHealth: 0,
          barrier: 0,
          level: 1,
        };
        participants.set(stats.entityId, {
          id: stats.entityId,
          name: entity.name,
          team,
          entity,
        });
      }
    }

    return [...participants.values()].sort((a, b) =>
      a.name.localeCompare(b.name),
    );
  }

  private isStatsForTeam(
    stats: EntityStats,
    team: CombatTeamName,
    visibleTeam: SimpleCombatEntityDto[],
  ): boolean {
    if (stats.team?.toLowerCase() === team.toLowerCase()) return true;
    return (
      !stats.team && visibleTeam.some((entity) => entity.id === stats.entityId)
    );
  }

  private createEmptyStats(id: string): EntityStats | null {
    const participant = [
      ...this.playerParticipants,
      ...this.enemyParticipants,
    ].find((item) => item.id === id);
    if (!participant) return null;

    return {
      entityId: participant.id,
      entityName: participant.name,
      abilities: [],
      damageDone: 0,
      damageTaken: 0,
      healingDone: 0,
      healingReceived: 0,
      healthRegenerated: 0,
      selfDamageDone: 0,
      selfDamageTaken: 0,
      alliedDamageDone: 0,
      alliedDamageTaken: 0,
      team: '',
      barrierGenerated: 0,
      damageBlocked: 0,
    };
  }

  private abilitySortValue(ability: AbilityStats): number {
    switch (this.abilitySortColumn) {
      case 'uses':
        return ability.uses ?? 0;
      case 'healing':
        return ability.totalHealing ?? 0;
      case 'barrier':
        return ability.totalBarrier ?? 0;
      default:
        return ability.totalDamage ?? 0;
    }
  }

  private primaryOutputTotal(ability: AbilityStats): number {
    if (ability.totalDamage > 0) return ability.totalDamage;
    if (ability.totalHealing > 0) return ability.totalHealing;
    return ability.totalBarrier ?? 0;
  }
}
