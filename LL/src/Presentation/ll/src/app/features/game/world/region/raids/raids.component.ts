import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, effect, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import {
  RaidBossSummary,
  RaidHistoryEntry,
  RaidRunSummary,
  RaidService,
  RaidTrophyVendor,
} from '../../../../../core/services/api/raid/raid.service';
import { GameEventService } from '../../../../../core/services/real-time/game-event.service';

@Component({
  selector: 'app-raids',
  imports: [CommonModule, RouterLink],
  templateUrl: './raids.component.html',
  styleUrl: './raids.component.scss',
})
export class RaidsComponent implements OnChanges {
  @Input({ required: true }) raidBoss!: RaidBossSummary;
  @Output() changed = new EventEmitter<void>();
  private readonly raids = inject(RaidService);
  private readonly router = inject(Router);
  private readonly events = inject(GameEventService);
  private lastRealtimeUpdateId: string | null = null;
  readonly openRaids = signal<RaidRunSummary[]>([]);
  readonly loading = signal(false);
  readonly action = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly selectedTier = signal<number>(0);
  readonly vendor = signal<RaidTrophyVendor | null>(null);
  readonly history = signal<RaidHistoryEntry[]>([]);
  readonly historyLoading = signal(false);

  constructor() {
    effect(() => {
      const envelope = this.events.eventEnvelope.RaidUpdated();
      if (
        !envelope?.updateId ||
        envelope.updateId === this.lastRealtimeUpdateId ||
        envelope.payload.raidBossId !== this.raidBoss?.id
      ) {
        return;
      }

      this.lastRealtimeUpdateId = envelope.updateId;
      this.load();
      this.changed.emit();
    });
  }

  ngOnChanges(): void {
    this.selectedTier.set(this.raidBoss?.tiers[0]?.tier ?? 0);
    this.load();
    this.loadHistory();
    this.loadVendor();
  }

  load(): void {
    if (!this.raidBoss) return;
    this.loading.set(true);
    this.error.set(null);
    this.raids
      .getOpenRaids(this.raidBoss.id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (runs) => this.openRaids.set(runs),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  create(): void {
    const tier = this.currentTier();
    if (!tier || this.action()) return;
    this.action.set('create');
    this.raids
      .create(this.raidBoss.id, tier.tier)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (run) => void this.router.navigate(['/game/world/raid', run.id]),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  createDevelopment(): void {
    const tier = this.currentTier();
    if (!tier || this.action()) return;
    this.action.set('development-create');
    this.error.set(null);
    this.raids
      .createDevelopment(this.raidBoss.id, tier.tier)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (run) => void this.router.navigate(['/game/world/raid', run.id]),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  join(run: RaidRunSummary): void {
    if (this.action()) return;
    this.action.set(`join-${run.id}`);
    this.raids
      .join(run.id)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: (joined) =>
          void this.router.navigate(['/game/world/raid', joined.id]),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  purchase(itemId: string): void {
    if (this.action()) return;
    this.action.set(`purchase-${itemId}`);
    this.error.set(null);
    this.raids
      .purchaseTrophyVendorItem(this.raidBoss.id, itemId)
      .pipe(finalize(() => this.action.set(null)))
      .subscribe({
        next: () => this.loadVendor(),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  closesIn(value: string): string {
    const milliseconds = new Date(value).getTime() - Date.now();
    if (milliseconds <= 0) return 'closing';
    const hours = Math.floor(milliseconds / 3_600_000);
    const minutes = Math.max(1, Math.floor((milliseconds % 3_600_000) / 60_000));
    return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
  }

  selectTier(tier: number): void {
    if (!this.action()) this.selectedTier.set(tier);
  }

  changeDifficulty(direction: -1 | 1): void {
    if (this.action()) return;
    const tiers = this.raidBoss.tiers;
    const index = tiers.findIndex((tier) => tier.tier === this.selectedTier());
    const next = tiers[index + direction];
    if (next) this.selectedTier.set(next.tier);
  }

  canChangeDifficulty(direction: -1 | 1): boolean {
    const tiers = this.raidBoss.tiers;
    const index = tiers.findIndex((tier) => tier.tier === this.selectedTier());
    return index >= 0 && !!tiers[index + direction];
  }

  difficultyLabel(plusLevel: number): string {
    return plusLevel === 0 ? 'Regular' : `+${plusLevel}`;
  }

  currentTier() {
    return this.raidBoss.tiers.find((tier) => tier.tier === this.selectedTier());
  }

  unclaimedRewardCount(): number {
    return this.history().filter((entry) => entry.canClaim).length;
  }

  private loadVendor(): void {
    if (!this.raidBoss) return;
    this.raids.getTrophyVendor(this.raidBoss.id).subscribe({
      next: (vendor) => this.vendor.set(vendor),
      error: (error) => this.error.set(this.errorMessage(error)),
    });
  }

  private loadHistory(): void {
    if (!this.raidBoss) return;
    this.historyLoading.set(true);
    this.raids
      .getHistory(undefined, 20)
      .pipe(finalize(() => this.historyLoading.set(false)))
      .subscribe({
        next: (history) => this.history.set(history),
        error: (error) => this.error.set(this.errorMessage(error)),
      });
  }

  private errorMessage(error: any): string {
    return error?.errorMessage ?? error?.error?.errorMessage ?? 'Raid action failed.';
  }
}
