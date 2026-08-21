import { signal, computed, effect, Injectable, untracked } from '@angular/core';
import { finalize, Observable, of, tap } from 'rxjs';
import {
  SoulstoneUpgradeBranch,
  SoulstoneUpgradeMutationResult,
  SoulstoneUpgradeView,
} from '../../../../shared/models/soulstones/soulstone-upgrade-view';
import { SoulstoneUpgradeService } from './soulstone-upgrade.service';
import { CharacterStateService } from '../character/character-state.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { VersionedMutationResult } from '../api.service';

export interface SoulstoneBranchGroup {
  branch: SoulstoneUpgradeBranch;
  title: string;
  upgrades: SoulstoneUpgradeView[];
}

@Injectable({
  providedIn: 'root',
})
export class SoulstoneUpgradeStateService {
  private readonly _upgrades = signal<SoulstoneUpgradeView[]>([]);
  private readonly _loading = signal(false);
  private readonly _loadedCharacterId = signal<string | null>(null);
  private readonly _loadingCharacterId = signal<string | null>(null);
  private readonly _error = signal<string | null>(null);
  private readonly _lastRefund = signal(0);
  private readonly _upgradeLoading = signal(new Map<string, boolean>());
  private loadEpoch = 0;

  readonly upgrades = computed(() => this.deriveUpgrades());
  readonly loading = computed(() => this._loading());
  readonly error = computed(() => this._error());
  readonly lastRefund = computed(() => this._lastRefund());
  readonly resetRefund = computed(() =>
    this.upgrades().reduce((total, upgrade) => total + upgrade.refundValue, 0),
  );
  readonly branchGroups = computed(() =>
    this.buildBranchGroups(this.upgrades()),
  );

  isUpgradeLoading = (id: string) =>
    computed(() => this._upgradeLoading().get(id) === true);

  constructor(
    private readonly service: SoulstoneUpgradeService,
    private readonly characterState: CharacterStateService,
    private readonly domainVersions: DomainVersionTracker,
    private readonly stateSync: StateSyncCoordinator,
  ) {
    this.stateSync.register(
      'soulstones',
      'soulstone-upgrades',
      () => this.synchronize(true),
      () => !!this.characterState.currentCharacterId(),
    );
    effect(
      () => {
        const characterId = this.characterState.currentCharacterId();

        untracked(() => {
          this._upgradeLoading.set(new Map());
          this._lastRefund.set(0);
          this._error.set(null);
          this.loadEpoch += 1;

          if (!characterId) {
            this._upgrades.set([]);
            this._loadedCharacterId.set(null);
            this._loadingCharacterId.set(null);
            return;
          }

          this.stateSync.activate('soulstones', 'soulstone-upgrades');
          if (characterId !== this._loadedCharacterId()) {
            this._upgrades.set([]);
            this.load(true);
          }
        });
      },
      { allowSignalWrites: true },
    );
  }

  load(force = false): void {
    this.synchronize(force).subscribe({ error: () => undefined });
  }

  private synchronize(force = false): Observable<unknown> {
    const characterId = this.characterState.currentCharacterId();
    if (!characterId) {
      this._upgrades.set([]);
      this._loadedCharacterId.set(null);
      this._loadingCharacterId.set(null);
      return of(undefined);
    }

    if (
      !force &&
      this._loadedCharacterId() === characterId &&
      this._upgrades().length > 0
    ) {
      return of(undefined);
    }

    if (
      !force &&
      this._loading() &&
      this._loadingCharacterId() === characterId
    ) {
      return of(undefined);
    }

    const loadEpoch = ++this.loadEpoch;
    this._loading.set(true);
    this._loadingCharacterId.set(characterId);
    this._error.set(null);
    this._lastRefund.set(0);

    return this.service.getSoulstoneUpgrades().pipe(
      tap({
        next: (list) => {
          if (
            this.characterState.currentCharacterId() !== characterId ||
            loadEpoch !== this.loadEpoch
          )
            return;

          this._upgrades.set(list);
          this._loadedCharacterId.set(characterId);
          this._error.set(null);
        },
        error: (err) => {
          if (
            this.characterState.currentCharacterId() !== characterId ||
            loadEpoch !== this.loadEpoch
          )
            return;

          this._error.set(
            err.message ?? 'Failed to load Soulstone constellations.',
          );
        },
      }),
      finalize(() => {
        if (
          this._loadingCharacterId() === characterId &&
          loadEpoch === this.loadEpoch
        ) {
          this._loading.set(false);
          this._loadingCharacterId.set(null);
        }
      }),
    );
  }

  upgrade(id: string): void {
    const upgrade = this.upgrades().find((candidate) => candidate.id === id);
    const characterId = this.characterState.currentCharacterId();
    if (!upgrade || !characterId || !upgrade.canPurchase) return;

    const map = new Map(this._upgradeLoading());
    if (map.get(id)) return;
    map.set(id, true);
    this._upgradeLoading.set(map);
    this._error.set(null);
    this._lastRefund.set(0);

    this.service
      .upgrade(id)
      .pipe(
        finalize(() => {
          const next = new Map(this._upgradeLoading());
          next.set(id, false);
          this._upgradeLoading.set(next);
        }),
      )
      .subscribe({
        next: (result) => this.applyMutationResult(result, characterId),
        error: (err) => {
          if (this.characterState.currentCharacterId() !== characterId) return;

          this._error.set(err.message ?? 'Upgrade failed.');
          this.load(true);
        },
      });
  }

  reset(): void {
    if (this._loading()) return;
    this._upgradeLoading.set(new Map());
    const characterId = this.characterState.currentCharacterId();
    if (!characterId) return;

    this._loading.set(true);
    this._loadingCharacterId.set(characterId);
    this._error.set(null);

    this.service
      .resetSoulstoneUpgrades()
      .pipe(
        finalize(() => {
          if (this._loadingCharacterId() === characterId) {
            this._loading.set(false);
            this._loadingCharacterId.set(null);
          }
        }),
      )
      .subscribe({
        next: (result) => this.applyMutationResult(result, characterId),
        error: (err) => {
          if (this.characterState.currentCharacterId() !== characterId) return;

          this._error.set(err.message ?? 'Reset failed.');
          this.load(true);
        },
      });
  }

  private applyMutationResult(
    result: VersionedMutationResult<SoulstoneUpgradeMutationResult>,
    characterId: string,
  ): void {
    if (this.characterState.currentCharacterId() !== characterId) return;

    const response = result.data;

    if (
      this.domainVersions.isCurrent(
        'soulstones',
        result.domainVersions['soulstones'],
      )
    ) {
      this.loadEpoch += 1;
      this._upgrades.set(response.upgrades);
      this._loadedCharacterId.set(characterId);
      this._lastRefund.set(response.refundedSoulstones ?? 0);
      this._error.set(null);
    }

    const latestCharacter = this.characterState.currentCharacter();
    if (
      !latestCharacter ||
      !this.domainVersions.isCurrent(
        'character',
        result.domainVersions['character'],
      )
    )
      return;

    this.characterState.updateCharacter({
      ...latestCharacter,
      soulstones: response.soulstones,
    });
  }

  private buildBranchGroups(
    upgrades: SoulstoneUpgradeView[],
  ): SoulstoneBranchGroup[] {
    return branchOrder
      .map((branch) => ({
        branch,
        title: branchTitles[branch],
        upgrades: upgrades
          .filter((upgrade) => upgrade.branch === branch)
          .sort(
            (a, b) =>
              a.sortOrder - b.sortOrder ||
              a.displayName.localeCompare(b.displayName),
          ),
      }))
      .filter((group) => group.upgrades.length > 0);
  }

  private deriveUpgrades(): SoulstoneUpgradeView[] {
    const soulstones = this.characterState.currentCharacter()?.soulstones ?? 0;

    return this._upgrades().map((upgrade) => {
      const affordabilityCanChange =
        upgrade.canPurchase ||
        upgrade.disabledReason === 'Not enough Soulstones.';
      if (!affordabilityCanChange || upgrade.nextCost == null) return upgrade;

      const canPurchase = upgrade.nextCost <= soulstones;
      return {
        ...upgrade,
        canPurchase,
        disabledReason: canPurchase ? null : 'Not enough Soulstones.',
      };
    });
  }
}

const branchOrder: SoulstoneUpgradeBranch[] = [
  'EssenceArchive',
  'CombatProgression',
  'Gathering',
  'Crafting',
  'Dungeons',
  'AccountConvenience',
];

const branchTitles: Record<SoulstoneUpgradeBranch, string> = {
  EssenceArchive: 'Essence & Archive',
  CombatProgression: 'Combat Progression',
  Gathering: 'Gathering',
  Crafting: 'Crafting',
  Dungeons: 'Dungeons',
  AccountConvenience: 'Account Convenience',
};
