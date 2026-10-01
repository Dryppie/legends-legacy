import { signal } from '@angular/core';
import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { CharacterService } from '../../../../core/services/api/character/character.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { EssenceItemViewService } from '../../../../core/services/api/essences/essence-item-view.service';
import { NobilityService } from '../../../../core/services/api/nobility/nobility.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { TimeSyncService } from '../../../../core/services/api/time-sync/time-sync.service';
import { CharacterDto, CharacterOverviewDto } from '../../../../shared/models/Dtos/characterDto';
import { AttributeType } from '../../../../shared/models/enums/attributeType';
import {
  CharacterOverviewGrimoireComponent,
  overviewAttributeText,
} from './character-overview-grimoire.component';

describe('CharacterOverviewGrimoireComponent', () => {
  let fixture: ComponentFixture<CharacterOverviewGrimoireComponent>;
  let characterService: jasmine.SpyObj<CharacterService>;
  let noble: boolean;

  function setup(overview = createOverview()): void {
    characterService = jasmine.createSpyObj<CharacterService>(
      'CharacterService',
      ['suggestCharacterNames', 'searchCharacter'],
      { currentCharacter: signal(createCharacter()).asReadonly() },
    );
    characterService.suggestCharacterNames.and.returnValue(of(['Maren', 'Marek']));
    characterService.searchCharacter.and.returnValue(
      of({ ...createOverview(), id: 'character-2', name: 'Maren', isOnline: false, lastSeenAt: null }),
    );
    TestBed.configureTestingModule({
      imports: [CharacterOverviewGrimoireComponent],
      providers: [
        provideRouter([]),
        { provide: CharacterService, useValue: characterService },
        {
          provide: CharacterStateService,
          useValue: {
            overview: signal(overview).asReadonly(),
            currentCharacter: signal(createCharacter()).asReadonly(),
            loading: signal(false).asReadonly(),
            error: signal(null).asReadonly(),
            refresh: jasmine.createSpy('refresh'),
            refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
          },
        },
        { provide: QuestStateService, useValue: { journal: signal({ quests: [] }).asReadonly() } },
        {
          provide: CombatStyleStateService,
          useValue: {
            data: signal({
              contentVersion: '1',
              styles: [
                {
                  definition: { id: 'reaper', name: 'Reaper', description: '', kind: 'Reaper', refinements: [], upgrades: [] },
                  level: 4,
                  currentXp: 1240,
                  xpRequired: 2000,
                  upgradeSlots: 0,
                  refinementId: null,
                  upgradeIds: [],
                },
              ],
              selection: { combatStyleId: 'reaper', refinementId: null, upgradeIds: [] },
              effectiveStyle: null,
              validationIssue: null,
            }).asReadonly(),
            dirty: signal(false).asReadonly(),
            error: signal(null).asReadonly(),
            refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
          },
        },
        {
          provide: NobilityService,
          useValue: {
            publicAppearance: () =>
              of({
                serverTime: new Date().toISOString(),
                expiresAt: noble ? new Date(Date.now() + 86_400_000).toISOString() : null,
                showBadge: true,
              }),
          },
        },
        { provide: TimeSyncService, useValue: { now: () => Date.now() } },
        { provide: EssenceItemViewService, useValue: {} },
      ],
    });
  }

  function render(): HTMLElement {
    fixture = TestBed.createComponent(CharacterOverviewGrimoireComponent);
    fixture.detectChanges();
    tick(0);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  const text = (el: Element | null) => (el?.textContent ?? '').replace(/\s+/g, ' ').trim();

  beforeEach(() => (noble = false));

  it('shows your own profile: journey, identity, Combat Style and Refresh', fakeAsync(() => {
    setup();
    const el = render();

    expect(text(el.querySelector('.lg-identity__eyebrow'))).toBe('Combat Profile');
    expect(text(el.querySelector('.lg-identity .lg-heading'))).toBe('Hero');
    expect(el.querySelector('.lg-journey')).not.toBeNull();
    expect(text(el.querySelector('.lg-pagehead__actions'))).toContain('Refresh');
    expect(text(el)).toContain('Combat Style');
    expect(text(el)).toContain('Mastery 4');
    expect(el.querySelector('.lg-identity__noble')).toBeNull();
    fixture.destroy();
    tick(60_000);
  }));

  it('marks an active Noble with the crown and offers the perks', fakeAsync(() => {
    noble = true;
    setup();
    const el = render();

    expect(el.querySelector('.lg-identity__noble')).not.toBeNull();
    const toggle = Array.from(el.querySelectorAll('button')).find((b) => text(b) === 'Show perks')!;
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
    toggle.click();
    fixture.detectChanges();
    expect(el.querySelector('#ovw-perks')).not.toBeNull();
    fixture.destroy();
    tick(60_000);
  }));

  it("shows another player's profile from the search, with Back to my profile", fakeAsync(() => {
    setup();
    TestBed.inject(Router).navigateByUrl('/?characterName=Maren');
    tick();
    const el = render();

    expect(characterService.searchCharacter).toHaveBeenCalledWith('Maren');
    expect(text(el.querySelector('.lg-identity__eyebrow'))).toBe('Viewing player');
    expect(text(el.querySelector('.lg-identity .lg-heading'))).toBe('Maren');
    expect(text(el.querySelector('.lg-presence'))).toBe('Last seen unknown');
    expect(el.querySelector('.lg-journey')).toBeNull();
    expect(text(el.querySelector('.lg-pagehead__actions'))).toContain('Back to my profile');
    expect(text(el)).not.toContain('Manage Combat Styles');
    fixture.destroy();
    tick(60_000);
  }));

  it('lists the projected attributes with explanations, equipment ratings and threat per second', fakeAsync(() => {
    const overview = createOverview();
    overview.baseAttributes = [{ attributeType: AttributeType.Cooldown, value: 20 }];
    overview.baseCombatAttributes = [
      { attributeType: AttributeType.MaxHealth, value: 4150 },
      { attributeType: AttributeType.Armor, value: 38 },
      { attributeType: AttributeType.Threat, value: 0 },
    ];
    overview.equipmentRatings = [{ attributeType: AttributeType.Armor, value: 240 }];
    setup(overview);
    render();
    const groups = fixture.componentInstance.attributeGroups();
    const rows = groups.flatMap((g) => g.rows);

    expect(rows.map((r) => r.id)).toEqual([AttributeType.MaxHealth, AttributeType.Armor, AttributeType.Threat]);
    expect(rows[0].value).toBe('4,150');
    expect(rows.every((r) => !!r.description)).toBeTrue();
    expect(rows[1].sub).toBeTruthy();
    expect(rows[1].tipMeta).toContain('From equipment:');
    expect(String(rows[2].value)).toContain('threat/s');
    fixture.destroy();
    tick(60_000);
  }));
});

describe('overviewAttributeText', () => {
  it('groups the integer part and keeps the game’s precision and unit', () => {
    expect(overviewAttributeText(4150, AttributeType.MaxHealth)).toBe('4,150');
    expect(overviewAttributeText(12, AttributeType.Power)).toBe('12');
  });
});

function createCharacter(): CharacterDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 5,
    experience: 10,
    experienceUntilNextLevel: 100,
    cinders: 0,
    soulstones: 0,
    fateEcho: 0,
    guildFavor: 0,
    arenaRating: 0,
  };
}

function createOverview(): CharacterOverviewDto {
  return {
    id: 'character-1',
    name: 'Hero',
    level: 5,
    experience: 5,
    experienceUntilNextLevel: 100,
    totalAchievementPoints: 1250,
    baseAttributes: [],
    baseCombatAttributes: [],
    isOnline: true,
  };
}
