import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { Component, computed, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { EssenceStateService } from '../../../../core/services/api/essences/essence-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { EssenceDescriptionComponent } from '../../../../shared/components/essences/essence-description/essence-description.component';
import {
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

type ArchiveFilter = 'all' | 'favorites' | 'attuned' | 'inactive';
type ArchiveSort = 'name' | 'level' | 'tier';

@Component({
  selector: 'app-essences',
  standalone: true,
  imports: [
    CommonModule,
    ScrollingModule,
    FormsModule,
    DefaultHeaderComponent,
    EssenceDescriptionComponent,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    EssencesAbsorbComponent,
    DropdownComponent,
  ],
  templateUrl: './essences.component.html',
})
export class EssencesComponent implements OnInit {
  readonly archiveSearch = signal('');
  readonly archiveFilter = signal<ArchiveFilter>('all');
  readonly archiveSort = signal<ArchiveSort>('name');
  readonly upgradeDetailsOpen = signal(false);

  readonly archiveFilters: { label: string; value: ArchiveFilter }[] = [
    { label: 'All', value: 'all' },
    { label: 'Favorites', value: 'favorites' },
    { label: 'Attuned', value: 'attuned' },
    { label: 'Inactive', value: 'inactive' },
  ];

  readonly archiveSorts: { label: string; value: ArchiveSort }[] = [
    { label: 'Name', value: 'name' },
    { label: 'Level', value: 'level' },
    { label: 'Tier', value: 'tier' },
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
          filter === 'inactive' &&
          essence.attunedSlot !== null &&
          essence.attunedSlot !== undefined
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

  constructor(public readonly essenceState: EssenceStateService) {}

  public ngOnInit(): void {
    this.essenceState.refresh();
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

  public draftSlotDropdownOptions(
    slotIndex: number,
  ): DropdownOption<string | null>[] {
    const draftSlots = this.essenceState.draftSlots();

    return [
      { label: 'Empty', value: null },
      ...this.essenceState.essenceOptions().map((essence) => ({
        label: essence.name,
        value: essence.id,
        disabled:
          draftSlots.includes(essence.id) &&
          draftSlots[slotIndex] !== essence.id,
      })),
    ];
  }

  public setDraftSlotFromDropdown(
    slotIndex: number,
    selection: DropdownSelection<unknown>,
  ): void {
    this.essenceState.setDraftSlot(slotIndex, selection.main as string | null);
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
    if (this.essenceState.canSaveDraft()) return '';
    if (!this.essenceState.draftLoadoutName().trim()) return 'Name required.';
    if (this.essenceState.hasDuplicateDraftEssences()) {
      return 'Each Essence can only be assigned once.';
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
}
