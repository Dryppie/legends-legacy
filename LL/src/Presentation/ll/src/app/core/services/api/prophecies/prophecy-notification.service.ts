import { Injectable } from '@angular/core';
import {
  NOTIFICATION_SURFACE,
  NotificationService,
  SIDEBAR_NOTIFICATION,
} from '../../client-side/notifications/notification.service';
import {
  PropheciesOverviewDto,
  ProphecyInstanceDto,
  ProphecyService,
} from './prophecy.service';

@Injectable({
  providedIn: 'root',
})
export class ProphecyNotificationService {
  private loading = false;

  constructor(
    private readonly prophecyService: ProphecyService,
    private readonly notificationService: NotificationService,
  ) {}

  refreshCount(): void {
    if (this.loading) return;

    this.loading = true;
    this.prophecyService.getOverview().subscribe({
      next: (overview) => {
        this.syncFromOverview(overview);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  syncFromOverview(overview: PropheciesOverviewDto | null): void {
    this.notificationService.setCount(
      NOTIFICATION_SURFACE.Sidebar,
      SIDEBAR_NOTIFICATION.Prophecies,
      this.countOverviewActions(overview),
    );
  }

  private countOverviewActions(overview: PropheciesOverviewDto | null): number {
    if (!overview) return 0;

    return (
      this.countClaimableProphecies(overview) +
      this.countClaimableMilestones(overview) +
      this.countOwnedCaches(overview) +
      (this.needsDailyChoice(overview.dailyProphecies) ? 1 : 0)
    );
  }

  private countClaimableProphecies(overview: PropheciesOverviewDto): number {
    return [
      ...overview.dailyProphecies,
      overview.greaterProphecy,
    ].filter((prophecy) => this.canClaim(prophecy)).length;
  }

  private countClaimableMilestones(overview: PropheciesOverviewDto): number {
    return overview.weeklyRevelation.milestones.filter(
      (milestone) => milestone.isUnlocked && !milestone.isClaimed,
    ).length;
  }

  private countOwnedCaches(overview: PropheciesOverviewDto): number {
    return overview.caches.reduce((sum, cache) => sum + cache.quantity, 0);
  }

  private needsDailyChoice(dailyProphecies: ProphecyInstanceDto[]): boolean {
    if (dailyProphecies.length === 0) return false;

    const hasChosenDaily = dailyProphecies.some((prophecy) =>
      prophecy.status === 'Accepted' ||
      prophecy.status === 'Completed' ||
      prophecy.status === 'Claimed',
    );

    return !hasChosenDaily && dailyProphecies.some((prophecy) => prophecy.status === 'Offered');
  }

  private canClaim(prophecy: ProphecyInstanceDto): boolean {
    return prophecy.status === 'Completed' ||
      (
        prophecy.status === 'Accepted' &&
        prophecy.targetValue > 0 &&
        prophecy.currentValue >= prophecy.targetValue
      );
  }
}
