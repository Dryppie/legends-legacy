import { Injectable, signal } from '@angular/core';
import {
  CombatRewardBreakdown,
  CombatSessionDto,
  SessionSummary,
} from '../../../../shared/models/Dtos/combatResultDto';
import { InventoryItem } from '../../../../shared/models/inventoryItem';

@Injectable({
  providedIn: 'root',
})
export class SessionSummaryService {
  readonly combatSession = signal<CombatSessionDto | null | undefined>(null);
  private pendingCombatSession: CombatSessionDto | null = null;

  loadCombatSince(
    session: CombatSessionDto | undefined,
    hasPendingResolution = false,
  ): CombatSessionDto | null {
    if (!session?.combatSummary) return null;

    const accumulated =
      this.pendingCombatSession &&
      this.sessionsAreContiguous(this.pendingCombatSession, session)
        ? this.mergeCombatSessions(this.pendingCombatSession, session)
        : session;

    if (hasPendingResolution) {
      this.pendingCombatSession = accumulated;
      return null;
    }

    this.pendingCombatSession = null;
    if (accumulated.combatSummary.totalBattles > 5) {
      this.combatSession.set(accumulated);
    }

    return accumulated;
  }

  dismiss() {
    this.pendingCombatSession = null;
    this.combatSession.set(undefined);
  }

  private mergeCombatSessions(
    first: CombatSessionDto,
    second: CombatSessionDto,
  ): CombatSessionDto {
    const firstTo = new Date(first.to).getTime();
    const secondTo = new Date(second.to).getTime();
    const latest =
      second.combatSummary.totalBattles > 0 && secondTo >= firstTo
        ? second
        : first;

    return {
      from:
        new Date(first.from).getTime() <= new Date(second.from).getTime()
          ? first.from
          : second.from,
      to: firstTo >= secondTo ? first.to : second.to,
      combatResult: latest.combatResult,
      combatSummary: this.mergeCombatSummaries(
        first.combatSummary,
        second.combatSummary,
      ),
    };
  }

  private sessionsAreContiguous(
    first: CombatSessionDto,
    second: CombatSessionDto,
  ): boolean {
    return new Date(first.to).getTime() === new Date(second.from).getTime();
  }

  private mergeCombatSummaries(
    first: SessionSummary,
    second: SessionSummary,
  ): SessionSummary {
    return {
      totalBattles: first.totalBattles + second.totalBattles,
      wins: first.wins + second.wins,
      losses: first.losses + second.losses,
      draws: first.draws + second.draws,
      totalExperience: first.totalExperience + second.totalExperience,
      totalGold: (first.totalGold ?? 0) + (second.totalGold ?? 0),
      totalCinders: first.totalCinders + second.totalCinders,
      totalSoulstones: first.totalSoulstones + second.totalSoulstones,
      rewardBreakdown: this.mergeRewardBreakdowns(
        first.rewardBreakdown,
        second.rewardBreakdown,
      ),
    };
  }

  private mergeRewardBreakdowns(
    first?: CombatRewardBreakdown,
    second?: CombatRewardBreakdown,
  ): CombatRewardBreakdown {
    return {
      powerItems: this.mergeItems(first?.powerItems, second?.powerItems),
      craftingItems: this.mergeItems(
        first?.craftingItems,
        second?.craftingItems,
      ),
      essenceItems: this.mergeItems(first?.essenceItems, second?.essenceItems),
      dungeonAccessItems: this.mergeItems(
        first?.dungeonAccessItems,
        second?.dungeonAccessItems,
      ),
    };
  }

  private mergeItems(
    first: InventoryItem[] = [],
    second: InventoryItem[] = [],
  ): InventoryItem[] {
    const merged = new Map<string, InventoryItem>();

    for (const item of [...first, ...second]) {
      const key = item.itemInstance.itemBase.id;
      const existing = merged.get(key);
      if (existing) {
        existing.quantity += item.quantity;
      } else {
        merged.set(key, { ...item });
      }
    }

    return [...merged.values()];
  }
}
