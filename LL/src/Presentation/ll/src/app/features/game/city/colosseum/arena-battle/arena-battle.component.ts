import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';
import { CharacterDto } from '../../../../../shared/models/Dtos/characterDto';
import { NgFor, NgIf } from '@angular/common';
import { ArenaTicketStatus } from '../../../../../shared/models/Dtos/colosseum/arenaTicketStatus';
import { ColosseumService } from '../../../../../core/services/api/colosseum/colosseum.service';
import { RegularButtonComponent } from '../../../../../shared/components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-arena-battle',
  standalone: true,
  imports: [NgFor, NgIf, RegularButtonComponent],
  templateUrl: './arena-battle.component.html',
})
export class ArenaBattleComponent implements OnInit {
  @Input() opponents!: CharacterDto[];
  arenaTicketStatus!: ArenaTicketStatus;

  @Output() refreshOpponents = new EventEmitter<void>();
  @Output() challenge = new EventEmitter<string>();

  restoreIntervalMs = 3 * 60 * 60 * 1000; // 3 hours in milliseconds
  newTicketsIn: string = '';
  private countdownInterval: any;

  constructor(private colosseumService: ColosseumService) {}

  ngOnInit(): void {
    this.colosseumService.arenaTicketStatus$.subscribe((status) => {
      if (!status) return;
      this.arenaTicketStatus = status;
      this.startOrResetCountdown();
    });
  }

  startOrResetCountdown() {
    // Clear previous interval if it exists
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }

    // Immediately update the countdown
    this.updateNextTicketCountdown();

    // Only start the interval if tickets aren't full
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

    const restoreIntervalMs = 3 * 60 * 60 * 1000; // 3 hours in ms
    const lastTicket = new Date(
      this.arenaTicketStatus.lastTicketUpdate,
    ).getTime();
    const now = Date.now();
    const elapsed = now - lastTicket;
    const remainder = restoreIntervalMs - (elapsed % restoreIntervalMs);

    this.newTicketsIn = this.formatTime(remainder);
  }

  formatTime(ms: number): string {
    const totalMinutes = Math.ceil(ms / 60000); // round up to next minute
    const hours = Math.floor(totalMinutes / 60);
    const minutes = totalMinutes % 60;
    return `${hours}h ${minutes}m - new ticket`;
  }

  onRefresh() {
    this.refreshOpponents.emit();
  }
  onChallenge(id: string) {
    if (this.arenaTicketStatus.currentTickets < 1) return;
    this.arenaTicketStatus.currentTickets--;
    this.challenge.emit(id);
  }

  ngOnDestroy() {
    // Always clear your interval to prevent memory leaks
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
    }
  }
}
