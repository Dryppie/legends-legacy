import { CommonModule } from '@angular/common';
import { Component, input, output, signal } from '@angular/core';
import {
  RaidJoinRequest,
  RaidLane,
  RaidRun,
  RaidSignup,
} from '../../../../../core/services/api/raid/raid.service';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { LocalDatePipe } from '../../../../../shared/pipes/local-date/local-date.pipe';

export interface RaidPartyAssignment {
  characterId: string;
  lane: RaidLane | null;
  wingSlotIndex: number | null;
}

export interface RaidSignupRemoval {
  signup: Pick<RaidSignup, 'characterId' | 'characterName'> | RaidJoinRequest;
  pending: boolean;
}

@Component({
  selector: 'app-raid-party-builder',
  imports: [CommonModule, CharacterTagComponent, LocalDatePipe],
  templateUrl: './raid-party-builder.component.html',
  styleUrls: [
    '../../tower/tower-party-builder.scss',
    './raid-party-builder.component.scss',
  ],
})
export class RaidPartyBuilderComponent {
  readonly raid = input.required<RaidRun>();
  readonly action = input<string | null>(null);

  readonly approve = output<RaidJoinRequest>();
  readonly remove = output<RaidSignupRemoval>();
  readonly transfer = output<RaidSignup>();
  readonly assignmentsChange = output<readonly RaidPartyAssignment[]>();
  readonly assignmentError = output<string>();

  readonly selectedSignupId = signal<string | null>(null);
  readonly collapsedLanes = signal<Set<RaidLane>>(new Set());
  readonly lanes: RaidLane[] = ['Rearguard', 'Vanguard', 'MainGuard'];

  wing(lane: RaidLane): RaidSignup[] {
    return this.raid()
      .signups.filter((signup) => signup.lane === lane)
      .sort(
        (left, right) =>
          (left.wingSlotIndex ?? Number.MAX_SAFE_INTEGER) -
          (right.wingSlotIndex ?? Number.MAX_SAFE_INTEGER),
      );
  }

  benched(): RaidSignup[] {
    return this.raid().signups.filter((signup) => !signup.lane);
  }

  wingSlots(raid: RaidRun): number[] {
    return Array.from({ length: raid.laneSlots }, (_, index) => index);
  }

  slotLabel(slotIndex: number): string {
    return (slotIndex + 1).toString().padStart(2, '0');
  }

  signupAtSlot(
    raid: RaidRun,
    lane: RaidLane,
    slotIndex: number,
  ): RaidSignup | null {
    return (
      raid.signups.find(
        (signup) => signup.lane === lane && signup.wingSlotIndex === slotIndex,
      ) ?? null
    );
  }

  wingAverage(raid: RaidRun, lane: RaidLane): number {
    const signups = raid.signups.filter((signup) => signup.lane === lane);
    if (!signups.length) return 0;
    return Math.round(
      signups.reduce((total, signup) => total + signup.powerRating, 0) /
        signups.length,
    );
  }

  isLaneCollapsed(lane: RaidLane): boolean {
    return this.collapsedLanes().has(lane);
  }

  toggleLane(lane: RaidLane): void {
    this.collapsedLanes.update((current) => {
      const next = new Set(current);
      if (next.has(lane)) next.delete(lane);
      else next.add(lane);
      return next;
    });
  }

  raidEncounterName(lane: RaidLane): string {
    return lane === 'MainGuard' ? 'Main Guard' : lane;
  }

  selectSignup(signup: RaidSignup, raid: RaidRun): void {
    if (!raid.canAssign || this.action()) return;
    this.selectedSignupId.update((selected) =>
      selected === signup.characterId ? null : signup.characterId,
    );
  }

  placeSelectedInWing(lane: RaidLane, raid: RaidRun): void {
    const signupId = this.selectedSignupId();
    if (!signupId) return;
    const openSlot = this.wingSlots(raid).find(
      (slotIndex) => !this.signupAtSlot(raid, lane, slotIndex),
    );
    if (openSlot !== undefined) this.moveSignup(signupId, lane, openSlot, raid);
  }

  placeSelectedInSlot(lane: RaidLane, slotIndex: number, raid: RaidRun): void {
    const signupId = this.selectedSignupId();
    if (signupId) this.moveSignup(signupId, lane, slotIndex, raid);
  }

  benchSignup(signup: RaidSignup, raid: RaidRun): void {
    this.moveSignup(signup.characterId, null, null, raid);
  }

  dragSignup(event: DragEvent, signup: RaidSignup, raid: RaidRun): void {
    if (!raid.canAssign || !event.dataTransfer) return;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', signup.characterId);
    this.selectedSignupId.set(signup.characterId);
  }

  allowPartyDrop(event: DragEvent): void {
    event.preventDefault();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
  }

  dropIntoSlot(
    event: DragEvent,
    lane: RaidLane,
    slotIndex: number,
    raid: RaidRun,
  ): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (signupId) this.moveSignup(signupId, lane, slotIndex, raid);
  }

  dropIntoWing(event: DragEvent, lane: RaidLane, raid: RaidRun): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (!signupId) return;
    const openSlot = this.wingSlots(raid).find(
      (slotIndex) => !this.signupAtSlot(raid, lane, slotIndex),
    );
    if (openSlot !== undefined) this.moveSignup(signupId, lane, openSlot, raid);
  }

  dropOnBench(event: DragEvent, raid: RaidRun): void {
    event.preventDefault();
    const signupId = event.dataTransfer?.getData('text/plain');
    if (signupId) this.moveSignup(signupId, null, null, raid);
  }

  distributeBenched(raid: RaidRun): void {
    const openPositions = this.lanes.flatMap((lane) =>
      this.wingSlots(raid)
        .filter((slotIndex) => !this.signupAtSlot(raid, lane, slotIndex))
        .map((slotIndex) => ({ lane, slotIndex })),
    );
    let positionIndex = 0;
    this.emitAssignments(
      raid.signups.map((signup) => {
        const position = signup.lane ? null : openPositions[positionIndex++];
        return {
          characterId: signup.characterId,
          lane: signup.lane ?? position?.lane ?? null,
          wingSlotIndex: signup.wingSlotIndex ?? position?.slotIndex ?? null,
        };
      }),
    );
  }

  private moveSignup(
    signupId: string,
    lane: RaidLane | null,
    wingSlotIndex: number | null,
    raid: RaidRun,
  ): void {
    if (!raid.canAssign || this.action()) return;
    const occupant = raid.signups.find(
      (signup) =>
        lane !== null &&
        signup.lane === lane &&
        signup.wingSlotIndex === wingSlotIndex &&
        signup.characterId !== signupId,
    );
    if (occupant) {
      this.assignmentError.emit('That raid party slot is already occupied.');
      return;
    }

    this.emitAssignments(
      raid.signups.map((signup) => ({
        characterId: signup.characterId,
        lane: signup.characterId === signupId ? lane : signup.lane,
        wingSlotIndex:
          signup.characterId === signupId
            ? wingSlotIndex
            : signup.wingSlotIndex,
      })),
    );
  }

  private emitAssignments(assignments: readonly RaidPartyAssignment[]): void {
    this.assignmentsChange.emit(assignments);
    this.selectedSignupId.set(null);
  }
}
