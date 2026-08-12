import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  TowerHallOfFameEntry,
  WorldTowerService,
} from '../../../../../core/services/api/world-tower/world-tower.service';

@Component({
  selector: 'app-tower-hall-of-fame',
  imports: [CommonModule, RouterLink],
  templateUrl: './tower-hall-of-fame.component.html',
  styleUrl: '../tower-page.scss',
})
export class TowerHallOfFameComponent implements OnInit {
  private readonly tower = inject(WorldTowerService);
  readonly records = signal<TowerHallOfFameEntry[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.tower
      .getHallOfFame()
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (records) => this.records.set(records),
        error: (error) =>
          this.error.set(
            (error as { errorMessage?: string })?.errorMessage ??
              'Server history could not be read.',
          ),
      });
  }

  duration(seconds: number): string {
    const total = Math.max(0, seconds);
    return `${Math.floor(total / 60)}:${(total % 60).toString().padStart(2, '0')}`;
  }

  rosterSummary(record: TowerHallOfFameEntry): string {
    const visible = record.participants
      .slice(0, 4)
      .map((entry) => entry.characterName);
    const remaining = record.participants.length - visible.length;
    return `${visible.join(', ')}${remaining > 0 ? `, +${remaining}` : ''}`;
  }

  floorLabel(floorNumber: number): string {
    return floorNumber.toString().padStart(2, '0');
  }
}
