import { Component, computed, inject } from '@angular/core';
import { DungeonStateService } from '../../../core/services/api/dungeon/dungeon-state.service';
import { NgIf } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DungeonRoomIconComponent } from '../dungeons/dungeon-room-icon/dungeon-room-icon.component';

@Component({
  selector: 'app-current-dungeon',
  standalone: true,
  imports: [NgIf, RouterLink, DungeonRoomIconComponent],
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

  readonly currentRoom = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return null;
    return run.rooms[this.currentRoomIndex()] ?? null;
  });

  readonly dungeonTitle = computed(() => {
    return this.activeDungeon()?.dungeonDefinitionName ?? 'Dungeon';
  });

  readonly combatStyleLabel = computed(() => {
    const style = this.activeDungeon()?.state?.combatStyle;
    if (!style) return null;

    return style.selectedFocusName
      ? `${style.styleName} - ${style.selectedFocusName}`
      : style.styleName;
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

  readonly roomTypeLabel = computed(() => {
    const type = this.currentRoom()?.type;

    switch (type) {
      case 'Combat':
        return 'Combat';
      case 'MiniBoss':
        return 'Mini Boss';
      case 'Boss':
        return 'Boss';
      case 'Event':
        return 'Event';
      case 'Checkpoint':
        return 'Checkpoint';
      default:
        return 'Exploring';
    }
  });

  readonly progressPercent = computed(() => {
    const total = this.totalRooms();
    const current = this.currentRoomNumber();

    if (total <= 0) return 0;
    if (this.activeDungeon()?.status === 'Completed') return 100;
    return Math.min(100, Math.round((current / total) * 100));
  });
}
