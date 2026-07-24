import { Component, Input, OnInit, OnDestroy, OnChanges } from '@angular/core';
import { interval, Subscription } from 'rxjs';

@Component({
    selector: 'app-countdown',
    imports: [],
    templateUrl: './countdown.component.html'
})
export class CountdownComponent implements OnInit, OnDestroy, OnChanges {
  @Input() targetDate!: Date;
  secondsLeft = 0;
  private subscription: Subscription | null = null;

  ngOnInit() {
    this.tryStartCountdown();
  }

  ngOnChanges() {
    this.tryStartCountdown();
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
  }

  private tryStartCountdown() {
    if (!this.targetDate) return;

    this.calculateSecondsLeft();
    this.subscription?.unsubscribe();
    this.subscription = interval(1000).subscribe(() =>
      this.calculateSecondsLeft(),
    );
  }

  private calculateSecondsLeft() {
    const now = Date.now();
    const target = new Date(this.targetDate).getTime();
    this.secondsLeft = Math.max(0, Math.floor((target - now) / 1000));
  }
}
