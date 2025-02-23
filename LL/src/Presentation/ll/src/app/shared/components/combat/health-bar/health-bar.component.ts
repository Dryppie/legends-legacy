import { Component, ElementRef, Input, ViewChild } from '@angular/core';

@Component({
  selector: 'app-health-bar',
  standalone: true,
  imports: [],
  templateUrl: './health-bar.component.html',
  styleUrl: './health-bar.component.css',
})
export class HealthBarComponent {
  @ViewChild('healthBar', { static: true })
  healthBar!: ElementRef<HTMLDivElement>;
  @ViewChild('barrierBar', { static: true })
  barrierBar!: ElementRef<HTMLDivElement>;

  @Input() health: number = 100; // Current health
  @Input() maxHealth: number = 100; // Max health
  @Input() barrier: number = 0;

  ngAfterViewInit(): void {
    this.updateHealthBar();
    this.updateBarrierBar();
  }

  ngOnChanges(): void {
    this.updateHealthBar();
    this.updateBarrierBar();
  }

  updateHealthBar() {
    if (this.healthBar) {
      const healthPercentage = (this.health / this.maxHealth) * 100;
      this.healthBar.nativeElement.style.width = `${healthPercentage}%`;
    }
  }

  updateBarrierBar() {
    if (this.barrierBar) {
      const barrierPercentage = (this.barrier / this.maxHealth) * 100;
      this.barrierBar.nativeElement.style.width = `${barrierPercentage}%`;
    }
  }
}
