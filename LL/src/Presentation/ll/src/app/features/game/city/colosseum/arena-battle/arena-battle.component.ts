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

@Component({
  selector: 'app-arena-battle',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgClass,
    RegularButtonComponent,
    CharacterTagComponent,
  ],
  templateUrl: './arena-battle.component.html',
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
        60000,
      );
    }
  }

  updateNextTicketCountdown() {
    if (!this.arenaTicketStatus) {
      this.newTicketsIn = '';
      return;
    }

    const restoreIntervalMs = 3 * 60 * 60 * 1000;
    const lastTicket = new Date(
      this.arenaTicketStatus.lastTicketUpdate,
    ).getTime();
    const now = Date.now();
    const elapsed = now - lastTicket;
    const remainder = restoreIntervalMs - (elapsed % restoreIntervalMs);

    this.newTicketsIn = this.formatTime(remainder);
  }

  formatTime(ms: number): string {
    const totalMinutes = Math.ceil(ms / 60000);
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return `${hours}h ${minutes}m - new ticket`;
  }

  onRefresh() {
    this.refreshOpponents.emit();
  }

  onChallenge(id: string) {
    if (!this.arenaTicketStatus || this.arenaTicketStatus.currentTickets < 1)
      return;

    this.challenge.emit(id);
  }

  ngOnDestroy() {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }
}
