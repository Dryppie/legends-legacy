import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { catchError, EMPTY, finalize, skip } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { AttributeDto } from '../../../../shared/models/Dtos/attributesDto';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { AttributeTypeFormatPipe } from '../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { ActivatedRoute, Router } from '@angular/router';
import { toDisplayedCombatRating } from '../../../../shared/models/combat-rating-display';
import { AttributeTooltipDirective } from '../../../../shared/directives/attribute-tooltip/attribute-tooltip.directive';
import { ProfessionsService } from '../../../../core/services/api/professions/professions.service';
import { ProfessionType } from '../../../../shared/models/Dtos/characterProfession';
import { EssencePreviewComponent } from '../../../../shared/components/essences/essence-preview/essence-preview.component';
import { PresenceIndicatorComponent } from '../../../../shared/components/character/presence-indicator/presence-indicator.component';
import { EssenceLoadoutDto } from '../../../../shared/models/essence-system';

export function estimateEssenceThreatPerSecond(
  loadout: EssenceLoadoutDto | null | undefined,
): number {
  return (
    loadout?.slots.reduce((total, slot) => {
      const definition = slot.definition;
      if (!definition) return total;

      return (
        total +
        (definition.activeAbility.estimatedThreatPerSecond ?? 0) +
        (definition.passiveAbility.estimatedThreatPerSecond ?? 0)
      );
    }, 0) ?? 0
  );
}

@Component({
  selector: 'app-character-overview',
  imports: [
    DefaultHeaderComponent,
    NgIf,
    NgFor,
    FormsModule,
    RegularButtonComponent,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
    AttributeTooltipDirective,
    DecimalPipe,
    EssencePreviewComponent,
    PresenceIndicatorComponent,
  ],
  templateUrl: './character-overview.component.html',
  styleUrl: './character-overview.component.scss',
})
export class CharacterOverviewComponent {
  readonly AttributeType = AttributeType;
  readonly displayCombatRating = toDisplayedCombatRating;
  searchValue = signal('');
  private readonly searchedCharacter = signal<CharacterOverviewDto | null>(
    null,
  );
  private readonly searchLoading = signal(false);
  private readonly searchErrorMessage = signal('');
  readonly character = computed(() =>
    this.isViewingSearchResult()
      ? this.searchedCharacter()
      : this.currentCharacterOverview(),
  );
  readonly estimatedEssenceThreatPerSecond = computed(() =>
    estimateEssenceThreatPerSecond(this.character()?.activeEssenceLoadout),
  );
  private readonly currentCharacterOverview = computed(() => {
    const overview = this.characterState.overview();
    const currentCharacter = this.characterState.currentCharacter();
    if (!overview || !currentCharacter || overview.id !== currentCharacter.id) {
      return overview;
    }

    const craftingProfession = this.professionsService.getProfession(
      ProfessionType.Crafting,
    );

    return {
      ...overview,
      level: currentCharacter.level,
      experience: currentCharacter.experience,
      experienceUntilNextLevel: currentCharacter.experienceUntilNextLevel,
      ...(craftingProfession
        ? {
            craftingLevel: craftingProfession.level,
            craftingExperience: craftingProfession.experience,
            craftingExperienceUntilNextLevel:
              craftingProfession.experienceUntilNextLevel,
          }
        : {}),
    };
  });
  readonly isLoading = computed(
    () =>
      this.searchLoading() ||
      (!this.isViewingSearchResult() && this.characterState.loading()),
  );
  readonly errorMessage = computed(
    () =>
      this.searchErrorMessage() ||
      (!this.isViewingSearchResult()
        ? (this.characterState.error() ?? '')
        : ''),
  );
  viewedCharacterName = signal('');
  isViewingSearchResult = signal(false);
  readonly profileLabel = computed(() => {
    const equippedTitle = this.character()?.equippedTitle?.displayName;
    if (equippedTitle) {
      return equippedTitle;
    }

    if (this.isViewingSearchResult()) {
      return this.viewedCharacterName();
    }

    return this.characterService.currentCharacter()?.name;
  });
  readonly attributeSections: { title: string; attributes: AttributeType[] }[] =
    [
      {
        title: 'Offense',
        attributes: [
          AttributeType.Power,
          AttributeType.AttackSpeed,
          AttributeType.CritChance,
          AttributeType.CritDamage,
          AttributeType.ArmorPenetration,
          AttributeType.MagicPenetration,
        ],
      },
      {
        title: 'Defense',
        attributes: [
          AttributeType.MaxHealth,
          AttributeType.Armor,
          AttributeType.Resistance,
          AttributeType.DodgeChance,
          AttributeType.BlockChance,
          AttributeType.DamageReduction,
        ],
      },
      {
        title: 'Recovery',
        attributes: [
          AttributeType.HealingPowerPercent,
          AttributeType.HealthRegeneration,
          AttributeType.LifeSteal,
        ],
      },
      {
        title: 'Utility',
        attributes: [
          AttributeType.Cooldown,
          AttributeType.StatusResistance,
          AttributeType.CrowdControlResistance,
          AttributeType.Threat,
        ],
      },
    ];

  constructor(
    private characterService: CharacterService,
    private readonly characterState: CharacterStateService,
    private readonly professionsService: ProfessionsService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {
    const initialCharacterName = this.route.snapshot.queryParamMap
      .get('characterName')
      ?.trim();

    if (initialCharacterName) {
      this.searchValue.set(initialCharacterName);
      this.searchCharacter(initialCharacterName);
    } else {
      this.showCurrentCharacter();
    }

    this.route.queryParamMap.pipe(skip(1)).subscribe((params) => {
      const characterName = params.get('characterName')?.trim();
      if (!characterName) {
        this.showCurrentCharacter();
        return;
      }

      this.searchValue.set(characterName);
      this.searchCharacter(characterName);
    });
  }

  onSearch() {
    const trimmed = this.searchValue().trim();
    if (!trimmed) {
      this.navigateToCharacter(null);
      return;
    }

    this.navigateToCharacter(trimmed);
  }

  refresh(): void {
    if (this.isViewingSearchResult()) {
      const characterName = this.viewedCharacterName().trim();
      if (characterName) {
        this.searchCharacter(characterName);
        return;
      }
    }

    this.characterState.refresh();
  }

  private navigateToCharacter(characterName: string | null): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { characterName },
      queryParamsHandling: 'merge',
    });
  }

  private searchCharacter(characterName: string): void {
    this.searchLoading.set(true);
    this.searchErrorMessage.set('');

    this.characterService
      .searchCharacter(characterName)
      .pipe(
        catchError((err) => {
          this.searchErrorMessage.set(err.message);
          return EMPTY;
        }),
        finalize(() => this.searchLoading.set(false)),
      )
      .subscribe((character) => {
        this.searchedCharacter.set(character);
        this.viewedCharacterName.set(characterName);
        this.isViewingSearchResult.set(true);
      });
  }

  private showCurrentCharacter(): void {
    this.characterState.refreshIfDirty();
    this.searchErrorMessage.set('');
    this.searchedCharacter.set(null);
    this.viewedCharacterName.set(
      this.characterService.currentCharacter()?.name ?? '',
    );
    this.isViewingSearchResult.set(false);
  }

  onEnter(event: KeyboardEvent) {
    if (event.key === 'Enter') {
      this.onSearch();
    }
  }

  getAttribute(type: AttributeType): AttributeDto {
    const current = this.character();
    return (
      current?.baseCombatAttributes.find(
        (attr) => attr.attributeType === type,
      ) ??
      current?.baseAttributes.find((attr) => attr.attributeType === type) ?? {
        attributeType: type,
        value: 0,
      }
    );
  }

  getSectionAttributes(attributes: AttributeType[]): AttributeDto[] {
    return attributes.map((type) => this.getAttribute(type));
  }

  getEquipmentRating(type: AttributeType): AttributeDto | null {
    return (
      this.character()?.equipmentRatings?.find(
        (rating) => rating.attributeType === type,
      ) ?? null
    );
  }

  experiencePercent(
    experience: number,
    experienceUntilNextLevel: number,
  ): number {
    if (experienceUntilNextLevel <= 0) return 100;

    return Math.min(
      100,
      Math.max(0, (experience / experienceUntilNextLevel) * 100),
    );
  }

  get filledLoadoutSlots(): number {
    return (
      this.character()?.activeEssenceLoadout?.slots.filter(
        (slot) => !!slot.playerEssenceId,
      ).length ?? 0
    );
  }

  get totalLoadoutSlots(): number {
    return this.character()?.activeEssenceLoadout?.slots.length ?? 0;
  }
}
