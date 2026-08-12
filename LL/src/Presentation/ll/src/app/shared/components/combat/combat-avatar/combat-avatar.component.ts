import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { HealthBarComponent } from '../health-bar/health-bar.component';

@Component({
  selector: 'app-combat-avatar',
  standalone: true,
  imports: [HealthBarComponent, CommonModule],
  templateUrl: './combat-avatar.component.html',
})
export class CombatAvatarComponent {
  @Input() name!: string;
  @Input() hp!: number;
  @Input() maxHp!: number;
  @Input() barrier!: number;
  @Input() isLoading = true;
}
