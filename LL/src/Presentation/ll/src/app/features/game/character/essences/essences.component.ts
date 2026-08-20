import { CommonModule } from '@angular/common';
import {
  CdkVirtualScrollViewport,
  ScrollingModule,
} from '@angular/cdk/scrolling';
import {
  Component,
  computed,
  effect,
  inject,
  OnInit,
  signal,
  untracked,
  ViewChild,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs';
import {
  EssenceStateService,
  EssenceView,
} from '../../../../core/services/api/essences/essence-state.service';
import {
  NavigationTab,
  NavigationTabsComponent,
} from '../../../../shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { EssenceDescriptionComponent } from '../../../../shared/components/essences/essence-description/essence-description.component';
import { EssenceDetailsComponent } from '../../../../shared/components/essences/essence-details/essence-details.component';
import { AbilityTagsComponent } from '../../../../shared/components/essences/ability-tags/ability-tags.component';
import {
  CreatureArchiveEntryDto,
  EssenceCodexEntryDto,
  EssenceCodexMemberDto,
  EssenceDefinitionDto,
  EssenceLoadoutDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';
import { EssencesAbsorbComponent } from './essences-absorb/essences-absorb.component';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { QuestPresenterService } from '../../../../core/services/api/quest/quest-presenter.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import {
  canSpendEssenceDust,
  essenceDustActionLabel,
  essenceDustLevelingDescription,
} from './essence-leveling.utils';
import {
  ONBOARDING_GOBLIN_ESSENCE_DEFINITION_ID,
  TRAINING_DAY_QUEST_ID,
} from '../../../../shared/models/quest';
import { PopoverComponent } from '../../../../shared/components/custom-components/popover/popover.component';
import { EssenceItemViewService } from '../../../../core/services/api/essences/essence-item-view.service';
import { Essence } from '../../../../shared/models/essence';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import {
  CreatureEssenceFilter,
  creatureArchiveSearchText,
  matchesCreatureEssenceFilter,
} from './creature-archive-search';
import { playerEssenceSearchText } from '../../../../shared/search/essence-search';

type ArchiveFilter = 'all' | 'favorites' | 'attuned' | 'ready';
type ArchiveSort = 'name' | 'level' | 'tier';
type CreatureSourceFilter = 'all' | 'Area' | 'Dungeon';
interface AscendRequirementView {
  label: string;
  current?: number;
  required?: number;
  isMet: boolean;
}

@Component({
  selector: 'app-essences',
  imports: [
    CommonModule,
    ScrollingModule,
    FormsModule,
    DefaultHeaderComponent,
    EssenceDescriptionComponent,
    EssenceDetailsComponent,
    AbilityTagsComponent,
    PopoverComponent,
    NavigationTabsComponent,
    EssencesAbsorbComponent,
    DropdownComponent,
  ],
  templateUrl: './essences.component.html',
  styleUrls: ['./essences.component.scss'],
})
export class EssencesComponent implements OnInit {
  @ViewChild(CdkVirtualScrollViewport)
  private archiveViewport?: CdkVirtualScrollViewport;

  private lastPreparedQuestObjective: string | null = null;
  private lastCreatureArchiveCombatRevision: string | null = null;
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly routeEssenceId = toSignal(
    this.route.paramMap.pipe(map((params) => params.get('essenceId'))),
    { initialValue: null },
  );
  private readonly requestedView = toSignal(
    this.route.queryParamMap.pipe(map((params) => params.get('view'))),
    { initialValue: null },
  );
  readonly archiveSearch = signal('');
  readonly mobileLoadoutOpen = signal(false);
  readonly mobileDetailOpen = computed(
    () => this.routeEssenceId() !== null && !this.mobileLoadoutOpen(),
  );
  readonly attunedEssenceCount = computed(
    () =>
      this.essenceState
        .archive()
        ?.essences.filter(
          (essence) =>
            essence.attunedSlot !== null && essence.attunedSlot !== undefined,
        ).length ?? 0,
  );
  readonly creatureSearch = signal('');
  readonly creatureRegionFilter = signal('all');
  readonly creatureSourceFilter = signal<CreatureSourceFilter>('all');
  readonly creatureLocationFilter = signal('all');
  readonly creatureEssenceFilter = signal<CreatureEssenceFilter>('all');
  readonly archiveFilter = signal<ArchiveFilter>('all');
  readonly archiveSort = signal<ArchiveSort>('name');
  readonly viewTabs = computed<readonly NavigationTab[]>(() => [
    { key: 'archive', label: 'Archive' },
    { key: 'absorb', label: 'Absorb' },
    {
      key: 'creatures',
      label: 'Creatures',
      badgeCount: this.essenceState.essenceFocusReady() ? 1 : 0,
      badgeLabel: 'Essence Focus ready',
    },
    { key: 'codex', label: 'Codex' },
  ]);

  readonly archiveFilters: { label: string; value: ArchiveFilter }[] = [
    { label: 'All', value: 'all' },
    { label: 'Favorites', value: 'favorites' },
    { label: 'Attuned', value: 'attuned' },
    { label: 'Ready', value: 'ready' },
  ];

  readonly archiveSorts: { label: string; value: ArchiveSort }[] = [
    { label: 'Name', value: 'name' },
    { label: 'Level', value: 'level' },
    { label: 'Tier', value: 'tier' },
  ];

  readonly creatureSourceOptions: readonly DropdownOption<CreatureSourceFilter>[] =
    [
      { label: 'Areas and dungeons', value: 'all' },
      { label: 'Areas', value: 'Area' },
      { label: 'Dungeons', value: 'Dungeon' },
    ];

  readonly creatureEssenceOptions: readonly DropdownOption<CreatureEssenceFilter>[] =
    [
      { label: 'All', value: 'all' },
      { label: 'Found', value: 'found' },
      { label: 'Not found', value: 'not-found' },
    ];

  /**
   * Definitions keyed by Essence definition id so Archive search can also match
   * on the Essence description and rarity, which the player-owned DTO omits.
   */
  readonly essenceDefinitionsById = computed(() => {
    const definitions = new Map<string, EssenceDefinitionDto>();

    for (const creature of this.essenceState.creatureArchive()?.creatures ??
      []) {
      for (const essence of creature.essences) {
        if (essence.definition) {
          definitions.set(essence.essenceDefinitionId, essence.definition);
        }
      }
    }

    for (const entry of this.essenceState.codex()?.entries ?? []) {
      for (const member of entry.essences) {
        if (member.essenceDefinitionId && member.definition) {
          definitions.set(member.essenceDefinitionId, member.definition);
        }
      }
    }

    return definitions;
  });

  readonly filteredArchiveEssences = computed(() => {
    const search = this.archiveSearch().trim().toLowerCase();
    const filter = this.archiveFilter();
    const sort = this.archiveSort();
    const definitions = this.essenceDefinitionsById();
    const essences = [...(this.essenceState.archive()?.essences ?? [])];

    return essences
      .filter((essence) => {
        if (filter === 'favorites' && !essence.isFavorite) return false;
        if (
          filter === 'attuned' &&
          (essence.attunedSlot === null || essence.attunedSlot === undefined)
        ) {
          return false;
        }
        if (filter === 'ready' && !this.canAscendEssence(essence)) {
          return false;
        }

        if (!search) return true;

        return playerEssenceSearchText(
          essence,
          definitions.get(essence.essenceDefinitionId),
        ).includes(search);
      })
      .sort((a, b) => {
        switch (sort) {
          case 'level':
            return b.level - a.level || a.name.localeCompare(b.name);
          case 'tier':
            return (
              b.ascensionTier - a.ascensionTier ||
              b.level - a.level ||
              a.name.localeCompare(b.name)
            );
          default:
            return a.name.localeCompare(b.name);
        }
      });
  });

  readonly filteredCreatures = computed(() => {
    const search = this.creatureSearch().trim().toLowerCase();
    const region = this.creatureRegionFilter();
    const source = this.creatureSourceFilter();
    const locationFilter = this.creatureLocationFilter();
    const essenceFilter = this.creatureEssenceFilter();
    const creatures = this.essenceState.creatureArchive()?.creatures ?? [];

    return creatures.filter((creature) => {
      const matchesLocation = creature.locations.some(
        (location) =>
          (region === 'all' || location.regionId.toString() === region) &&
          (source === 'all' || location.sourceType === source) &&
          (locationFilter === 'all' ||
            this.creatureLocationKey(location) === locationFilter),
      );
      if (
        (region !== 'all' || source !== 'all' || locationFilter !== 'all') &&
        !matchesLocation
      ) {
        return false;
      }

      if (!matchesCreatureEssenceFilter(creature, essenceFilter)) return false;

      if (!search) return true;

      return creatureArchiveSearchText(creature).includes(search);
    });
  });

  readonly creatureRegionOptions = computed<readonly DropdownOption<string>[]>(
    () => {
      const regions = new Map<number, string>();
      for (const creature of this.essenceState.creatureArchive()?.creatures ??
        []) {
        for (const location of creature.locations) {
          regions.set(location.regionId, location.regionName);
        }
      }

      return [
        { label: 'All regions', value: 'all' },
        ...[...regions.entries()]
          .sort(([left], [right]) => left - right)
          .map(([id, name]) => ({ label: name, value: id.toString() })),
      ];
    },
  );

  readonly creatureLocationOptions = computed<
    readonly DropdownOption<string>[]
  >(() => {
    const region = this.creatureRegionFilter();
    const source = this.creatureSourceFilter();
    const locations = new Map<
      string,
      { sourceName: string; sourceType: 'Area' | 'Dungeon' }
    >();

    for (const creature of this.essenceState.creatureArchive()?.creatures ??
      []) {
      for (const location of creature.locations) {
        if (region !== 'all' && location.regionId.toString() !== region) {
          continue;
        }
        if (source !== 'all' && location.sourceType !== source) continue;

        locations.set(this.creatureLocationKey(location), {
          sourceName: location.sourceName,
          sourceType: location.sourceType,
        });
      }
    }

    return [
      { label: 'All locations', value: 'all' },
      ...[...locations.entries()]
        .sort(([, left], [, right]) =>
          left.sourceName.localeCompare(right.sourceName),
        )
        .map(([value, location]) => ({
          label:
            source === 'all'
              ? `${location.sourceName} (${location.sourceType})`
              : location.sourceName,
          value,
        })),
    ];
  });

  readonly unlockedCodexEntries = computed(
    () =>
      this.essenceState.codex()?.entries.filter((entry) => entry.isUnlocked)
        .length ?? 0,
  );

  constructor(
    public readonly essenceState: EssenceStateService,
    public readonly questState: QuestStateService,
    private readonly questPresenter: QuestPresenterService,
    private readonly inventoryState: InventoryStateService,
    private readonly essenceItemView: EssenceItemViewService,
    private readonly characterActions: CharacterActionsStateService,
  ) {
    effect(() => {
      const view = this.requestedView();
      if (
        view === 'archive' ||
        view === 'absorb' ||
        view === 'creatures' ||
        view === 'codex'
      ) {
        this.essenceState.setActiveView(view);
      }
    });

    effect(
      () => {
        const objective = this.questState.pinnedOnboardingObjective();
        if (!objective) {
          this.lastPreparedQuestObjective = null;
          return;
        }

        if (objective.key === this.lastPreparedQuestObjective) return;
        this.lastPreparedQuestObjective = objective.key;

        if (objective.type === 'EssenceAbsorbed') {
          this.essenceState.setActiveView('absorb');
          return;
        }

        if (objective.type === 'EssenceEquipped') {
          this.essenceState.setActiveView('archive');
          untracked(() => this.questPresenter.presentCurrentObjective());
        }
      },
      { allowSignalWrites: true },
    );

    effect(() => {
      const essenceId = this.routeEssenceId();
      const archive = this.essenceState.archive();
      if (!essenceId || !archive) return;

      const essence = archive.essences.find((entry) => entry.id === essenceId);
      untracked(() => {
        this.essenceState.setActiveView('archive');
        if (essence) {
          this.essenceState.selectPlayerEssence(essence);
          return;
        }

        this.router.navigate(['/game/character/essences'], {
          replaceUrl: true,
        });
      });
    });

    effect(() => {
      if (
        this.essenceState.activeView() !== 'creatures' ||
        this.characterActions.resolvingOfflineProgress()
      ) {
        return;
      }

      const action = this.characterActions.currentAction();
      if (!action?.combatSession?.combatResult) return;

      const revision =
        action.revision ??
        `${action.updatedAt}:${action.combatSession.combatResult.startedAt}`;
      if (!this.essenceState.creatureArchive()) {
        this.lastCreatureArchiveCombatRevision = revision;
        return;
      }

      if (this.lastCreatureArchiveCombatRevision === null) {
        this.lastCreatureArchiveCombatRevision = revision;
        return;
      }

      if (revision === this.lastCreatureArchiveCombatRevision) return;
      this.lastCreatureArchiveCombatRevision = revision;
      untracked(() => this.essenceState.refreshCreatureArchive());
    });
  }

  public ngOnInit(): void {
    if (
      this.essenceState.archive() &&
      this.essenceState.loadouts() &&
      this.essenceState.creatureArchive() &&
      this.essenceState.codex()
    ) {
      if (this.essenceState.activeView() === 'creatures') {
        this.essenceState.refreshCreatureArchive();
      }
      return;
    }

    this.essenceState.refresh(true);
  }

  public selectView(view: string): void {
    this.mobileLoadoutOpen.set(false);
    if (this.routeEssenceId()) {
      this.router.navigate(['/game/character/essences'], { replaceUrl: true });
    }

    switch (view) {
      case 'archive':
      case 'absorb':
      case 'creatures':
      case 'codex':
        this.essenceState.setActiveView(view as EssenceView);
        if (
          view === 'archive' &&
          this.questState.pinnedOnboardingObjective()?.type ===
            'EssenceEquipped'
        ) {
          this.questPresenter.presentCurrentObjective();
        }
    }
  }

  public setCreatureRegionFilter(selection: DropdownSelection<string>): void {
    this.creatureRegionFilter.set(selection.main);
    this.clearUnavailableCreatureLocation();
  }

  public setCreatureSourceFilter(
    selection: DropdownSelection<CreatureSourceFilter>,
  ): void {
    this.creatureSourceFilter.set(selection.main);
    this.clearUnavailableCreatureLocation();
  }

  public setCreatureLocationFilter(selection: DropdownSelection<string>): void {
    this.creatureLocationFilter.set(selection.main);
  }

  public setCreatureEssenceFilter(
    selection: DropdownSelection<CreatureEssenceFilter>,
  ): void {
    this.creatureEssenceFilter.set(selection.main);
  }

  private creatureLocationKey(location: {
    sourceType: 'Area' | 'Dungeon';
    sourceId: string;
  }): string {
    return `${location.sourceType}:${location.sourceId}`;
  }

  private clearUnavailableCreatureLocation(): void {
    const selectedLocation = this.creatureLocationFilter();
    if (
      selectedLocation !== 'all' &&
      !this.creatureLocationOptions().some(
        (option) => option.value === selectedLocation,
      )
    ) {
      this.creatureLocationFilter.set('all');
    }
  }

  public selectPlayerEssence(essence: PlayerEssenceDto): void {
    this.essenceState.selectPlayerEssence(essence);

    if (window.matchMedia('(max-width: 639px)').matches) {
      this.mobileLoadoutOpen.set(false);
      this.router.navigate(['/game/character/essences', essence.id]);
    }
  }

  public essenceDefinitionDetails(definition: EssenceDefinitionDto): Essence {
    return this.essenceItemView.fromDefinition(definition);
  }

  public focusLoadoutEssence(essence: PlayerEssenceDto): void {
    let archiveIndex = this.filteredArchiveEssences().findIndex(
      (entry) => entry.id === essence.id,
    );

    if (archiveIndex < 0) {
      this.archiveSearch.set('');
      this.archiveFilter.set('all');
      archiveIndex = this.filteredArchiveEssences().findIndex(
        (entry) => entry.id === essence.id,
      );
    }

    this.selectPlayerEssence(essence);

    if (archiveIndex >= 0) {
      requestAnimationFrame(() =>
        this.archiveViewport?.scrollToIndex(archiveIndex, 'smooth'),
      );
    }
  }

  public backToArchive(): void {
    this.router.navigate(['/game/character/essences'], { replaceUrl: true });
  }

  public toggleMobileLoadout(): void {
    this.mobileLoadoutOpen.update((isOpen) => !isOpen);
  }

  public favorite(essence: PlayerEssenceDto): void {
    this.essenceState.favorite(essence);
  }

  public spendDust(essence: PlayerEssenceDto): void {
    if (!this.canSpendDust(essence)) return;
    this.essenceState.spendDust(essence);
  }

  public canSpendDust(essence: PlayerEssenceDto): boolean {
    return canSpendEssenceDust(
      essence.level,
      essence.levelCap,
      this.essenceDustHeld(),
      this.essenceState.spendingDust(),
    );
  }

  public dustLevelingDescription(essence: PlayerEssenceDto): string {
    return essenceDustLevelingDescription(
      essence.level,
      essence.levelCap,
      essence.ascendInfo.nextTier !== null &&
        essence.ascendInfo.nextTier !== undefined,
      this.essenceDustHeld(),
    );
  }

  public dustActionLabel(essence: PlayerEssenceDto): string {
    return essenceDustActionLabel(
      essence.level,
      essence.levelCap,
      this.essenceDustHeld(),
      this.essenceState.spendingDust(),
    );
  }

  private essenceDustHeld(): number {
    return this.essenceState.archive()?.essenceDust ?? 0;
  }

  public ascend(essence: PlayerEssenceDto): void {
    this.essenceState.ascend(essence);
  }

  public ascendRequirements(
    essence: PlayerEssenceDto,
  ): readonly AscendRequirementView[] {
    const requirements: AscendRequirementView[] = [];
    const requiredLevel = essence.ascendInfo.requiredLevel;
    if (requiredLevel !== null && requiredLevel !== undefined) {
      requirements.push({
        label: `Level ${requiredLevel}`,
        current: essence.level,
        required: requiredLevel,
        isMet: essence.level >= requiredLevel,
      });
    }

    const requiredItems = essence.ascendInfo.requiredItemAmount;
    const requiredItemId = essence.ascendInfo.requiredItemId;
    if (
      requiredItems !== null &&
      requiredItems !== undefined &&
      requiredItemId
    ) {
      const currentItems = this.inventoryQuantity(requiredItemId);
      requirements.push({
        label: essence.ascendInfo.requiredItemName ?? 'Required item',
        current: currentItems,
        required: requiredItems,
        isMet: currentItems >= requiredItems,
      });
    }

    if (requirements.length > 0) return requirements;

    return essence.ascendInfo.requirements.map((requirement) => ({
      label: this.cleanRequirement(requirement),
      isMet: essence.ascendInfo.canPerform,
    }));
  }

  public canAscendEssence(essence: PlayerEssenceDto): boolean {
    return (
      essence.ascendInfo.nextTier !== null &&
      essence.ascendInfo.nextTier !== undefined &&
      this.ascendRequirements(essence).every((requirement) => requirement.isMet)
    );
  }

  public selectLoadout(loadout: EssenceLoadoutDto): void {
    this.essenceState.selectLoadout(loadout);
  }

  public setArchiveFilter(filter: ArchiveFilter): void {
    this.archiveFilter.set(filter);
  }

  public setArchiveSortValue(sort: string): void {
    this.archiveSort.set(sort as ArchiveSort);
  }

  public setArchiveSortSelection(selection: DropdownSelection<unknown>): void {
    this.archiveSort.set(selection.main as ArchiveSort);
  }

  public toggleEssenceSlot(essence: PlayerEssenceDto): void {
    if (this.essenceState.savingLoadout()) return;

    const equippedSlot = this.equippedDraftSlot(essence);
    if (equippedSlot !== null) {
      this.essenceState.setDraftSlot(equippedSlot, null);
      this.essenceState.saveDraftSlots();
      return;
    }

    const slotIndex = this.nextEquipSlot(essence);
    if (slotIndex === null) return;

    this.essenceState.setDraftSlot(slotIndex, essence.id);
    this.essenceState.saveDraftSlots();
  }

  public isOnboardingStarterAttunement(essence: PlayerEssenceDto): boolean {
    const starterEssenceDefinitionId =
      this.onboardingStarterEssenceDefinitionId();
    return (
      this.questState.pinnedOnboardingObjective()?.type === 'EssenceEquipped' &&
      essence.essenceDefinitionId === starterEssenceDefinitionId
    );
  }

  public equipOnboardingStarterEssence(essence: PlayerEssenceDto): void {
    if (
      this.essenceState.savingLoadout() ||
      this.equippedDraftSlot(essence) !== null
    ) {
      return;
    }

    const slotIndex = this.nextEquipSlot(essence);
    if (slotIndex === null) return;

    this.essenceState.setDraftSlot(slotIndex, essence.id);
    this.essenceState.saveDraftSlots(true);

    if (window.matchMedia('(max-width: 639px)').matches) {
      this.mobileLoadoutOpen.set(true);
      requestAnimationFrame(() =>
        window.dispatchEvent(new Event('ll-tour-layout-change')),
      );
    }
  }

  public onboardingEquipButtonText(essence: PlayerEssenceDto): string {
    const slotIndex = this.equippedDraftSlot(essence);
    return slotIndex === null
      ? 'Equip Essence'
      : `Equipped in Slot ${slotIndex + 1}`;
  }

  public saveLoadout(): void {
    this.essenceState.saveDraftLoadout();
  }

  private onboardingStarterEssenceDefinitionId(): string {
    const firstHunt = this.questState
      .journal()
      .quests.find((quest) => quest.questId === TRAINING_DAY_QUEST_ID);
    if (!firstHunt?.choice) {
      return ONBOARDING_GOBLIN_ESSENCE_DEFINITION_ID;
    }

    return (
      firstHunt.choice.options.find(
        (option) => option.key === firstHunt.choice?.selectedOptionKey,
      )?.essenceDefinitionId ?? ONBOARDING_GOBLIN_ESSENCE_DEFINITION_ID
    );
  }

  public canToggleEssenceSlot(essence: PlayerEssenceDto): boolean {
    return (
      !this.essenceState.savingLoadout() &&
      (this.equippedDraftSlot(essence) !== null ||
        this.nextEquipSlot(essence) !== null)
    );
  }

  public nextEquipSlot(essence: PlayerEssenceDto): number | null {
    if (this.equippedDraftSlot(essence) !== null) return null;

    return (
      this.essenceState
        .slotIndexes()
        .find(
          (slotIndex) =>
            !this.essenceState.draftSlots()[slotIndex] &&
            this.essenceState.canAssignEssenceToDraftSlot(
              slotIndex,
              essence.id,
            ),
        ) ?? null
    );
  }

  public equipButtonText(essence: PlayerEssenceDto): string {
    if (this.essenceState.savingLoadout()) return 'Saving...';

    const equippedSlot = this.equippedDraftSlot(essence);
    if (equippedSlot !== null) return `Remove from Slot ${equippedSlot + 1}`;
    if (!this.essenceState.loadouts()) return 'Loading slots';
    if (this.essenceState.slotIndexes().length === 0) {
      return 'No slots unlocked';
    }
    if (this.essenceState.draftSlots().every(Boolean)) {
      return 'No empty slots';
    }
    if (this.nextEquipSlot(essence) === null) {
      return 'Creature already equipped';
    }

    return 'Equip to slot';
  }

  public draftSlotEssence(slotIndex: number): PlayerEssenceDto | null {
    const essenceId = this.essenceState.draftSlots()[slotIndex];
    if (!essenceId) return null;

    return (
      this.essenceState
        .essenceOptions()
        .find((essence) => essence.id === essenceId) ?? null
    );
  }

  public clearDraftSlot(slotIndex: number): void {
    if (this.essenceState.savingLoadout()) return;

    this.essenceState.setDraftSlot(slotIndex, null);
    this.essenceState.saveDraftSlots();
  }

  public equippedDraftSlot(essence: PlayerEssenceDto): number | null {
    const slotIndex = this.essenceState.draftSlots().indexOf(essence.id);
    return slotIndex >= 0 ? slotIndex : null;
  }

  private inventoryQuantity(itemId: string): number {
    return this.inventoryState
      .items()
      .filter((item) => item.itemInstance.itemBase.id === itemId)
      .reduce((total, item) => total + item.quantity, 0);
  }

  private cleanRequirement(requirement: string): string {
    return requirement.trim().replace(/\.$/, '');
  }

  public selectedAttunementLabel(essence: PlayerEssenceDto): string {
    return essence.attunedSlot === null || essence.attunedSlot === undefined
      ? 'Inactive'
      : `Slot ${essence.attunedSlot + 1}`;
  }

  public eligibilityClass(canPerform: boolean): string {
    return canPerform ? 'll-badge-accent' : 'll-badge-muted';
  }

  public draftSlotsFilled(): number {
    return this.essenceState.draftSlots().filter(Boolean).length;
  }

  public loadoutSaveHint(): string {
    if (!this.essenceState.loadouts()) return 'Loading loadout slots.';
    if (this.essenceState.savingLoadout()) return 'Saving loadout changes...';
    if (this.essenceState.canSaveDraft()) return '';
    if (!this.essenceState.draftLoadoutName().trim()) return 'Name required.';
    if (
      !this.essenceState.selectedLoadoutId() &&
      (this.essenceState.loadouts()?.loadouts?.length ?? 0) >=
        (this.essenceState.loadouts()?.limit ?? 0)
    ) {
      return 'Loadout limit reached.';
    }
    return 'Essence changes save automatically.';
  }

  public trackEssence(_: number, essence: PlayerEssenceDto): string {
    return essence.id;
  }

  public trackCreature(_: number, creature: CreatureArchiveEntryDto): string {
    return creature.creatureId;
  }

  public setEssenceFocus(creature: CreatureArchiveEntryDto): void {
    if (creature.essences.length === 0) return;
    if (creature.isEssenceFocus || !this.essenceState.canChangeEssenceFocus()) {
      return;
    }

    this.essenceState.setEssenceFocus(creature.creatureId);
  }

  public canSetEssenceFocus(creature: CreatureArchiveEntryDto): boolean {
    return (
      creature.essences.length > 0 &&
      !creature.isEssenceFocus &&
      this.essenceState.canChangeEssenceFocus()
    );
  }

  public essenceFocusStatusText(): string {
    const archive = this.essenceState.creatureArchive();
    if (!archive) return 'Loading focus status.';
    if (this.essenceState.canChangeEssenceFocus()) {
      return 'You can choose a new target now. After setting one, Focus is locked for 8 hours.';
    }
    if (archive.essenceFocusAvailableAtUtc) {
      return `New target available ${new Date(archive.essenceFocusAvailableAtUtc).toLocaleString()}.`;
    }

    return 'Focus is locked for 8 hours after choosing a target.';
  }

  public totalFocusDurationLabel(creature: CreatureArchiveEntryDto): string {
    return this.formatDuration(this.getLiveTotalFocusDurationSeconds(creature));
  }

  public currentFocusDurationLabel(creature: CreatureArchiveEntryDto): string {
    return this.formatDuration(
      this.getLiveCurrentFocusDurationSeconds(creature),
    );
  }

  public trackCodex(_: number, entry: EssenceCodexEntryDto): string {
    return entry.id;
  }

  public trackCodexMember(
    index: number,
    member: EssenceCodexMemberDto,
  ): string {
    return member.essenceDefinitionId ?? `undiscovered-${index}`;
  }

  public progressPercent(current: number, required: number): number {
    if (required <= 0) return 100;
    return Math.min(100, Math.round((current / required) * 100));
  }

  public bonusValueLabel(entry: EssenceCodexEntryDto): string {
    const percent = entry.bonusValue / 100;
    return `${percent.toLocaleString(undefined, {
      maximumFractionDigits: 2,
    })}%`;
  }

  public tagLabel(tag: string): string {
    const displayPart = tag.split('.').at(-1) ?? tag;
    return this.formatDisplayLabel(displayPart);
  }

  private getLiveTotalFocusDurationSeconds(
    creature: CreatureArchiveEntryDto,
  ): number {
    const currentAtLoad = creature.currentEssenceFocusDurationSeconds ?? 0;
    const completed = Math.max(
      0,
      (creature.essenceFocusTotalDurationSeconds ?? 0) - currentAtLoad,
    );

    return completed + this.getLiveCurrentFocusDurationSeconds(creature);
  }

  private getLiveCurrentFocusDurationSeconds(
    creature: CreatureArchiveEntryDto,
  ): number {
    if (!creature.isEssenceFocus) return 0;

    const startedAt = creature.essenceFocusSetAtUtc
      ? new Date(creature.essenceFocusSetAtUtc).getTime()
      : Number.NaN;
    if (Number.isNaN(startedAt)) {
      return creature.currentEssenceFocusDurationSeconds ?? 0;
    }

    return Math.max(
      0,
      Math.floor((this.essenceState.currentTime() - startedAt) / 1000),
    );
  }

  private formatDuration(totalSeconds: number): string {
    const seconds = Math.max(0, Math.floor(totalSeconds));
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);

    if (days > 0) return `${days}d ${hours}h`;
    if (hours > 0) return `${hours}h ${minutes}m`;
    if (minutes > 0) return `${minutes}m`;
    return `${seconds}s`;
  }

  private formatDisplayLabel(value: string | null | undefined): string {
    if (!value) return '';

    return value
      .replace(/[_-]+/g, ' ')
      .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
      .replace(/([A-Z]+)([A-Z][a-z])/g, '$1 $2')
      .trim();
  }
}
