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

  @Input() health: number = 100; // Current health
  @Input() maxHealth: number = 100; // Max health

  ngAfterViewInit(): void {
    this.updateHealthBar();
  }

  ngOnChanges(): void {
    this.updateHealthBar();
  }

  updateHealthBar() {
    if (this.healthBar) {
      const healthPercentage = (this.health / this.maxHealth) * 100;
      this.healthBar.nativeElement.style.width = `${healthPercentage}%`;
    }
  }
}
