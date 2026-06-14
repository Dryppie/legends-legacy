import { Component, computed, inject } from '@angular/core';
import { DungeonStateService } from '../../../../../../core/services/api/dungeon/dungeon-state.service';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { RegularButtonComponent } from '../../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { CombatStateService } from '../../../../../../core/state/combat-state/combat-state.service';
import { CombatComponent } from '../../../../../../shared/components/combat/combat.component';
import { BattleType } from '../../../../../../core/state/combat-state/combatState';
import { Router } from '@angular/router';
import { DungeonRoomIconComponent } from '../../../../../../shared/components/dungeons/dungeon-room-icon/dungeon-room-icon.component';

@Component({
  selector: 'app-dungeon-page',
  standalone: true,
  imports: [
    NgIf,
    NgFor,
    NgClass,
    RegularButtonComponent,
    CombatComponent,
    DungeonRoomIconComponent,
  ],
  templateUrl: './dungeon-page.component.html',
})
export class DungeonPageComponent {
  readonly dungeonState = inject(DungeonStateService);
  readonly combatStateService = inject(CombatStateService);
  private readonly router = inject(Router);

  battleType = BattleType.Dungeon;

  readonly activeDungeon = this.dungeonState.activeDungeon;
  readonly loading = this.dungeonState.loading;
  readonly error = this.dungeonState.error;
  readonly message = this.dungeonState.message;
  readonly hasActiveDungeon = this.dungeonState.hasActiveDungeon;

  readonly totalRooms = computed(
    () => this.activeDungeon()?.rooms?.length ?? 0,
  );

  readonly currentRoomZeroBasedIndex = computed(() => {
    const run = this.activeDungeon();
    const total = run?.rooms?.length ?? 0;

    if (!run || total <= 0) return 0;
    return Math.min(Math.max(0, run.currentRoomIndex ?? 0), total - 1);
  });

  readonly currentRoomNumber = computed(() => {
    const total = this.totalRooms();
    if (total <= 0) return 0;

    return this.currentRoomZeroBasedIndex() + 1;
  });

  readonly currentRoom = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return null;
    return run.rooms[this.currentRoomZeroBasedIndex()] ?? null;
  });

  readonly nextRoom = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return null;
    return run.rooms[this.currentRoomZeroBasedIndex() + 1] ?? null;
  });

  readonly progressPercent = computed(() => {
    const total = this.totalRooms();
    const current = this.currentRoomNumber();

    if (total <= 0) return 0;
    if (this.activeDungeon()?.status === 'Completed') return 100;
    if (this.activeDungeon()?.status === 'Withdrawn') {
      return Math.min(100, Math.round((current / total) * 100));
    }

    return Math.min(100, Math.round((current / total) * 100));
  });

  readonly dungeonTitle = computed(() => {
    const run = this.activeDungeon();
    return run?.dungeonDefinitionName ?? 'Dungeon';
  });

  readonly dungeonStatus = computed(() => {
    return this.activeDungeon()?.status ?? 'Unknown';
  });

  readonly isCombatRoom = computed(() => {
    const type = this.currentRoom()?.type;
    return type === 'Combat' || type === 'MiniBoss' || type === 'Boss';
  });

  readonly isEventRoom = computed(() => {
    return this.currentRoom()?.type === 'Event';
  });

  readonly isCheckpointRoom = computed(() => {
    return this.currentRoom()?.type === 'Checkpoint';
  });

  readonly primaryActionLabel = computed(() => {
    const run = this.activeDungeon();
    const room = this.currentRoom();

    if (!run || !room || this.loading()) return null;

    if (run.status === 'Completed' || run.status === 'Withdrawn')
      return 'Claim Rewards';
    if (run.status === 'Failed') return null;

    switch (room.type) {
      case 'Combat':
      case 'MiniBoss':
      case 'Boss':
        return 'Fight';

      case 'Checkpoint':
        return 'Continue';

      case 'Event':
        return room.status === 'Active' ? 'Accept' : 'Inspect';

      default:
        return null;
    }
  });

  readonly canLeave = computed(() => {
    const run = this.activeDungeon();
    if (!run) return false;
    if (this.loading()) return false;

    return run.status !== 'Completed' && run.status !== 'Withdrawn';
  });

  readonly canClaimRewards = computed(() => {
    const run = this.activeDungeon();
    if (!run) return false;
    if (this.loading()) return false;

    return run.status === 'Completed' || run.status === 'Withdrawn';
  });

  readonly isFailedRun = computed(() => {
    return this.activeDungeon()?.status === 'Failed';
  });

  readonly isRewardClaimRun = computed(() => {
    const status = this.activeDungeon()?.status;
    return status === 'Completed' || status === 'Withdrawn';
  });

  readonly rewardSummaryTitle = computed(() => {
    return this.activeDungeon()?.status === 'Withdrawn'
      ? 'Rewards Secured'
      : 'Dungeon Complete';
  });

  readonly pendingRewards = computed(() => {
    return this.activeDungeon()?.pendingRewards ?? [];
  });

  readonly completedRooms = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return [];
    if (run.status === 'Completed') return run.rooms;
    if (run.status === 'Withdrawn') {
      return run.rooms.slice(0, run.currentRoomIndex + 1);
    }

    const currentIndex = run.currentRoomIndex ?? 0;
    return run.rooms.slice(0, currentIndex);
  });

  readonly upcomingRooms = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return [];
    if (run.status === 'Completed') return [];
    if (run.status === 'Withdrawn') return [];

    const currentIndex = this.currentRoomZeroBasedIndex();
    return run.rooms.slice(currentIndex + 1);
  });

  readonly clearedRooms = computed(() => {
    const run = this.activeDungeon();
    if (!run?.rooms?.length) return 0;
    if (run.status === 'Completed') return run.rooms.length;
    if (run.status === 'Withdrawn') {
      return Math.min(run.rooms.length, run.currentRoomIndex + 1);
    }

    return Math.max(0, this.currentRoomZeroBasedIndex());
  });

  readonly failedRoom = computed(() => {
    const run = this.activeDungeon();
    if (run?.status !== 'Failed' || !run.rooms?.length) return null;

    return run.rooms[this.currentRoomZeroBasedIndex()] ?? null;
  });

  readonly defeatedEncounters = computed(() => {
    return this.completedRooms().flatMap((room) => room.encounterIds ?? []);
  });

  readonly remainingRooms = computed(() => {
    if (this.isRewardClaimRun()) return 0;

    const total = this.totalRooms();
    const current = this.currentRoomNumber();
    return Math.max(0, total - current);
  });

  readonly canExecutePrimaryAction = computed(() => {
    const run = this.activeDungeon();
    const room = this.currentRoom();

    if (!run) return false;
    if (this.loading()) return false;
    if (run.status === 'Failed') return false;

    if (run.status === 'Completed' || run.status === 'Withdrawn') {
      return this.canClaimRewards();
    }

    if (!room) return false;

    return true;
  });

  executePrimaryAction(): void {
    const run = this.activeDungeon();
    const room = this.currentRoom();

    if (!run || this.loading()) return;

    if (run.status === 'Completed' || run.status === 'Withdrawn') {
      this.claimDungeonRewards();
      return;
    }

    if (!room) return;

    switch (room.type) {
      case 'Combat':
      case 'MiniBoss':
      case 'Boss':
        this.startCombat();
        return;

      case 'Checkpoint':
        this.continueAtCheckpoint();
        return;

      case 'Event':
        this.handleEventRoom();
        return;

      default:
        return;
    }
  }

  startCombat(): void {
    const room = this.currentRoom();
    if (!room || !this.isCombatRoom()) return;

    this.dungeonState.fight();
  }

  continueAtCheckpoint(): void {
    const room = this.currentRoom();
    if (!room || room.type !== 'Checkpoint') return;

    this.dungeonState.continueAtCheckpoint();
  }

  withdrawAtCheckpoint(): void {
    const room = this.currentRoom();
    if (!room || room.type !== 'Checkpoint') return;

    this.dungeonState.withdraw();
  }

  handleEventRoom(): void {
    const room = this.currentRoom();
    if (!room || room.type !== 'Event') return;

    this.dungeonState.chooseEventAction(
      room.status === 'Active' ? 'event.accept' : 'event.inspect',
    );
  }

  chooseEventAction(actionId: string, payload?: unknown): void {
    const room = this.currentRoom();
    if (!room || room.type !== 'Event') return;

    this.dungeonState.chooseEventAction(actionId, payload);
  }

  leaveDungeon(): void {
    if (!this.canLeave()) return;
    this.dungeonState.leaveDungeon();
  }

  dismissFailedDungeonRun(): void {
    const run = this.activeDungeon();
    if (!run || run.status !== 'Failed' || this.loading()) return;

    this.dungeonState.dismissFailedDungeonRun(() => {
      void this.router.navigate(['/game/world/shenic']);
    });
  }

  claimDungeonRewards(): void {
    if (!this.canClaimRewards()) return;
    this.dungeonState.claimDungeonRewards(() => {
      void this.router.navigate(['/game/world/shenic']);
    });
  }

  formatEncounterName(value: string | null | undefined): string {
    if (!value) return 'Unknown';
    return value
      .replace(/_/g, ' ')
      .replace(/\b\w/g, (char) => char.toUpperCase());
  }

  formatRewardSource(value: string | null | undefined): string {
    if (!value) return 'Unknown source';

    return value
      .replace(/:/g, ' ')
      .replace(/-/g, ' ')
      .replace(/\b\w/g, (char) => char.toUpperCase());
  }

  refresh(): void {
    this.dungeonState.refresh();
  }

  trackByIndex(index: number): number {
    return index;
  }

  getRoomTypeLabel(type: string | null | undefined): string {
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
        return type || 'Unknown';
    }
  }

  getRoomStateClasses(index: number): string {
    const current = this.currentRoomZeroBasedIndex();

    if (index < current) {
      return 'border-primary/20 bg-primary/5 opacity-60';
    }

    if (index === current) {
      return 'border-primary bg-primary/10 shadow-[0_0_0_1px_rgba(212,175,55,0.15)]';
    }

    return 'border-white/10 bg-black/20';
  }

  getCurrentStepText(): string {
    const room = this.currentRoom();
    if (!room) return 'Preparing the next room';

    switch (room.type) {
      case 'Combat':
        return 'Enemies block your path';
      case 'MiniBoss':
        return 'A stronger foe awaits';
      case 'Boss':
        return 'The final battle stands before you';
      case 'Event':
        return this.getEventDescription(room.eventOutcome);
      case 'Checkpoint':
        return 'A brief moment of safety';
      default:
        return 'Exploring the dungeon';
    }
  }

  getEventTitle(outcome: string | null | undefined): string {
    switch (outcome) {
      case 'ExtraCombat':
        return 'Enemy Patrol';
      case 'TreasureRoom':
        return 'Hidden Cache';
      case 'Shrine':
        return 'Ancient Shrine';
      case 'Trap':
        return 'Suspicious Mechanism';
      default:
        return 'Unknown Event';
    }
  }

  getEventDescription(outcome: string | null | undefined): string {
    switch (outcome) {
      case 'ExtraCombat':
        return 'Noise in the dark suggests enemies are nearby. Accepting draws them into a fight.';
      case 'TreasureRoom':
        return 'A concealed cache promises extra Cinders, Soulstones, and Monster Core fragments.';
      case 'Shrine':
        return 'A quiet shrine offers a small pulse of experience and Soulstones.';
      case 'Trap':
        return 'The room is dangerous. Accepting risks some of your pending Cinders.';
      default:
        return 'A strange event unfolds.';
    }
  }

  skipBattle() {
    this.dungeonState.skipDungeonMatch();
  }
}
