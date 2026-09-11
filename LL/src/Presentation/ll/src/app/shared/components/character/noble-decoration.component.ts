import { Component, Input, OnChanges, OnDestroy, computed, inject, signal } from '@angular/core';
import { Subscription, switchMap, timer } from 'rxjs';
import { NobilityAppearance, NobilityService } from '../../../core/services/api/nobility/nobility.service';
import { TimeSyncService } from '../../../core/services/api/time-sync/time-sync.service';

@Component({ selector: 'app-noble-decoration', template: `
  @if (visible()) {
    <span class="mr-1 inline-block align-middle text-[10px] leading-none text-primary" role="img" aria-label="Noble" title="Active Nobility">◆</span>
  }
` })
export class NobleDecorationComponent implements OnChanges, OnDestroy {
  @Input() characterId = '';
  private readonly nobility = inject(NobilityService);
  private readonly time = inject(TimeSyncService);
  private readonly appearance = signal<NobilityAppearance | null>(null);
  private readonly clock = signal(Date.now());
  private expiry?: ReturnType<typeof setTimeout>;
  private updates?: Subscription;
  readonly active = computed(() => {
    const value = this.appearance();
    return value?.expiresAt && Date.parse(value.expiresAt) > this.clock() ? value : null;
  });
  readonly visible = computed(() => {
    const value = this.active();
    return value?.showBadge ? value : null;
  });
  ngOnChanges(): void {
    this.updates?.unsubscribe(); this.appearance.set(null); clearTimeout(this.expiry);
    if (!this.characterId) return;
    this.updates = timer(0, 60_000).pipe(switchMap(() => this.nobility.publicAppearance(this.characterId))).subscribe(value => {
      this.appearance.set(value); this.clock.set(this.time.now()); clearTimeout(this.expiry);
      if (value.expiresAt) {
        const delay = Date.parse(value.expiresAt) - this.time.now();
        if (delay > 0) this.expiry = setTimeout(() => this.appearance.set(null), Math.min(delay, 2_147_000_000));
      }
    });
  }
  ngOnDestroy(): void { this.updates?.unsubscribe(); clearTimeout(this.expiry); }
}
