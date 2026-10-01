import { DatePipe } from '@angular/common';
import {
  Component,
  DestroyRef,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { takeUntilDestroyed, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  EMPTY,
  catchError,
  debounceTime,
  distinctUntilChanged,
  finalize,
  map,
  of,
  skip,
  switchMap,
} from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import {
  buildPlayerJourneyGuidance,
  playerJourneyStageLabels,
} from '../../../../core/services/client-side/player-journey/player-journey';
import { lastSeenLabel } from '../../../../shared/components/character/presence-indicator/last-seen';
import { injectNobilityAppearance } from '../../../../shared/components/character/nobility-appearance';
import { EssencePreviewComponent } from '../../../../shared/components/essences/essence-preview/essence-preview.component';
import {
  LG_GRIMOIRE,
  LG_RARITY_CODES,
  LgLedgerRow,
  LgRarity,
  lgFormatDuration,
  lgFormatNumber,
  lgFormatUnit,
  LG_SHELL,
} from '../../../../shared/components/grimoire';
import { CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { AttributeDto } from '../../../../shared/models/Dtos/attributesDto';
import { toDisplayedCombatRating } from '../../../../shared/models/combat-rating-display';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import { EssenceLoadoutSlotDto } from '../../../../shared/models/essence-system';
import {
  formatAttributeTooltip,
  formatAttributeType,
} from '../../../../shared/pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { formatAttributeValue } from '../../../../shared/pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import { estimateEssenceThreatPerSecond } from '../character-overview/character-overview.component';

/** The four attribute groups, in the order the game has always shown them. */
export const OVERVIEW_ATTRIBUTE_GROUPS: readonly { title: string; attributes: readonly AttributeType[] }[] = [
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
      AttributeType.Restoration,
      AttributeType.HealingPowerPercent,
      AttributeType.HealthRegeneration,
      AttributeType.LifeSteal,
    ],
  },
  {
    title: 'Utility',
    attributes: [
      AttributeType.AbilityHaste,
      AttributeType.Tenacity,
      AttributeType.Cooldown,
      AttributeType.StatusResistance,
      AttributeType.CrowdControlResistance,
      AttributeType.Threat,
    ],
  },
];

export const NOBILITY_PERK_GROUPS: readonly { label: string; perks: readonly string[] }[] = [
  {
    label: 'Capacity',
    perks: [
      '+3 Equipment and Essence loadouts',
      '+3 Arena ticket capacity',
      '30 sell listings and 30 buy orders instead of 10 each',
    ],
  },
  {
    label: 'Time & cost',
    perks: [
      '7 days of offline combat retention',
      '2-hour Creature Focus cooldown instead of 8',
      '+1 free Prophecy reroll (costs: 0, 0, 40, 80 Fate Echo)',
      'Noble badge',
    ],
  },
];

/** The game's formatting of an attribute value, with its integer part grouped ("4,150", "38%", "1.12"). */
export function overviewAttributeText(
  value: number | null | undefined,
  type: string,
  equipmentRating = false,
): string {
  return formatAttributeValue(value, type, false, equipmentRating).replace(/\d+/, (digits) =>
    lgFormatNumber(Number(digits)),
  );
}

export interface OverviewLoadoutSlot {
  index: number;
  state: 'attuned' | 'open';
  name?: string;
  rarity?: LgRarity;
  active?: { name: string; cooldown?: string };
  passive?: { name: string };
  slot: EssenceLoadoutSlotDto;
}

/**
 * The Character Overview on the Grimoire design system (ScreenOverview, ArchetypeInformation). It shows what the
 * current Overview shows, from the same services, and replaces it once Martin signs it off; until then Settings →
 * Interface switches between them. It sits in the game's own frame, so its Page takes `flow` (D-097).
 */
@Component({
  selector: 'app-character-overview-grimoire',
  imports: [...LG_GRIMOIRE, RouterLink, DatePipe, EssencePreviewComponent],
  templateUrl: './character-overview-grimoire.component.html',
  styleUrl: './character-overview-grimoire.component.scss',
  host: { class: 'lg-root', '[class.in-shell]': 'inShell' },
})
export class CharacterOverviewGrimoireComponent {
  /** In the new look's GameShell the Page is the stage's own; in the old frame it flows inside it (D-097). */
  protected readonly inShell = !!inject(LG_SHELL, { optional: true });
  private readonly characterService = inject(CharacterService);
  private readonly characterState = inject(CharacterStateService);
  private readonly questState = inject(QuestStateService);
  readonly combatStyles = inject(CombatStyleStateService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly perkGroups = NOBILITY_PERK_GROUPS;
  protected readonly fmt = lgFormatNumber;
  protected readonly cornerSrc = 'assets/grimoire/corner-ornament.svg';
  protected readonly bannerArt = 'assets/backgrounds/optimized/background.webp';

  // Search (PatternPlayerLookup).
  readonly query = signal('');
  readonly suggestions = signal<string[]>([]);
  readonly suggesting = signal(false);
  readonly suggestionsSearched = signal(false);

  // The profile shown: your own, or another player's from ?characterName=.
  readonly viewing = signal<string | null>(null);
  private readonly searched = signal<CharacterOverviewDto | null>(null);
  readonly searchLoading = signal(false);
  private readonly searchError = signal('');
  readonly showPerks = signal(false);

  private readonly ownOverview = computed(() => {
    const overview = this.characterState.overview();
    const current = this.characterState.currentCharacter();
    if (!overview || !current || overview.id !== current.id) return overview;
    return {
      ...overview,
      level: current.level,
      experience: current.experience,
      experienceUntilNextLevel: current.experienceUntilNextLevel,
    };
  });
  readonly character = computed(() => (this.viewing() ? this.searched() : this.ownOverview()));
  readonly isOwnProfile = computed(() => {
    const id = this.characterService.currentCharacter()?.id;
    return !!id && this.character()?.id === id;
  });
  readonly loading = computed(
    () => this.searchLoading() || (!this.viewing() && this.characterState.loading()),
  );
  readonly error = computed(
    () => this.searchError() || (!this.viewing() ? (this.characterState.error() ?? '') : ''),
  );

  private readonly nobility = injectNobilityAppearance(() => this.character()?.id);
  /** Your own Nobility shows while it lasts; another player's only when they show the badge. */
  readonly membership = computed(() =>
    this.isOwnProfile() ? this.nobility.active() : this.nobility.visible(),
  );

  readonly profileName = computed(() => {
    const c = this.character();
    return c?.equippedTitle?.displayName || c?.name || this.viewing() || '';
  });
  readonly presence = computed(() => {
    const c = this.character();
    if (!c || !this.viewing()) return null;
    return { online: c.isOnline, lastSeen: lastSeenLabel(c.lastSeenAt) };
  });
  readonly combatRating = computed(() => {
    const power = this.character()?.power;
    return power?.state === 'Available' ? lgFormatNumber(Math.round(toDisplayedCombatRating(power.overall))) : null;
  });

  readonly journey = computed(() =>
    this.viewing()
      ? null
      : buildPlayerJourneyGuidance(
          this.questState.journal(),
          this.characterState.currentCharacter()?.level ?? 1,
        ),
  );
  readonly journeyStages = computed(() => playerJourneyStageLabels(this.journey()));

  readonly combatStyle = computed(() => {
    const data = this.combatStyles.data();
    const entry = data?.styles.find((e) => e.definition.id === data.selection.combatStyleId) ?? null;
    if (!entry) return null;
    const refinementId = data?.selection.refinementId ?? null;
    const refinement = refinementId
      ? (entry.definition.refinements.find((r) => r.id === refinementId)?.name ?? refinementId)
      : 'Base form';
    return {
      name: entry.definition.name,
      level: entry.level,
      mastered: entry.level >= 10,
      xp: entry.currentXp,
      xpRequired: entry.xpRequired,
      refinement,
    };
  });

  readonly loadout = computed<OverviewLoadoutSlot[]>(() =>
    (this.character()?.essenceLoadout?.slots ?? []).map((slot) => {
      const d = slot.definition;
      if (!slot.playerEssenceId) return { index: slot.slotIndex, state: 'open', slot };
      return {
        index: slot.slotIndex,
        state: 'attuned',
        name: slot.essenceName ?? d?.name ?? undefined,
        rarity: d && d.rarity in LG_RARITY_CODES ? (d.rarity as LgRarity) : undefined,
        active: d ? { name: d.activeAbility.name, cooldown: lgFormatDuration(d.activeAbility.cooldownSeconds) } : undefined,
        passive: d ? { name: d.passiveAbility.name } : undefined,
        slot,
      };
    }),
  );
  readonly attuned = computed(() => this.loadout().filter((s) => s.state === 'attuned').length);

  readonly attributeGroups = computed(() => {
    const c = this.character();
    if (!c) return [];
    // Projected keys identify the active rules; raw bases can still hold retired aliases.
    const available = c.baseCombatAttributes.length ? c.baseCombatAttributes : c.baseAttributes;
    const byType = new Map(available.map((a) => [a.attributeType, a]));
    const ratings = new Map((c.equipmentRatings ?? []).map((r) => [r.attributeType, r]));
    const threat = estimateEssenceThreatPerSecond(c.essenceLoadout);
    return OVERVIEW_ATTRIBUTE_GROUPS.map((group) => ({
      title: group.title,
      rows: group.attributes.flatMap((type): LgLedgerRow[] => {
        const attribute: AttributeDto | undefined = byType.get(type);
        if (!attribute) return [];
        const rating = ratings.get(type);
        const ratingText = rating
          ? `${overviewAttributeText(rating.value, rating.attributeType, true)} ${formatAttributeType(rating.attributeType, true)}`
          : undefined;
        return [
          {
            id: type,
            label: formatAttributeType(type),
            value:
              type === AttributeType.Threat
                ? lgFormatUnit(threat, 'threat/s', 1)
                : overviewAttributeText(attribute.value, type),
            sub: ratingText,
            description: formatAttributeTooltip(type),
            tipMeta: ratingText ? `From equipment: ${ratingText}` : undefined,
          },
        ];
      }),
    }));
  });

  constructor() {
    const destroyRef = inject(DestroyRef);

    // Suggestions after two letters, 200ms after the player stops typing.
    toObservable(this.query)
      .pipe(
        map((q) => q.trim()),
        distinctUntilChanged(),
        switchMap((prefix) => {
          this.suggestionsSearched.set(false);
          if (prefix.length < 2) {
            this.suggesting.set(false);
            return of([] as string[]);
          }
          this.suggesting.set(true);
          return of(prefix).pipe(
            debounceTime(200),
            switchMap((p) =>
              this.characterService.suggestCharacterNames(p).pipe(catchError(() => of([] as string[]))),
            ),
          );
        }),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((names) => {
        this.suggesting.set(false);
        this.suggestions.set(names);
        this.suggestionsSearched.set(this.query().trim().length >= 2);
      });

    const initial = this.route.snapshot.queryParamMap.get('characterName')?.trim();
    if (initial) this.load(initial);
    else this.showOwn();
    this.route.queryParamMap.pipe(skip(1), takeUntilDestroyed(destroyRef)).subscribe((params) => {
      const name = params.get('characterName')?.trim();
      if (name) this.load(name);
      else this.showOwn();
    });

    effect(() => {
      if (this.combatStyles.dirty()) untracked(() => this.combatStyles.refreshIfDirty());
    });
  }

  /** The Search button and Enter in the field. An empty search goes back to your own profile. */
  search(value = this.query()): void {
    const name = value.trim();
    this.navigateTo(name || null);
  }

  /** A suggestion picked from the list. */
  pick(name: string): void {
    this.query.set(name);
    this.navigateTo(name);
  }

  backToOwn(): void {
    this.query.set('');
    this.navigateTo(null);
  }

  refresh(): void {
    const name = this.viewing();
    if (name) this.load(name);
    else this.characterState.refresh();
  }

  go(route: string): void {
    void this.router.navigateByUrl(route);
  }

  private navigateTo(characterName: string | null): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { characterName },
      queryParamsHandling: 'merge',
    });
  }

  private load(name: string): void {
    this.query.set(name);
    this.searchLoading.set(true);
    this.searchError.set('');
    this.characterService
      .searchCharacter(name)
      .pipe(
        catchError((err) => {
          this.searchError.set(err?.message ?? 'Couldn’t load that player.');
          return EMPTY;
        }),
        finalize(() => this.searchLoading.set(false)),
      )
      .subscribe((character) => {
        this.showPerks.set(false);
        this.searched.set(character);
        this.viewing.set(name);
      });
  }

  private showOwn(): void {
    this.showPerks.set(false);
    this.characterState.refreshIfDirty();
    this.searchError.set('');
    this.searched.set(null);
    this.viewing.set(null);
  }
}
