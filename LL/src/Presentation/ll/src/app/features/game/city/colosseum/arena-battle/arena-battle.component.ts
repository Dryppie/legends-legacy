import {
  Component,
  EventEmitter,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
} from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';
import { ArenaTicketStatus } from '../../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { RegularButtonComponent } from '../../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { ArenaOpponentPreview } from '../../../../../shared/models/Dtos/colosseum/arenaOpponentPreview';
import { CharacterTagComponent } from '../../../../../shared/components/character/character-tag/character-tag.component';
import { NumberFormatPipe } from '../../../../../shared/pipes/number-format/number-format.pipe';

@Component({
    selector: 'app-arena-battle',
    imports: [
        NgFor,
        NgIf,
        NgClass,
        RegularButtonComponent,
        CharacterTagComponent,
        NumberFormatPipe,
    ],
    templateUrl: './arena-battle.component.html'
})
export class ArenaBattleComponent implements OnChanges, OnDestroy {
  @Input() opponents: ArenaOpponentPreview[] = [];
  @Input() arenaTicketStatus: ArenaTicketStatus | null = null;

  @Output() refreshOpponents = new EventEmitter<void>();
  @Output() challenge = new EventEmitter<string>();

  newTicketsIn: string = '';
  private countdownInterval?: ReturnType<typeof setInterval>;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['arenaTicketStatus']) {
      this.startOrResetCountdown();
    }
  }

  startOrResetCountdown() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }

    this.updateNextTicketCountdown();

    if (this.arenaTicketStatus) {
      this.countdownInterval = setInterval(
        () => this.updateNextTicketCountdown(),
        1000,
      );
    }
  }

  updateNextTicketCountdown() {
    if (!this.arenaTicketStatus) {
      this.newTicketsIn = '';
      return;
    }

    const now = Date.now();
    const nextTicketAt = this.arenaTicketStatus.nextTicketAt
      ? new Date(this.arenaTicketStatus.nextTicketAt).getTime()
      : null;
    const restoreIntervalMs = 3 * 60 * 60 * 1000;
    const lastTicket = new Date(
      this.arenaTicketStatus.lastTicketUpdate,
    ).getTime();
    const elapsed = now - lastTicket;
    const fallbackRemainder = restoreIntervalMs - (elapsed % restoreIntervalMs);
    const remainder = nextTicketAt
      ? Math.max(0, nextTicketAt - now)
      : fallbackRemainder;

    this.newTicketsIn = this.formatTime(remainder);
  }

  formatTime(ms: number): string {
    const totalMinutes = Math.ceil(ms / 60000);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return `${hours}h ${minutes}m`;
  }

  onRefresh() {
    this.refreshOpponents.emit();
  }

  onChallenge(id: string) {
    const opponent = this.opponents.find((item) => item.opponentId === id);
    if (!opponent || !this.canChallenge(opponent)) return;

    this.challenge.emit(id);
  }

  canChallenge(opponent?: ArenaOpponentPreview): boolean {
    return (
      !!this.arenaTicketStatus &&
      this.arenaTicketStatus.currentTickets > 0 &&
      !this.isChallengeOnCooldown(opponent)
    );
  }

  isChallengeOnCooldown(opponent?: ArenaOpponentPreview): boolean {
    const availableAt = this.challengeAvailableAt(opponent);
    return availableAt !== null && availableAt.getTime() > Date.now();
  }

  challengeCooldownLabel(opponent: ArenaOpponentPreview): string {
    const availableAt = this.challengeAvailableAt(opponent);
    if (!availableAt) return '';

    const remainingMs = Math.max(0, availableAt.getTime() - Date.now());
    const totalSeconds = Math.ceil(remainingMs / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;

    if (minutes <= 0) return `${seconds}s`;
    return `${minutes}m ${seconds.toString().padStart(2, '0')}s`;
  }

  challengeButtonText(opponent: ArenaOpponentPreview): string {
    if (!this.isChallengeOnCooldown(opponent)) return 'Challenge';

    return `Challenge in ${this.challengeCooldownLabel(opponent)}`;
  }

  private challengeAvailableAt(
    opponent?: ArenaOpponentPreview,
  ): Date | null {
    if (!opponent?.challengeAvailableAt) return null;

    const availableAt = new Date(opponent.challengeAvailableAt);
    return Number.isNaN(availableAt.getTime()) ? null : availableAt;
  }

  deltaClass(delta: number, positiveClass = 'll-text-success'): string {
    if (delta > 0) return positiveClass;
    if (delta < 0) return 'll-text-danger';
    return 'll-text-muted';
  }

  formatDelta(delta: number): string {
    return delta > 0 ? `+${delta}` : `${delta}`;
  }

  ngOnDestroy() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }
}
