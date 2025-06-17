import { CommonModule, NgStyle } from '@angular/common';
import { Component, Input } from '@angular/core';
import { HealthBarComponent } from '../health-bar/health-bar.component';
import { ManaBarComponent } from '../mana-bar/mana-bar.component';

@Component({
  selector: 'app-combat-avatar',
  standalone: true,
  imports: [NgStyle, HealthBarComponent, ManaBarComponent, CommonModule],
  templateUrl: './combat-avatar.component.html',
})
export class CombatAvatarComponent {
  @Input() imagePath!: string;
  @Input() name!: string;
  @Input() hp!: number;
  @Input() maxHp!: number;
  @Input() mp!: number;
  @Input() maxMp!: number;
  @Input() barrier!: number;
  @Input() isLoading = true;

  imageLoaded = false;

  onImageLoad() {
    console.log('[✔] Image loaded');
    this.imageLoaded = true;
  }
}
