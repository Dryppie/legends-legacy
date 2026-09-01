import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { DecimalPipe, NgFor, NgIf } from '@angular/common';
import { Component, computed, OnDestroy, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  catchError,
  distinctUntilChanged,
  EMPTY,
  finalize,
  map,
  of,
  skip,
  Subject,
  switchMap,
  takeUntil,
  timer,
} from 'rxjs';
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
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { buildPlayerJourneyGuidance } from '../../../../core/services/client-side/player-journey/player-journey';

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
    OverlayModule,
  ],
  templateUrl: './character-overview.component.html',
  styleUrl: './character-overview.component.scss',
})
export class CharacterOverviewComponent implements OnDestroy {
  readonly AttributeType = AttributeType;
  readonly displayCombatRating = toDisplayedCombatRating;
  searchValue = signal('');
  readonly characterSuggestions = signal<string[]>([]);
  readonly isSearchingCharacters = signal(false);
  readonly characterSuggestionSearchCompleted = signal(false);
  readonly characterSuggestionPanelOpen = signal(false);
  readonly activeCharacterSuggestion = signal(-1);
  readonly characterSuggestionPositions: ConnectedPosition[] = [
    {
      originX: 'start',
      originY: 'bottom',
      overlayX: 'start',
      overlayY: 'top',
      offsetY: 4,
    },
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -4,
    },
  ];
  private readonly characterSuggestionSearch = new Subject<string>();
  private readonly destroy = new Subject<void>();
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
    estimateEssenceThreatPerSecond(this.character()?.essenceLoadout),
  );
  readonly journeyGuidance = computed(() =>
    buildPlayerJourneyGuidance(
      this.questState.journal(),
      this.characterState.currentCharacter()?.level ?? 1,
    ),
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
    const gatheringProfessions = (overview.gatheringProfessions ?? []).map(
      (profession) => {
        const liveProfession = this.professionsService.getProfession(
          profession.professionType,
        );
        return liveProfession ?? profession;
      },
    );

    return {
      ...overview,
      level: currentCharacter.level,
      experience: currentCharacter.experience,
      experienceUntilNextLevel: currentCharacter.experienceUntilNextLevel,
      gatheringProfessions,
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
    private readonly questState: QuestStateService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
  ) {
    this.characterSuggestionSearch
      .pipe(
        map((prefix) => prefix.trim()),
        distinctUntilChanged(),
        switchMap((prefix) => {
          if (prefix.length < 2) {
            this.isSearchingCharacters.set(false);
            return of([] as string[]);
          }

          this.isSearchingCharacters.set(true);
          return timer(200).pipe(
            switchMap(() =>
              this.characterService
                .suggestCharacterNames(prefix)
                .pipe(catchError(() => of([] as string[]))),
            ),
          );
        }),
        takeUntil(this.destroy),
      )
      .subscribe((suggestions) => {
        this.isSearchingCharacters.set(false);
        this.characterSuggestionSearchCompleted.set(true);
        this.characterSuggestions.set(suggestions);
        this.activeCharacterSuggestion.set(suggestions.length ? 0 : -1);
      });

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

  ngOnDestroy(): void {
    this.destroy.next();
    this.destroy.complete();
  }

  onSearchValueChange(value: string): void {
    this.searchValue.set(value);
    this.searchErrorMessage.set('');
    this.characterSuggestionSearchCompleted.set(false);
    this.activeCharacterSuggestion.set(-1);

    if (value.trim().length < 2) {
      this.characterSuggestions.set([]);
      this.isSearchingCharacters.set(false);
      this.characterSuggestionPanelOpen.set(false);
    } else {
      this.characterSuggestionPanelOpen.set(true);
    }

    this.characterSuggestionSearch.next(value);
  }

  openCharacterSuggestions(): void {
    if (this.searchValue().trim().length >= 2) {
      this.characterSuggestionPanelOpen.set(true);
    }
  }

  closeCharacterSuggestions(): void {
    this.characterSuggestionPanelOpen.set(false);
    this.activeCharacterSuggestion.set(-1);
  }

  selectCharacterSuggestion(event: Event, name: string): void {
    event.preventDefault();
    this.searchValue.set(name);
    this.characterSuggestions.set([]);
    this.characterSuggestionSearchCompleted.set(false);
    this.closeCharacterSuggestions();
  }

  handleSearchKeydown(event: KeyboardEvent): void {
    const suggestions = this.characterSuggestions();
    if (event.key === 'Escape') {
      this.closeCharacterSuggestions();
      return;
    }

    if (this.characterSuggestionPanelOpen() && suggestions.length) {
      if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
        event.preventDefault();
        const direction = event.key === 'ArrowDown' ? 1 : -1;
        const nextIndex =
          (this.activeCharacterSuggestion() + direction + suggestions.length) %
          suggestions.length;
        this.activeCharacterSuggestion.set(nextIndex);
        return;
      }

      if (event.key === 'Enter' && this.activeCharacterSuggestion() >= 0) {
        event.preventDefault();
        this.selectCharacterSuggestion(
          event,
          suggestions[this.activeCharacterSuggestion()],
        );
        return;
      }
    }

    if (event.key === 'Enter') {
      this.onSearch();
    }
  }

  showCharacterSuggestionPanel(): boolean {
    return (
      this.characterSuggestionPanelOpen() &&
      this.searchValue().trim().length >= 2
    );
  }

  onSearch() {
    this.closeCharacterSuggestions();
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

  navigateToJourney(route: string): void {
    void this.router.navigateByUrl(route);
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
      this.character()?.essenceLoadout?.slots.filter(
        (slot) => !!slot.playerEssenceId,
      ).length ?? 0
    );
  }

  get totalLoadoutSlots(): number {
    return this.character()?.essenceLoadout?.slots.length ?? 0;
  }
}
