import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import {
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';

type CombatTeamName = 'Friendly' | 'Hostile';
type StatsParticipant = {
  id: string;
  name: string;
};

@Component({
  selector: 'app-combat-entity-stats',
  standalone: true,
  imports: [NgIf, NgFor, NgClass, DecimalPipe],
  templateUrl: './combat-entity-stats.component.html',
})
export class CombatEntityStatsComponent implements OnChanges {
  @Input() playerTeam: SimpleCombatEntityDto[] = [];
  @Input() enemyTeam: SimpleCombatEntityDto[] = [];
  @Input() entityStats!: EntityStats[];

  selectedStats: EntityStats | null = null;
  selectedEntityId: string = '';
  selectedEntityName: string = '';

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
      this.enemyParticipants = this.buildParticipants('Hostile', this.enemyTeam);
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

  private refreshSelection(): void {
    const participants = [...this.playerParticipants, ...this.enemyParticipants];
    if (
      this.selectedEntityId &&
      participants.some((participant) => participant.id === this.selectedEntityId)
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
      participants.set(entity.id, { id: entity.id, name: entity.name });
    }

    for (const stats of this.entityStats ?? []) {
      if (!stats.entityId || !this.isStatsForTeam(stats, team, visibleTeam))
        continue;

      if (!participants.has(stats.entityId)) {
        participants.set(stats.entityId, {
          id: stats.entityId,
          name: stats.entityName || stats.entityId,
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
      !stats.team &&
      visibleTeam.some((entity) => entity.id === stats.entityId)
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
    };
  }
}
