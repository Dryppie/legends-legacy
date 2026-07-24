import { Component, computed, inject } from '@angular/core';
import { DungeonStateService } from '../../../core/services/api/dungeon/dungeon-state.service';
import { NgClass, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-current-dungeon',
    imports: [NgIf, NgClass, RouterLink],
    templateUrl: './current-dungeon.component.html'
})
export class CurrentDungeonComponent {
  private readonly dungeonState = inject(DungeonStateService);

  readonly activeDungeon = this.dungeonState.activeDungeon;
  readonly hasActiveDungeon = this.dungeonState.hasActiveDungeon;
  readonly loading = this.dungeonState.loading;

  readonly totalDepths = computed(() => {
    const nodes = this.activeDungeon()?.state?.mapNodes ?? [];
    return nodes.length ? Math.max(...nodes.map((node) => node.depth)) + 1 : 0;
  });

  readonly currentDepthNumber = computed(() => {
    const run = this.activeDungeon();
    const total = this.totalDepths();
    if (!run || total <= 0) return 0;

    const depth =
      run.state.mapNodes.find((node) => node.roomIndex === run.currentRoomIndex)
        ?.depth ?? 0;
    return Math.min(total, Math.max(1, depth + 1));
  });

  readonly dungeonTitle = computed(() => {
    return this.activeDungeon()?.dungeonDefinitionName ?? 'Dungeon';
  });

  readonly statusLabel = computed(() => {
    const status = this.activeDungeon()?.status;

    switch (status) {
      case 'Active':
        return 'In Dungeon';
      case 'Completed':
        return 'Dungeon Complete';
      case 'Failed':
        return 'Dungeon Failed';
      default:
        return status ?? 'Dungeon';
    }
  });

  readonly progressText = computed(() => {
    if (!this.hasActiveDungeon()) return '';
    const total = this.totalDepths();
    return total > 0
      ? `Depth ${this.currentDepthNumber()} / ${total}`
      : 'Map unavailable';
  });

  readonly statusDotClasses = computed(() => {
    switch (this.activeDungeon()?.status) {
      case 'Failed':
        return 'bg-[var(--ll-color-danger)]';
      default:
        return 'bg-[var(--ll-color-success)]';
    }
  });
}
