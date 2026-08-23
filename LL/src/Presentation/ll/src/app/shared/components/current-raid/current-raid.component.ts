import { NgClass, NgIf } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RaidService } from '../../../core/services/api/raid/raid.service';

@Component({
  selector: 'app-current-raid',
  imports: [NgIf, NgClass, RouterLink],
  templateUrl: './current-raid.component.html',
})
export class CurrentRaidComponent {
  private readonly raidService = inject(RaidService);

  readonly activeRaid = this.raidService.activeRaid;

  readonly raidTitle = computed(
    () => this.activeRaid()?.raidBossName ?? 'Raid',
  );

  readonly statusLabel = computed(() => {
    const raid = this.activeRaid();
    if (!raid) return 'Raid';

    const hasPendingRequest = raid.joinRequests?.some(
      (request) => request.isCurrentCharacter,
    );
    if (hasPendingRequest) return 'Raid Request';

    switch (raid.status) {
      case 'Mustering':
        return 'In Raid';
      case 'Resolving':
        return 'Raid Resolving';
      case 'Playback':
        return 'Raid Ready';
      default:
        return 'Raid';
    }
  });

  readonly progressText = computed(() => {
    const raid = this.activeRaid();
    if (!raid) return '';

    switch (raid.status) {
      case 'Mustering':
        return raid.joinRequests?.some((request) => request.isCurrentCharacter)
          ? 'Awaiting approval'
          : 'Recruiting';
      case 'Resolving':
        return 'Battle resolving';
      case 'Playback':
        return 'Battle ongoing';
      default:
        return '';
    }
  });

  readonly statusDotClasses = computed(() => {
    const raid = this.activeRaid();
    if (raid?.joinRequests?.some((request) => request.isCurrentCharacter)) {
      return 'bg-[var(--ll-color-warning)]';
    }

    return raid?.status === 'Resolving'
      ? 'animate-pulse bg-primary'
      : 'bg-[var(--ll-color-success)]';
  });
}
