import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { interval, Subscription } from 'rxjs';

@Component({
  selector: 'app-countdown',
  standalone: true,
  imports: [],
  templateUrl: './countdown.component.html',
  styleUrl: './countdown.component.css',
})
export class CountdownComponent implements OnInit, OnDestroy {
  @Input() targetDate!: Date;
  secondsLeft: number = 0;
  private subscription: Subscription = new Subscription();

  ngOnInit() {
    this.calculateSecondsLeft();
    this.subscription = interval(1000).subscribe(() =>
      this.calculateSecondsLeft(),
    );
  }

  ngOnDestroy() {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }

  private calculateSecondsLeft() {
    const now = new Date().getTime();
    const target = new Date(this.targetDate).getTime();
    this.secondsLeft = Math.max(0, Math.floor((target - now) / 1000));
  }
}
