import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TowerPersonalExpedition,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';

@Component({
  selector: 'app-tower-personal-expeditions',
  imports: [CommonModule, RouterLink],
  templateUrl: './tower-personal-expeditions.component.html',
  styleUrl: '../tower-page.scss',
})
export class TowerPersonalExpeditionsComponent implements OnInit {
  private readonly tower = inject(WorldTowerService);
  readonly expeditions = signal<TowerPersonalExpedition[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.tower
      .getPersonalExpeditions()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (expeditions) => this.expeditions.set(expeditions),
        error: (error) =>
          this.error.set(
            (error as { errorMessage?: string })?.errorMessage ??
              'Your Expedition history could not be read.',
          ),
      });
  }

  duration(seconds: number | null): string {
    if (seconds === null) return '—';
    const total = Math.max(0, seconds);
    return `${Math.floor(total / 60)}:${(total % 60).toString().padStart(2, '0')}`;
  }

  rosterSummary(expedition: TowerPersonalExpedition): string {
    const visible = expedition.participants
      .slice(0, 4)
      .map((entry) => entry.characterName);
    const remaining = expedition.participants.length - visible.length;
    return `${visible.join(', ')}${remaining > 0 ? `, +${remaining}` : ''}`;
  }

  floorLabel(floorNumber: number): string {
    return floorNumber.toString().padStart(2, '0');
  }

  modeLabel(expedition: TowerPersonalExpedition): string {
    return expedition.mode === 'FirstClear' ? 'First clear' : 'Echo';
  }
}
