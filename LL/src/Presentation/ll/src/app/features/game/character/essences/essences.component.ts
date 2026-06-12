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
  ],
  templateUrl: './essences.component.html',
})
export class EssencesComponent implements OnInit {
  readonly archiveSearch = signal('');
  readonly archiveFilter = signal<ArchiveFilter>('all');
  readonly archiveSort = signal<ArchiveSort>('name');

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

  public trackEssence(_: number, essence: PlayerEssenceDto): string {
    return essence.id;
  }
}
