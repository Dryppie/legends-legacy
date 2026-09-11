import { Injectable, computed, effect, signal, untracked } from '@angular/core';
import { Observable, of, Subscription } from 'rxjs';
import {
  CombatStyleOverview,
  CombatStyleSelectionRequest,
} from '../../../../shared/models/combat-styles';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { CharacterStateService } from '../character/character-state.service';
import { VersionedMutationResult } from '../api.service';
import { CombatStylesService } from './combat-styles.service';

const emptySelection = (): CombatStyleSelectionRequest => ({
  combatStyleId: null,
  refinementId: null,
  upgradeIds: [],
  masteredUpgradeId: null,
});

@Injectable({ providedIn: 'root' })
export class CombatStyleStateService {
  readonly data = signal<CombatStyleOverview | null>(null);
  readonly draft = signal<CombatStyleSelectionRequest>(emptySelection());
  readonly preview = signal<CombatStyleOverview | null>(null);
  readonly loading = signal(false);
  readonly previewing = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly previewError = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly dirty = signal(true);
  readonly edited = computed(() => {
    const saved = this.data()?.selection;
    return !!saved && !this.sameSelection(this.draft(), saved);
  });
  readonly selected = computed(
    () =>
      this.data()?.styles.find(
        (entry) => entry.definition.id === this.draft().combatStyleId,
      ) ?? null,
  );
  readonly masteryInvalid = computed(() => {
    const id = this.draft().masteredUpgradeId;
    if (!id) return false;
    const entry = this.selected();
    return (
      !entry ||
      entry.level < 9 ||
      !this.draft().upgradeIds.includes(id) ||
      !entry.definition.upgrades.some((upgrade) => upgrade.id === id)
    );
  });
  readonly canSave = computed(
    () =>
      this.data() !== null &&
      this.edited() &&
      this.draft().combatStyleId !== null &&
      !this.busy() &&
      !this.loading() &&
      !this.dirty() &&
      !this.previewing() &&
      !this.previewError() &&
      this.preview() !== null &&
      !this.preview()?.validationIssue &&
      !this.masteryInvalid(),
  );
  private epoch = 0;
  private previewEpoch = 0;
  private previewRequest?: Subscription;
  private accountEpoch = 0;
  private revision = 0;
  private dirtyGeneration = 0;

  constructor(
    private readonly api: CombatStylesService,
    events: EventBusService,
    private readonly sync: StateSyncCoordinator,
    private readonly versions: DomainVersionTracker,
    private readonly character: CharacterStateService,
  ) {
    sync.register(
      'combat-styles',
      'combat-styles',
      ({ targetRevision }) => {
        if (targetRevision > this.revision) this.invalidate();
        return of(undefined);
      },
      () => this.data() !== null,
    );
    effect(() => {
      if (events.logout()) untracked(() => this.reset());
    });
  }

  invalidate() {
    ++this.dirtyGeneration;
    this.cancelPreview();
    this.dirty.set(true);
  }

  refreshIfDirty() {
    if (!this.loading() && (this.dirty() || !this.data())) this.refresh();
  }

  refresh() {
    const epoch = ++this.epoch;
    const generation = this.dirtyGeneration;
    const revision = this.latestRevision();
    this.loading.set(true);
    this.error.set(null);
    this.api.get().subscribe({
      next: (data) => {
        if (epoch !== this.epoch) return;
        const preserveDraft = this.edited();
        this.data.set(data);
        this.loading.set(false);
        this.revision = revision;
        this.dirty.set(
          generation !== this.dirtyGeneration ||
            this.latestRevision() > revision,
        );
        if (!preserveDraft)
          this.draft.set({
            ...data.selection,
            masteredUpgradeId: data.selection.masteredUpgradeId ?? null,
            upgradeIds: [...data.selection.upgradeIds],
          });
        // An update received during this request can leave dirty already true,
        // so the page's dirty effect will not run again when loading ends.
        if (this.dirty()) {
          this.refresh();
          return;
        }
        this.requestPreview();
        this.sync.activate('combat-styles', 'combat-styles');
      },
      error: (error) => {
        if (epoch === this.epoch) {
          this.loading.set(false);
          this.error.set(
            this.errorText(error, 'Could not load Combat Styles.'),
          );
        }
      },
    });
  }

  chooseStyle(id: string | null) {
    if (!id || this.busy()) return;
    const entry = this.data()?.styles.find(
      (style) => style.definition.id === id,
    );
    this.changeDraft({
      ...emptySelection(),
      combatStyleId: id,
      refinementId: entry?.refinementId ?? null,
      upgradeIds: [...(entry?.upgradeIds ?? [])],
      masteredUpgradeId: entry?.masteredUpgradeId ?? null,
    });
  }

  changeRefinement(refinementId: string | null) {
    this.changeDraft({ ...this.draft(), refinementId });
  }

  changeMastery(masteredUpgradeId: string | null) {
    if (this.busy()) return;
    if (
      masteredUpgradeId &&
      ((this.selected()?.level ?? 0) < 9 ||
        !this.draft().upgradeIds.includes(masteredUpgradeId) ||
        !this.selected()?.definition.upgrades.some(
          (upgrade) => upgrade.id === masteredUpgradeId,
        ))
    )
      return;
    this.changeDraft({ ...this.draft(), masteredUpgradeId });
  }

  toggleUpgrade(id: string) {
    if (this.busy()) return;
    const selection = this.draft();
    const exists = selection.upgradeIds.includes(id);
    if (
      !exists &&
      selection.upgradeIds.length >= (this.selected()?.upgradeSlots ?? 0)
    )
      return;
    this.changeDraft({
      ...selection,
      upgradeIds: exists
        ? selection.upgradeIds.filter((value) => value !== id)
        : [...selection.upgradeIds, id],
      masteredUpgradeId:
        exists && selection.masteredUpgradeId === id
          ? null
          : (selection.masteredUpgradeId ?? null),
    });
  }

  resetDraft() {
    const selection = this.data()?.selection;
    if (selection) {
      this.draft.set({
        ...selection,
        masteredUpgradeId: selection.masteredUpgradeId ?? null,
        upgradeIds: [...selection.upgradeIds],
      });
      this.requestPreview();
    }
  }

  private changeDraft(selection: CombatStyleSelectionRequest) {
    if (this.sameSelection(selection, this.draft()) && !this.previewError())
      return;
    this.draft.set(selection);
    this.message.set(null);
    this.requestPreview();
  }

  requestPreview() {
    this.cancelPreview();
    const epoch = this.previewEpoch;
    const account = this.accountEpoch;
    this.previewError.set(null);
    const selection = this.draft();
    const data = this.data();
    // Overview and save responses already contain the authoritative preview.
    if (
      data &&
      !this.dirty() &&
      !this.loading() &&
      this.sameSelection(selection, data.selection)
    ) {
      this.preview.set(data);
      return;
    }
    this.previewing.set(true);
    // Keep the existing card mounted during edits; a different style needs its
    // own preview, but same-style validation must not collapse the page.
    if (this.preview()?.selection.combatStyleId !== selection.combatStyleId)
      this.preview.set(null);
    this.previewRequest = this.api.preview(selection).subscribe({
      next: (preview) => {
        if (account !== this.accountEpoch || epoch !== this.previewEpoch)
          return;
        this.preview.set(preview);
        this.previewing.set(false);
      },
      error: (error) => {
        if (account === this.accountEpoch && epoch === this.previewEpoch) {
          this.previewing.set(false);
          this.previewError.set(
            this.errorText(error, 'Could not preview this configuration.'),
          );
        }
      },
    });
  }

  private cancelPreview() {
    ++this.previewEpoch;
    this.previewRequest?.unsubscribe();
    this.previewRequest = undefined;
    this.previewing.set(false);
  }

  private sameSelection(
    first: CombatStyleSelectionRequest,
    second: CombatStyleSelectionRequest,
  ) {
    return (
      first.combatStyleId === second.combatStyleId &&
      first.refinementId === second.refinementId &&
      (first.masteredUpgradeId ?? null) ===
        (second.masteredUpgradeId ?? null) &&
      !!first.restoreRememberedChoices === !!second.restoreRememberedChoices &&
      first.upgradeIds.length === second.upgradeIds.length &&
      first.upgradeIds.every((id, index) => id === second.upgradeIds[index])
    );
  }

  save() {
    if (this.canSave())
      this.mutate(
        this.api.select(this.draft()),
        'Combat Style saved for the next eligible encounter.',
      );
  }

  private mutate(
    request: Observable<VersionedMutationResult<CombatStyleOverview>>,
    message: string,
  ) {
    const account = this.accountEpoch;
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    request.subscribe({
      next: (result) => {
        if (account !== this.accountEpoch) return;
        this.busy.set(false);
        if (
          !this.versions.isCurrent(
            'combat-styles',
            result.domainVersions['combat-styles'],
          )
        ) {
          this.invalidate();
          this.refresh();
          return;
        }
        ++this.epoch;
        this.data.set(result.data);
        this.draft.set({
          ...result.data.selection,
          masteredUpgradeId: result.data.selection.masteredUpgradeId ?? null,
          upgradeIds: [...result.data.selection.upgradeIds],
        });
        this.revision =
          result.domainVersions['combat-styles'] ?? this.latestRevision();
        this.dirty.set(false);
        this.loading.set(false);
        this.message.set(message);
        this.character.markOverviewDirty();
        this.requestPreview();
      },
      error: (error) => {
        if (account === this.accountEpoch) {
          this.busy.set(false);
          this.error.set(
            this.errorText(error, 'Combat Style could not be changed.'),
          );
        }
      },
    });
  }

  private latestRevision() {
    return Math.max(
      this.sync.latestRevision('combat-styles'),
      this.versions.latest('combat-styles'),
    );
  }
  private errorText(error: any, fallback: string) {
    return error?.errorMessage ?? error?.message ?? fallback;
  }

  reset() {
    ++this.accountEpoch;
    ++this.epoch;
    this.cancelPreview();
    ++this.dirtyGeneration;
    this.revision = 0;
    this.data.set(null);
    this.draft.set(emptySelection());
    this.preview.set(null);
    this.loading.set(false);
    this.previewing.set(false);
    this.busy.set(false);
    this.error.set(null);
    this.previewError.set(null);
    this.message.set(null);
    this.dirty.set(true);
  }
}
