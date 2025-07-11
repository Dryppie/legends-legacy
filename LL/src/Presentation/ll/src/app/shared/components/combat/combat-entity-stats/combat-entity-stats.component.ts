import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import {
  EntityStats,
  SimpleCombatEntityDto,
} from '../../../models/Dtos/combatResultDto';
import { DecimalPipe, NgClass, NgFor, NgIf } from '@angular/common';

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

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['entityStats'] && this.playerTeam.length > 0) {
      const firstPlayerId = this.playerTeam[0].id;
      this.selectEntity(firstPlayerId);
    }
  }

  selectEntity(id: string) {
    this.selectedStats =
      this.entityStats.find((stats) => stats.entityId === id) ?? null;
    const entity = [...this.playerTeam, ...this.enemyTeam].find(
      (e) => e.id === id,
    );
    this.selectedEntityId = entity?.id ?? '';
    this.selectedEntityName = entity?.name ?? '';
  }
}
