import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Component, computed, effect, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
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
import {
  CreatureArchiveEntryDto,
  EssenceCodexEntryDto,
  EssenceCodexMemberDto,
  EssenceLoadoutDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';
import { AttributeTypeFormatPipe } from '../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { EssencesAbsorbComponent } from './essences-absorb/essences-absorb.component';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../../shared/components/custom-components/dropdown/dropdown.component';
import { TutorialStateService } from '../../../../core/services/api/tutorial/tutorial-state.service';
import {
  TUTORIAL_STEP_ABSORB_ESSENCE,
  TUTORIAL_STEP_EQUIP_ESSENCE,
} from '../../../../shared/models/tutorial';

type ArchiveFilter = 'all' | 'favorites' | 'attuned' | 'ready';
type ArchiveSort = 'name' | 'level' | 'tier';
type CreatureSourceFilter = 'all' | 'Area' | 'Dungeon';
type CreatureEssenceFilter = 'all' | 'found' | 'not-found';

@Component({
  selector: 'app-essences',
  standalone: true,
  imports: [
    CommonModule,
    ScrollingModule,
    FormsModule,
    DefaultHeaderComponent,
    EssenceDescriptionComponent,
    NavigationTabsComponent,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    EssencesAbsorbComponent,
    DropdownComponent,
  ],
  templateUrl: './essences.component.html',
  styleUrls: ['./essences.component.scss'],
})
export class EssencesComponent implements OnInit {
  readonly archiveSearch = signal('');
  readonly creatureSearch = signal('');
  readonly creatureRegionFilter = signal('all');
  readonly creatureSourceFilter = signal<CreatureSourceFilter>('all');
  readonly creatureEssenceFilter = signal<CreatureEssenceFilter>('all');
  readonly archiveFilter = signal<ArchiveFilter>('all');
  readonly archiveSort = signal<ArchiveSort>('name');
  readonly upgradeDetailsOpen = signal(false);
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

  readonly filteredArchiveEssences = computed(() => {
    const search = this.archiveSearch().trim().toLowerCase();
    const filter = this.archiveFilter();
    const sort = this.archiveSort();
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
        if (
          filter === 'ready' &&
          !essence.canUpgradePotential &&
          !essence.canAscend &&
          !essence.canEvolve
        ) {
          return false;
        }

        if (!search) return true;

        const searchable = [
          essence.name,
          essence.activeAbility.name,
          essence.passiveAbility.name,
          ...essence.currentAttributeBonuses.map((bonus) => bonus.attribute),
        ]
          .join(' ')
          .toLowerCase();

        return searchable.includes(search);
      })
      .sort((a, b) => {
        switch (sort) {
          case 'level':
            return b.level - a.level || a.name.localeCompare(b.name);
          case 'tier':
            return (
              b.potentialTier - a.potentialTier ||
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
    const essenceFilter = this.creatureEssenceFilter();
    const creatures = this.essenceState.creatureArchive()?.creatures ?? [];

    return creatures.filter((creature) => {
      const matchesLocation = creature.locations.some(
        (location) =>
          (region === 'all' || location.regionId.toString() === region) &&
          (source === 'all' || location.sourceType === source),
      );
      if ((region !== 'all' || source !== 'all') && !matchesLocation) {
        return false;
      }

      const hasFoundEssence = creature.essences.some(
        (essence) => essence.isAbsorbed,
      );
      if (essenceFilter === 'found' && !hasFoundEssence) return false;
      if (essenceFilter === 'not-found' && hasFoundEssence) return false;

      if (!search) return true;

      return [
        creature.name,
        creature.creatureId,
        ...creature.essences.map((essence) => essence.name),
        ...creature.locations.flatMap((location) => [
          location.regionName,
          location.sourceType,
          location.sourceName,
        ]),
        ...creature.tags,
      ]
        .join(' ')
        .toLowerCase()
        .includes(search);
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

  readonly unlockedCodexEntries = computed(
    () =>
      this.essenceState.codex()?.entries.filter((entry) => entry.isUnlocked)
        .length ?? 0,
  );

  constructor(
    public readonly essenceState: EssenceStateService,
    private readonly tutorialState: TutorialStateService,
  ) {
    effect(
      () => {
        const tutorial = this.tutorialState.state();
        if (!tutorial || tutorial.isCompleted) return;

        if (tutorial.currentStep === TUTORIAL_STEP_ABSORB_ESSENCE) {
          this.essenceState.setActiveView('absorb');
          return;
        }

        if (tutorial.currentStep === TUTORIAL_STEP_EQUIP_ESSENCE) {
          this.essenceState.setActiveView('archive');
        }
      },
      { allowSignalWrites: true },
    );
  }

  public ngOnInit(): void {
    this.essenceState.refresh();
  }

  public selectView(view: string): void {
    switch (view) {
      case 'archive':
      case 'absorb':
      case 'creatures':
      case 'codex':
        this.essenceState.setActiveView(view as EssenceView);
    }
  }

  public setCreatureRegionFilter(selection: DropdownSelection<string>): void {
    this.creatureRegionFilter.set(selection.main);
  }

  public setCreatureSourceFilter(
    selection: DropdownSelection<CreatureSourceFilter>,
  ): void {
    this.creatureSourceFilter.set(selection.main);
  }

  public setCreatureEssenceFilter(
    selection: DropdownSelection<CreatureEssenceFilter>,
  ): void {
    this.creatureEssenceFilter.set(selection.main);
  }

  public selectPlayerEssence(essence: PlayerEssenceDto): void {
    this.essenceState.selectPlayerEssence(essence);
  }

  public favorite(essence: PlayerEssenceDto): void {
    this.essenceState.favorite(essence);
  }

  public spendDust(essence: PlayerEssenceDto): void {
    this.essenceState.spendDust(essence);
  }

  public ascend(essence: PlayerEssenceDto): void {
    this.essenceState.ascend(essence);
  }

  public upgradePotential(essence: PlayerEssenceDto): void {
    this.essenceState.upgradePotential(essence);
  }

  public evolve(essence: PlayerEssenceDto): void {
    this.essenceState.evolve(essence);
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
    const equippedSlot = this.equippedDraftSlot(essence);
    if (equippedSlot !== null) {
      this.essenceState.setDraftSlot(equippedSlot, null);
      return;
    }

    const slotIndex = this.nextEquipSlot(essence);
    if (slotIndex === null) return;

    this.essenceState.setDraftSlot(slotIndex, essence.id);
  }

  public canToggleEssenceSlot(essence: PlayerEssenceDto): boolean {
    return (
      this.equippedDraftSlot(essence) !== null ||
      this.nextEquipSlot(essence) !== null
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
    this.essenceState.setDraftSlot(slotIndex, null);
  }

  public equippedDraftSlot(essence: PlayerEssenceDto): number | null {
    const slotIndex = this.essenceState.draftSlots().indexOf(essence.id);
    return slotIndex >= 0 ? slotIndex : null;
  }

  public toggleUpgradeDetails(): void {
    this.upgradeDetailsOpen.update((open) => !open);
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
    if (this.essenceState.canSaveDraft()) return '';
    if (!this.essenceState.draftLoadoutName().trim()) return 'Name required.';
    if (this.essenceState.hasDuplicateDraftEssences()) {
      return 'Each Essence can only be assigned once.';
    }
    if (this.essenceState.hasDuplicateDraftCreatureSources()) {
      return 'Only one Essence from each creature can be active.';
    }
    if (
      !this.essenceState.selectedLoadoutId() &&
      (this.essenceState.loadouts()?.loadouts?.length ?? 0) >=
        (this.essenceState.loadouts()?.limit ?? 0)
    ) {
      return 'Loadout limit reached.';
    }
    return 'Select at least one valid change.';
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
