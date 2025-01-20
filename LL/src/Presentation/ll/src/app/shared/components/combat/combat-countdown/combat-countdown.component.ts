import { NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnDestroy,
  OnInit,
  Output,
} from '@angular/core';

@Component({
  selector: 'app-combat-countdown',
  standalone: true,
  imports: [NgIf],
  templateUrl: './combat-countdown.component.html',
  styleUrl: './combat-countdown.component.css',
})
export class CombatCountdownComponent implements OnInit, OnDestroy {
  @Input() startValue: number = 3; // default to 3 seconds
  @Output() countdownComplete = new EventEmitter<void>();

  countdownValue: number = 0;
  private countdownTimer: any;

  ngOnInit(): void {
    this.countdownValue = this.startValue;
    this.startCountdown();
  }

  ngOnDestroy(): void {
    this.clearTimer();
  }

  private startCountdown(): void {
    this.countdownTimer = setInterval(() => {
      if (this.countdownValue > 0) {
        this.countdownValue--;
      } else {
        // Countdown is done; clear timer and emit event
        this.clearTimer();
        this.countdownComplete.emit();
      }
    }, 1000);
  }

  private clearTimer(): void {
    if (this.countdownTimer) {
      clearInterval(this.countdownTimer);
      this.countdownTimer = null;
    }
  }
}
