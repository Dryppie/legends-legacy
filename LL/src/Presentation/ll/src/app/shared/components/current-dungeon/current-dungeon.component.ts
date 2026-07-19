import { Component, computed, inject } from '@angular/core';
import { DungeonStateService } from '../../../core/services/api/dungeon/dungeon-state.service';
import { NgClass, NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-current-dungeon',
  standalone: true,
  imports: [NgIf, NgClass, RouterLink],
  templateUrl: './current-dungeon.component.html',
})
export class CurrentDungeonComponent {
  private readonly dungeonState = inject(DungeonStateService);

  readonly activeDungeon = this.dungeonState.activeDungeon;
  readonly hasActiveDungeon = this.dungeonState.hasActiveDungeon;
  readonly loading = this.dungeonState.loading;

  readonly totalRooms = computed(() => {
    return this.activeDungeon()?.rooms?.length ?? 0;
  });

  readonly currentRoomIndex = computed(() => {
    const run = this.activeDungeon();
    const total = run?.rooms?.length ?? 0;

    if (!run || total <= 0) return 0;
    return Math.min(Math.max(0, run.currentRoomIndex ?? 0), total - 1);
  });

  readonly currentRoomNumber = computed(() => {
    const total = this.totalRooms();
    if (total <= 0) return 0;

    return this.currentRoomIndex() + 1;
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
    return `Room ${this.currentRoomNumber()} / ${this.totalRooms()}`;
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
