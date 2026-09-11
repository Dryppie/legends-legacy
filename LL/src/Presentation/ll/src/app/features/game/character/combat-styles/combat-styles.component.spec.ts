import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { HelpOverlayService } from '../../../../shared/help/help-overlay.service';
import {
  CombatStyleOverview,
  combatStyleNextMilestone,
} from '../../../../shared/models/combat-styles';
import { CombatStylesComponent } from './combat-styles.component';

describe('Combat Style milestones', () => {
  it('gives every remaining level a milestone, including opening and mastery unlocks', () => {
    expect(combatStyleNextMilestone(0)).toBe('Level 1: Mastery bonus');
    expect(combatStyleNextMilestone(1)).toBe('Level 2: Mastery bonus');
    expect(combatStyleNextMilestone(6)).toBe(
      'Level 7: Mastery bonus and Opening Technique',
    );
    expect(combatStyleNextMilestone(7)).toBe(
      'Level 8: Mastery bonus and second upgrade slot',
    );
    expect(combatStyleNextMilestone(9)).toBe(
      'Level 10: Maximum mastery bonus and level cap',
    );
    expect(combatStyleNextMilestone(8)).toBe(
      'Level 9: Mastery bonus and Upgrade Mastery',
    );
    expect(combatStyleNextMilestone(10)).toContain('Maximum mastery');
  });
});

describe('Combat Styles global configuration page', () => {
  async function createPage(level = 0) {
    const overview: CombatStyleOverview = {
      contentVersion: 'test',
      selection: {
        combatStyleId: 'conduit',
        refinementId: null,
        upgradeIds: [],
        masteredUpgradeId: null,
      },
      effectiveStyle: null,
      validationIssue: null,
      previewFacts: [],
      styles: [
        {
          definition: {
            id: 'conduit',
            name: 'Conduit',
            kind: 'Conduit',
            description:
              'The first Essence in your Loadout is Channeled. Each other Essence grants 1 Charge when cast, up to a cap of 3 Charges. Your Channeled Essence consumes all Charges to boost its Damage, Healing, and Barrier.',
            tuning: {
              barrierFraction: 0.75,
              barrierPerMasteryLevel: 0.01,
              channeledBaseMultiplier: 0.8,
              channeledPerCharge: 0.2,
              channeledPerMasteryLevel: 0.01,
            },
            openingTechnique: {
              name: 'Primed Circuit',
              description: 'Begin every battle with 1 Charge.',
            },
            refinements: [
              {
                id: 'refinement',
                name: 'Refined circuit',
                description: 'An alternative circuit.',
              },
            ],
            upgrades: [
              {
                id: 'upgrade-one',
                name: 'First upgrade',
                description: 'First benefit.',
                masteryDescription: 'Empowered first benefit.',
              },
              {
                id: 'upgrade-two',
                name: 'Second upgrade',
                description: 'Second benefit.',
                masteryDescription: 'Empowered second benefit.',
              },
            ],
          },
          level,
          currentXp: 0,
          xpRequired: 36900,
          upgradeSlots: level >= 8 ? 2 : level >= 5 ? 1 : 0,
          refinementId: null,
          upgradeIds: [],
          masteredUpgradeId: null,
        },
      ],
    };
    const data = signal(overview);
    const draft = signal(overview.selection);
    const busy = signal(false);
    const state = {
      data,
      selected: computed(() => data().styles[0]),
      draft,
      preview: signal(overview),
      busy,
      loading: signal(false),
      previewing: signal(false),
      edited: signal(true),
      dirty: signal(false),
      canSave: computed(() => !busy()),
      error: signal<string | null>(null),
      message: signal(null),
      previewError: signal(null),
      resetDraft: jasmine.createSpy('resetDraft'),
      save: jasmine.createSpy('save'),
      refresh: jasmine.createSpy('refresh'),
      refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
      chooseStyle: jasmine.createSpy('chooseStyle'),
      changeMastery: jasmine
        .createSpy('changeMastery')
        .and.callFake((id: string | null) =>
          draft.update((selection) => ({
            ...selection,
            masteredUpgradeId: id,
          })),
        ),
      toggleUpgrade: jasmine
        .createSpy('toggleUpgrade')
        .and.callFake((id: string) =>
          draft.update((selection) => ({
            ...selection,
            upgradeIds: selection.upgradeIds.includes(id)
              ? selection.upgradeIds.filter((value) => value !== id)
              : [...selection.upgradeIds, id],
          })),
        ),
      changeRefinement: jasmine
        .createSpy('changeRefinement')
        .and.callFake((id: string | null) =>
          draft.update((selection) => ({ ...selection, refinementId: id })),
        ),
    };
    await TestBed.configureTestingModule({
      imports: [CombatStylesComponent],
      providers: [
        { provide: CharacterStateService, useValue: { overview: signal(null) } },
        { provide: CombatStyleStateService, useValue: state },
        {
          provide: HelpOverlayService,
          useValue: { open: jasmine.createSpy('open') },
        },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(CombatStylesComponent);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    return { fixture, element, state };
  }

  it('offers only actual styles and ignores the removed empty-slot tab', async () => {
    const { fixture, element, state } = await createPage();
    expect(fixture.componentInstance.styleTabs().map(tab => tab.key)).toEqual(['conduit']);
    expect(element.textContent).not.toContain('Empty slot');
    fixture.componentInstance.selectStyleTab('empty-slot');
    expect(state.chooseStyle).not.toHaveBeenCalled();
    fixture.componentInstance.selectStyleTab('conduit');
    expect(state.chooseStyle).toHaveBeenCalledOnceWith('conduit');
  });

  it('renders Duelist and switches mastery previews with the selected refinement', async () => {
    const { fixture, element, state } = await createPage(10);
    const duelist = {
      readRequired: 3, openingMultiplier: 1.45, perMasteryLevel: 0.01,
      returnedRead: 0, guardCharges: 0, firstImpressionRead: 2,
      upgradeBonus: 0.1, masteredMeasuredStrikesBonus: 0.2,
      finishingTouchHealthThreshold: 0.35, masteredFinishingTouchHealthThreshold: 0.5,
    };
    state.data.update((overview) => ({ ...overview, styles: overview.styles.map((entry) => {
      const tuning = { ...entry.definition.tuning!, duelist };
      return { ...entry, definition: {
        ...entry.definition, id: 'duelist', name: 'Duelist', kind: 'Duelist',
        description: 'Each basic attack or Essence cast that deals direct damage builds 1 Read. At 3 Read, your next damaging Essence consumes all Read to deal 145% direct damage to that opponent. Changing opponents resets Read.', tuning,
        openingTechnique: { name: 'First Impression', description: 'Gain 2 extra Read from your first successful action.' },
        refinements: [
          { id: 'flurry', name: 'Flurry', description: 'Regain 1 Read.', tuning: { ...tuning, duelist: { ...duelist, openingMultiplier: 1.3, returnedRead: 1 } } },
          { id: 'patient-blade', name: 'Patient Blade', description: 'Prepare 5 Read.', tuning: { ...tuning, duelist: { ...duelist, openingMultiplier: 1.8, readRequired: 5 } } },
          { id: 'guarded-thrust', name: 'Guarded Thrust', description: 'Gain Guard(1) after an Opening.', tuning: { ...tuning, duelist: { ...duelist, openingMultiplier: 1.25, guardCharges: 1 } } },
        ],
      } };
    }) }));
    for (const [form, strength, read] of [[null, 155, 3], ['flurry', 140, 3], ['patient-blade', 190, 5], ['guarded-thrust', 135, 3]] as const) {
      state.draft.set({ combatStyleId: 'duelist', refinementId: form, upgradeIds: [], masteredUpgradeId: null });
      fixture.detectChanges();
      expect(fixture.componentInstance.mechanicName()).toBe('Read the Opponent');
      expect(fixture.componentInstance.masteryBenefit()?.current).toBe(`Mastery 10: ${strength}% Opening damage`);
      expect(fixture.componentInstance.masteryBenefit()?.example).toContain(`At ${read} Read`);
      expect(element.querySelector('#style-mechanic-heading + p')?.textContent)
        .toContain('deal 155% direct damage to that opponent.');
    }
    expect(element.textContent).toContain('Guard(1)');
    expect(element.textContent).toContain('First Impression');
    expect(element.textContent).not.toContain('Channeled Essence');
    for (const [level, openingMultiplier, perMasteryLevel, expected] of [
      [0, 1.45, 0.01, '145%'],
      [5, 1.45, 0.01, '150%'],
      [5, 1.5, 0.015, '157.5%'],
    ] as const) {
      state.data.update((overview) => ({
        ...overview,
        styles: overview.styles.map((entry) => ({
          ...entry,
          level,
          definition: {
            ...entry.definition,
            tuning: {
              ...entry.definition.tuning!,
              duelist: { ...duelist, openingMultiplier, perMasteryLevel },
            },
          },
        })),
      }));
      fixture.detectChanges();
      expect(element.querySelector('#style-mechanic-heading + p')?.textContent)
        .toContain(`deal ${expected} direct damage to that opponent.`);
    }
  });

  it('renders Reaper Harvest mastery and all three refinements without Conduit guidance', async () => {
    const { fixture, element, state } = await createPage(10);
    state.data.update((overview) => ({
      ...overview,
      styles: overview.styles.map((entry) => ({
        ...entry,
        definition: {
          ...entry.definition,
          id: 'reaper', name: 'Reaper', kind: 'Reaper',
          description: 'When an active ability directly damages an enemy, consume the next tick of all current Bleed, Burn, and Poison stacks you applied to them, dealing 110% of that damage immediately.',
          tuning: {
            ...entry.definition.tuning!,
            reaper: {
              baseMultiplier: 1.1, perMasteryLevel: 0.01, deathSentenceBonus: 0.1,
              lastRitesHealthThreshold: 0.35, upgradeBonus: 0.05,
              closingHandHealthThreshold: 0.35, masteredClosingHandHealthThreshold: 0.5,
              openingPoisonStacks: 5,
            },
          },
          openingTechnique: { name: 'Grave Seed', description: 'Apply Poison(5) at battle start.' },
          refinements: [
            { id: 'soul-siphon', name: 'Soul Siphon', description: 'Heal yourself.' },
            { id: 'last-rites', name: 'Last Rites', description: 'Harvest all future ticks at 35% Health.' },
            { id: 'death-sentence', name: 'Death Sentence', description: 'Bank damage as Doom.' },
          ],
        },
      })),
    }));
    state.draft.set({ combatStyleId: 'reaper', refinementId: null, upgradeIds: [], masteredUpgradeId: null });
    fixture.detectChanges();
    expect(fixture.componentInstance.mechanicName()).toBe('Harvest');
    expect(fixture.componentInstance.masteryBenefit()?.current).toBe('Mastery 10: 120% Harvest');
    expect(element.querySelector('#style-mechanic-heading + p')?.textContent)
      .toContain('dealing 120% of that damage immediately.');
    expect(element.textContent).toContain('Soul Siphon');
    expect(element.textContent).toContain('Last Rites');
    expect(element.textContent).toContain('Death Sentence');
    expect(element.textContent).toContain('Grave Seed');
    expect(element.textContent).not.toContain('Channeled Essence');
    state.draft.update((draft) => ({ ...draft, refinementId: 'death-sentence' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.masteryBenefit()?.current).toBe('Mastery 10: 130% Harvest');
    expect(fixture.componentInstance.masteryBenefit()?.example).toContain('store 130 Magical Damage as Doom');
    state.draft.update((draft) => ({ ...draft, refinementId: 'soul-siphon' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.masteryBenefit()?.current).toBe('Mastery 10: 120% Harvest');
    expect(fixture.componentInstance.masteryBenefit()?.example).toContain('restore up to 120 Health');
    for (const [level, baseMultiplier, perMasteryLevel, expected] of [
      [0, 1.1, 0.01, '110%'],
      [5, 1.1, 0.01, '115%'],
      [5, 1.2, 0.015, '127.5%'],
    ] as const) {
      state.data.update((overview) => ({
        ...overview,
        styles: overview.styles.map((entry) => ({
          ...entry,
          level,
          definition: {
            ...entry.definition,
            tuning: {
              ...entry.definition.tuning!,
              reaper: { ...entry.definition.tuning!.reaper!, baseMultiplier, perMasteryLevel },
            },
          },
        })),
      }));
      fixture.detectChanges();
      expect(element.querySelector('#style-mechanic-heading + p')?.textContent)
        .toContain(`dealing ${expected} of that damage immediately.`);
    }
  });

  it('shows starting mastery and the full first combat XP block', async () => {
    const { element } = await createPage();
    expect(
      element.querySelector('.level-stat strong')?.textContent?.trim(),
    ).toBe('0');
    const progress =
      element.querySelector<HTMLProgressElement>('.level-progress')!;
    expect(progress.value).toBe(0);
    expect(progress.max).toBe(36900);
    expect(element.querySelector('.level-stat')?.textContent).toContain(
      '36,900',
    );
  });

  it('uses the shared header, icon and navigation with the global save controls', async () => {
    const { element, state } = await createPage();
    expect(
      element.querySelector('.style-header')?.classList.contains('ll-panel'),
    ).toBeFalse();
    expect(
      element.querySelector('app-default-header .header-stats'),
    ).not.toBeNull();
    expect(
      element.querySelector('app-default-header img')?.getAttribute('src'),
    ).toBe('icons/sidebar/character/combat-styles.svg');
    expect(
      element.querySelector('app-navigation-tabs'),
    ).not.toBeNull();
    expect(
      Array.from(element.querySelectorAll('button')).map((button) =>
        button.textContent?.trim(),
      ),
    ).not.toContain('Page tour');
    expect(
      Array.from(element.querySelectorAll('button')).map((button) =>
        button.textContent?.trim(),
      ),
    ).not.toContain('Refresh');
    expect(
      element.querySelector(
        '[aria-labelledby="style-mechanic-heading"] app-dropdown',
      ),
    ).toBeNull();
    expect(
      element.querySelector('.overview-grid > .milestone-panel'),
    ).not.toBeNull();
    expect(element.querySelector('select')).toBeNull();
    expect(element.querySelector('.battle-notice')?.textContent).toContain(
      'Conduit is in your slot.',
    );
    expect(element.textContent).not.toMatch(/cost|cast order|penalty/i);
    const controls = element.querySelector('[data-tour="combat-style-save"]')!;
    expect(controls.closest('.battle-notice')).not.toBeNull();
    expect(
      element.querySelector('app-default-header app-regular-button'),
    ).toBeNull();
    expect(element.querySelector('.mobile-save')).toBeNull();
    const discard = Array.from(controls.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Discard',
    )!;
    const save = Array.from(controls.querySelectorAll('button')).find(
      (button) => button.textContent?.trim() === 'Save',
    )!;
    discard.click();
    save.click();
    expect(state.resetDraft).toHaveBeenCalledOnceWith();
    expect(state.save).toHaveBeenCalledOnceWith();
    expect(element.querySelector('fieldset')?.disabled).toBeTrue();
  });

  it('allows taking Conduit into battle without a separately selected Channeled Essence', async () => {
    const { fixture, element, state } = await createPage();
    state.data.update((data) => ({
      ...data,
      selection: { ...data.selection, combatStyleId: null },
    }));
    fixture.detectChanges();
    const action = element.querySelector<HTMLButtonElement>(
      '.battle-action button',
    )!;
    expect(element.querySelector('.preview-notice')?.textContent).toContain(
      'Your slot stays empty',
    );
    expect(action.textContent).toContain('Take Conduit into battle');
    expect(action.disabled).toBeFalse();
    expect(element.querySelector('app-dropdown')).toBeNull();
    expect(
      element.querySelector('[data-tour="combat-style-channeled-essence"]')
        ?.textContent,
    ).toContain('The first Essence in your Loadout is Channeled.');
    expect(element.textContent).not.toContain('Your other Essences build Charge to strengthen its casts.');
    action.click();
    expect(state.save).toHaveBeenCalledOnceWith();
  });

  it('shows the automatic opening unlock and meaningful level seven and nine milestones', async () => {
    const { fixture, element, state } = await createPage(6);
    const opening = element.querySelector('.opening-technique')!;
    expect(opening.textContent).toContain('Primed Circuit');
    expect(opening.textContent).toContain('Begin every battle with 1 Charge.');
    expect(opening.textContent).toContain('Unlocks at level 7');
    const steps = element.querySelectorAll('.milestone-step');
    expect(steps.length).toBe(11);
    expect(steps[1].textContent).toContain('Mastery bonus');
    expect(steps[7].classList).toContain('milestone-unlock');
    expect(steps[9].classList).toContain('milestone-unlock');
    expect(steps[7].textContent).toContain('Opening Technique');
    expect(steps[9].textContent).toContain('Upgrade Mastery');
    expect(
      element.querySelector('.milestone-summary')!.textContent,
    ).not.toContain('No unlock');
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({ ...entry, level: 7 })),
    }));
    fixture.detectChanges();
    expect(opening.textContent).toContain('Unlocked at level 7');
    expect(opening.textContent).not.toContain('Unlocks at level 7');
  });

  it('improves Bastion every mastery level, including odd levels, through the unchanged maximum', async () => {
    const { fixture, element, state } = await createPage(0);
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({
        ...entry,
        definition: {
          ...entry.definition,
          id: 'bastion',
          kind: 'Bastion' as const,
          name: 'Bastion',
        },
      })),
    }));
    for (const [level, barrier, next] of [
      [0, 150, 'Level 1: +1% converted Barrier'],
      [1, 151.5, 'Level 2: +2% converted Barrier'],
      [3, 154.5, 'Level 4: +4% converted Barrier'],
      [7, 160.5, 'Level 8: +8% converted Barrier'],
      [9, 163.5, 'Level 10: +10% converted Barrier'],
      [10, 165, null],
    ] as const) {
      state.data.update((data) => ({
        ...data,
        styles: data.styles.map((entry) => ({
          ...entry,
          level,
        })),
      }));
      fixture.detectChanges();
      const footer = element.querySelector(
        '.milestone-panel [aria-label="Mastery bonus"]',
      )!;
      expect(footer.textContent).toContain(
        '+1% of base converted Barrier per mastery level.',
      );
      expect(footer.textContent).toContain(
        `Mastery ${level}: +${level}% converted Barrier`,
      );
      expect(footer.textContent).toContain(
        `200 healing received: 150 → ${barrier} Barrier before upgrades, sharing and caps.`,
      );
      expect(
        element.querySelector('.mastery-bonus-stat')?.textContent,
      ).toContain(`+${level}% Barrier`);
      expect(element.textContent).not.toMatch(
        /core rank|unranked|even levels/i,
      );
      expect(fixture.componentInstance.masteryBenefit()?.next).toBe(next);
      if (next) expect(footer.textContent).toContain(next);
      else expect(footer.textContent).not.toContain('Next level');
    }
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({
        ...entry,
        definition: {
          ...entry.definition,
          tuning: {
            ...entry.definition.tuning!,
            barrierFraction: 0.6,
            barrierPerMasteryLevel: 0.015,
          },
        },
      })),
    }));
    fixture.detectChanges();
    const footer = element.querySelector('[aria-label="Mastery bonus"]')!;
    expect(footer.textContent).toContain(
      '+1.5% of base converted Barrier per mastery level.',
    );
    expect(footer.textContent).toContain('Mastery 10: +15% converted Barrier');
    expect(footer.textContent).toContain(
      '200 healing received: 120 → 138 Barrier before upgrades, sharing and caps.',
    );
  });

  it('explains Conduit flat bonuses and recalculates them for the selected refinement', async () => {
    const { fixture, element, state } = await createPage(3);
    const footer = element.querySelector('[aria-label="Mastery bonus"]')!;
    expect(footer.textContent).toContain(
      "Each mastery level adds a flat +1% to your Channeled Essence's damage, healing and Barrier when it spends at least 1 Charge.",
    );
    expect(footer.textContent).toContain('Mastery 3: +3% flat increase');
    expect(footer.textContent).toContain(
      '1 Charge: 100% → 103% of normal strength before upgrades.',
    );
    expect(footer.textContent).toContain(
      'The mastery bonus applies once when your Channeled Essence spends at least 1 Charge.',
    );
    expect(footer.textContent).toContain(
      'Level 4: +4% flat increase to your Channeled Essence',
    );
    for (const level of [0, 1, 3, 7, 9, 10]) {
      state.data.update((data) => ({
        ...data,
        styles: data.styles.map((entry) => ({ ...entry, level })),
      }));
      fixture.detectChanges();
      expect(footer.textContent).toContain(
        `Mastery ${level}: +${level}% flat increase`,
      );
      expect(footer.textContent).toContain(
        `1 Charge: 100% → ${100 + level}% of normal strength before upgrades.`,
      );
      expect(fixture.componentInstance.masteryBenefit()?.next).toBe(
        level === 10
          ? null
          : `Level ${level + 1}: +${level + 1}% flat increase to your Channeled Essence`,
      );
    }
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({
        ...entry,
        level: 3,
        definition: {
          ...entry.definition,
          refinements: entry.definition.refinements.map((refinement) => ({
            ...refinement,
            tuning: {
              ...entry.definition.tuning!,
              channeledBaseMultiplier: 0.6,
              channeledPerCharge: 0.25,
              channeledPerMasteryLevel: 0.015,
            },
          })),
        },
      })),
    }));
    state.draft.update((selection) => ({
      ...selection,
      refinementId: 'refinement',
    }));
    fixture.detectChanges();
    expect(footer.textContent).toContain('adds a flat +1.5%');
    expect(footer.textContent).toContain('Mastery 3: +4.5% flat increase');
    expect(footer.textContent).toContain(
      '1 Charge: 85% → 89.5% of normal strength before upgrades.',
    );
    expect(footer.textContent).toContain(
      'Level 4: +6% flat increase to your Channeled Essence',
    );
    state.draft.update((selection) => ({ ...selection, refinementId: null }));
    fixture.detectChanges();
    expect(footer.textContent).toContain(
      '1 Charge: 100% → 103% of normal strength before upgrades.',
    );
  });

  it('omits mastery explanations when authored tuning is unavailable', async () => {
    const { fixture, element, state } = await createPage(10);
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({
        ...entry,
        definition: { ...entry.definition, tuning: undefined },
      })),
    }));
    fixture.detectChanges();
    expect(fixture.componentInstance.masteryBenefit()).toBeNull();
    expect(element.querySelector('[aria-label="Mastery bonus"]')).toBeNull();
  });

  it('empowers only equipped upgrades at level nine and allows changing or clearing the choice', async () => {
    const { fixture, element, state } = await createPage(9);
    const configuration = element.querySelector('.desktop-configuration')!;
    const upgrades = configuration.querySelectorAll<HTMLInputElement>(
      'input[type="checkbox"]',
    );
    const mastery = configuration.querySelectorAll<HTMLInputElement>(
      '.mastery-options input',
    );
    expect(mastery[0].disabled).toBeTrue();
    expect(mastery[1].disabled).toBeTrue();
    expect(configuration.textContent).toContain('Empowered first benefit.');
    upgrades[0].click();
    fixture.detectChanges();
    expect(mastery[0].disabled).toBeFalse();
    expect(mastery[1].disabled).toBeTrue();
    mastery[1].click();
    expect(state.changeMastery).not.toHaveBeenCalled();
    mastery[0].click();
    fixture.detectChanges();
    expect(state.changeMastery).toHaveBeenCalledWith('upgrade-one');
    expect(mastery[0].checked).toBeTrue();
    upgrades[1].click();
    fixture.detectChanges();
    mastery[1].click();
    fixture.detectChanges();
    expect(mastery[0].checked).toBeFalse();
    expect(mastery[1].checked).toBeTrue();
    expect(state.draft().upgradeIds).toEqual(['upgrade-one', 'upgrade-two']);
    configuration
      .querySelector<HTMLButtonElement>('.mastery-options button')!
      .click();
    fixture.detectChanges();
    expect(state.draft().masteredUpgradeId).toBeNull();
    expect(mastery[1].checked).toBeFalse();
    expect(state.save).not.toHaveBeenCalled();
  });

  it('keeps Upgrade Mastery locked before level nine and blocks changes while saving', async () => {
    const { fixture, element, state } = await createPage(8);
    state.draft.update((selection) => ({
      ...selection,
      upgradeIds: ['upgrade-one'],
    }));
    fixture.detectChanges();
    const fieldset =
      element.querySelector<HTMLFieldSetElement>('.mastery-options')!;
    const radio = fieldset.querySelector<HTMLInputElement>('input')!;
    expect(fieldset.disabled).toBeTrue();
    expect(fieldset.textContent).toContain(
      'At Mastery 9, choose one of your equipped upgrades',
    );
    radio.click();
    expect(state.changeMastery).not.toHaveBeenCalled();
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({ ...entry, level: 9 })),
    }));
    fixture.detectChanges();
    expect(fieldset.disabled).toBeFalse();
    state.busy.set(true);
    fixture.detectChanges();
    radio.click();
    expect(state.changeMastery).not.toHaveBeenCalled();
  });

  it('keeps the current mechanic preview in place while an edited choice is being recalculated', async () => {
    const { fixture, element, state } = await createPage(9);
    state.preview.update((overview) => ({
      ...overview,
      previewFacts: [
        { label: '0 Charge', value: '80% of normal strength', condition: null },
        {
          label: '1 Charge',
          value: '102% of normal strength',
          condition: null,
        },
      ],
    }));
    fixture.detectChanges();
    const panel = element.querySelector<HTMLElement>(
      '[aria-labelledby="style-mechanic-heading"]',
    )!;
    const example = panel.querySelector<HTMLElement>('.mechanic-example')!;
    const panelHeight = panel.getBoundingClientRect().height;
    state.previewing.set(true);
    fixture.detectChanges();
    expect(panel.getAttribute('aria-busy')).toBe('true');
    expect(panel.querySelector('.mechanic-example')).toBe(example);
    expect(panel.getBoundingClientRect().height).toBe(panelHeight);
    expect(panel.querySelector('[role="status"]')?.textContent).toContain(
      'Updating Combat Style preview',
    );
    expect(panel.querySelector('[role="status"]')?.classList).toContain(
      'sr-only',
    );
    expect(example.textContent).toContain('80% of normal strength');
    state.preview.update((overview) => ({
      ...overview,
      previewFacts: [
        { label: '0 Charge', value: '90% of normal strength', condition: null },
      ],
    }));
    state.previewing.set(false);
    fixture.detectChanges();
    expect(panel.getAttribute('aria-busy')).toBe('false');
    expect(panel.querySelector('.mechanic-example')).toBe(example);
    expect(example.textContent).toContain('90% of normal strength');
    expect(panel.querySelector('[role="status"]')).toBeNull();
  });

  it('keeps the header and content scroll position stable when a mastery card focuses its hidden radio', async () => {
    const { fixture, element, state } = await createPage(9);
    state.draft.update((selection) => ({
      ...selection,
      upgradeIds: ['upgrade-one', 'upgrade-two'],
      masteredUpgradeId: 'upgrade-one',
    }));
    fixture.detectChanges();
    const frame = document.createElement('iframe');
    frame.style.width = '1200px';
    frame.style.height = '560px';
    document.body.appendChild(frame);
    try {
      const doc = frame.contentDocument!;
      const styles = doc.createElement('style');
      styles.textContent =
        Array.from(document.styleSheets)
          .flatMap((sheet) =>
            Array.from(sheet.cssRules, (rule) => rule.cssText),
          )
          .join('\n') +
        '\nhtml, body { margin: 0; height: 100%; } body { padding: 12px; box-sizing: border-box; }';
      doc.head.appendChild(styles);
      doc.body.appendChild(element.cloneNode(true));
      const page = doc.querySelector<HTMLElement>('.style-page')!;
      const header = doc.querySelector<HTMLElement>('.style-header')!;
      const scroll = doc.querySelector<HTMLElement>('.style-scroll')!;
      const card = doc.querySelectorAll<HTMLLabelElement>(
        '.desktop-configuration .mastery-options label',
      )[1];
      const radio = card.querySelector<HTMLInputElement>('input')!;
      expect(scroll.scrollHeight).toBeGreaterThan(scroll.clientHeight);
      scroll.scrollTop = scroll.scrollHeight;
      const headerTop = header.getBoundingClientRect().top;
      const scrollTop = scroll.scrollTop;
      const cardBounds = card.getBoundingClientRect();
      expect(cardBounds.top).toBeGreaterThanOrEqual(
        scroll.getBoundingClientRect().top,
      );
      expect(cardBounds.bottom).toBeLessThanOrEqual(
        scroll.getBoundingClientRect().bottom,
      );
      card.click();
      expect(doc.activeElement).toBe(radio);
      expect(radio.checked).toBeTrue();
      expect(page.scrollTop).toBe(0);
      expect(header.getBoundingClientRect().top).toBe(headerTop);
      expect(scroll.scrollTop).toBe(scrollTop);
      // Keyboard focus must use the same visible card position.
      radio.blur();
      radio.focus();
      expect(page.scrollTop).toBe(0);
      expect(header.getBoundingClientRect().top).toBe(headerTop);
      expect(scroll.scrollTop).toBe(scrollTop);
    } finally {
      frame.remove();
    }
  });

  it('stages mobile refinement choices until confirmed and resets dismissed choices on reopening', async () => {
    const { fixture, element, state } = await createPage(10);
    const dialog =
      element.querySelector<HTMLDialogElement>('.refinement-sheet')!;
    const open = element.querySelector<HTMLButtonElement>('.mobile-summary')!;
    open.click();
    fixture.detectChanges();
    expect(dialog.open).toBeTrue();
    const radios = dialog.querySelectorAll<HTMLInputElement>(
      'input[type="radio"]',
    );
    radios[1].click();
    fixture.detectChanges();
    expect(state.changeRefinement).not.toHaveBeenCalled();
    dialog.querySelector<HTMLButtonElement>('.editor-close')!.click();
    open.click();
    fixture.detectChanges();
    expect(radios[0].checked).toBeTrue();
    radios[1].click();
    fixture.detectChanges();
    dialog.querySelector<HTMLButtonElement>('.editor-footer button')!.click();
    fixture.detectChanges();
    expect(state.changeRefinement).toHaveBeenCalledOnceWith('refinement');
    expect(dialog.open).toBeFalse();
    expect(state.save).not.toHaveBeenCalled();
  });

  it('keeps mobile refinements locked before level three and while saving', async () => {
    const { fixture, element, state } = await createPage();
    const dialog =
      element.querySelector<HTMLDialogElement>('.refinement-sheet')!;
    fixture.componentInstance.openRefinements(dialog);
    expect(dialog.open).toBeFalse();
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({ ...entry, level: 10 })),
    }));
    state.busy.set(true);
    fixture.detectChanges();
    fixture.componentInstance.openRefinements(dialog);
    expect(dialog.open).toBeFalse();
  });

  it('respects mobile upgrade capacity, clears choices and shows save failures inside the editor', async () => {
    const { fixture, element, state } = await createPage(5);
    const dialog = element.querySelector<HTMLDialogElement>('.upgrade-screen')!;
    element.querySelectorAll<HTMLButtonElement>('.mobile-summary')[1].click();
    fixture.detectChanges();
    const choices = dialog.querySelectorAll<HTMLInputElement>(
      'input[type="checkbox"]',
    );
    choices[0].click();
    fixture.detectChanges();
    expect(choices[1].disabled).toBeTrue();
    expect(dialog.textContent).toContain('Slot 1');
    const buttons = dialog.querySelectorAll<HTMLButtonElement>(
      '.editor-footer button',
    );
    buttons[1].click();
    expect(state.save).toHaveBeenCalledOnceWith();
    state.error.set('Combat Style could not be changed.');
    fixture.detectChanges();
    expect(dialog.querySelector('[role="alert"]')?.textContent).toContain(
      'Combat Style could not be changed.',
    );
    expect(dialog.open).toBeTrue();
    buttons[0].click();
    fixture.detectChanges();
    expect(state.draft().upgradeIds).toEqual([]);
    expect(choices[1].disabled).toBeFalse();
    dialog.close();
  });

  it('keeps desktop upgrade descriptions and reserved status columns fixed when choices change', async () => {
    const { fixture, element, state } = await createPage(10);
    state.data.update((data) => ({
      ...data,
      styles: data.styles.map((entry) => ({
        ...entry,
        definition: {
          ...entry.definition,
          upgrades: [
            {
              id: 'prepared-wall',
              name: 'Prepared Wall',
              description:
                'When you receive healing at 80% Health or higher, Fortification grants extra Barrier equal to 7.5% of the heal.',
              masteryDescription:
                'You also gain this bonus when you have no Barrier.',
            },
            {
              id: 'hold-the-breach',
              name: 'Hold the Breach',
              description:
                'When you receive healing while having no Barrier, Fortification grants extra Barrier equal to 7.5% of the heal.',
              masteryDescription: 'That heal also restores 20% more Health.',
            },
            {
              id: 'measured-recovery',
              name: 'Measured Recovery',
              description:
                'Fortification’s normal healing split restores 30% as Health, up from 25%.',
              masteryDescription: 'Any overhealing is converted into Barrier.',
            },
          ],
        },
      })),
    }));
    const frame = document.createElement('iframe');
    frame.style.height = '900px';
    document.body.appendChild(frame);
    try {
      const doc = frame.contentDocument!;
      const styles = doc.createElement('style');
      styles.textContent =
        Array.from(document.styleSheets)
          .flatMap((sheet) =>
            Array.from(sheet.cssRules, (rule) => rule.cssText),
          )
          .join('\n') +
        '\nhtml, body { margin: 0; height: 100%; } body { padding: 12px; box-sizing: border-box; }';
      doc.head.appendChild(styles);
      const measure = () => {
        doc.body.replaceChildren(element.cloneNode(true));
        const list = doc.querySelector<HTMLElement>(
          '.desktop-configuration .upgrade-choice-list',
        )!;
        const rows = Array.from(
          list.querySelectorAll<HTMLElement>('.upgrade-option'),
        );
        expect(rows.length).toBe(3);
        const measurements = rows.map((row, index) => {
          const bounds = row.getBoundingClientRect();
          const name = row
            .querySelector<HTMLElement>('.upgrade-option-name')!
            .getBoundingClientRect();
          const description = row
            .querySelector<HTMLElement>('.upgrade-option-description')!
            .getBoundingClientRect();
          const status = row.querySelector<HTMLElement>(
            '.upgrade-option-status',
          )!;
          const statusBounds = status.getBoundingClientRect();
          const selected = state
            .draft()
            .upgradeIds.includes(
              state.data().styles[0].definition.upgrades[index].id,
            );
          expect(statusBounds.width).toBeGreaterThan(0);
          expect(description.left - name.right).toBeGreaterThanOrEqual(0);
          expect(description.left - name.right).toBeLessThanOrEqual(16);
          expect(statusBounds.left).toBeGreaterThanOrEqual(description.right);
          expect(statusBounds.top).toBeLessThanOrEqual(description.bottom);
          expect(statusBounds.bottom).toBeGreaterThanOrEqual(description.top);
          expect(row.scrollWidth).toBeLessThanOrEqual(row.clientWidth);
          expect(frame.contentWindow!.getComputedStyle(status).visibility).toBe(
            selected ? 'visible' : 'hidden',
          );
          if (selected) {
            expect(status.textContent?.trim()).toBe('Selected');
            expect(status.getAttribute('aria-hidden')).not.toBe('true');
          } else {
            expect(status.getAttribute('aria-hidden')).toBe('true');
          }
          return {
            height: bounds.height,
            descriptionLeft: description.left - bounds.left,
            descriptionTop: description.top - bounds.top,
            statusLeft: statusBounds.left - bounds.left,
            statusTop: statusBounds.top - bounds.top,
            statusWidth: statusBounds.width,
          };
        });
        for (const row of measurements) {
          expect(row.descriptionLeft).toBe(measurements[0].descriptionLeft);
          expect(row.statusLeft).toBe(measurements[0].statusLeft);
          expect(row.statusWidth).toBe(measurements[0].statusWidth);
        }
        return { width: list.getBoundingClientRect().width, measurements };
      };
      for (const [frameWidth, minimumListWidth, maximumListWidth] of [
        [1380, 780, 820],
        [820, 440, 480],
      ]) {
        frame.style.width = `${frameWidth}px`;
        state.draft.update((selection) => ({ ...selection, upgradeIds: [] }));
        fixture.detectChanges();
        const baseline = measure();
        expect(baseline.width).toBeGreaterThanOrEqual(minimumListWidth);
        expect(baseline.width).toBeLessThanOrEqual(maximumListWidth);
        for (const index of [0, 1, 0, 2]) {
          element
            .querySelectorAll<HTMLInputElement>(
              '.desktop-configuration .upgrade-option input',
            )
            [index].click();
          fixture.detectChanges();
          expect(measure().measurements).toEqual(baseline.measurements);
        }
      }
    } finally {
      frame.remove();
    }
  });

  it('keeps preview facts, tabs and the equip action readable on desktop and mobile', async () => {
    const { fixture, element, state } = await createPage(9);
    state.data.update((data) => ({
      ...data,
      selection: { ...data.selection, combatStyleId: 'bastion' },
      styles: [
        ...data.styles,
        {
          ...data.styles[0],
          definition: {
            ...data.styles[0].definition,
            id: 'bastion',
            name: 'Bastion',
          },
        },
      ],
    }));
    // Both styles' preview facts use these same generic rows.
    state.preview.update((overview) => ({
      ...overview,
      previewFacts: [
        { label: '0 Charge', value: '80% of normal strength', condition: null },
        {
          label: 'Charge limit',
          value: '3 Charge',
          condition:
            'Each of your other Essences builds 1 Charge when it casts, once between Channeled Essence casts.',
        },
        {
          label: 'Full Circuit',
          value: '+5% flat increase',
          condition:
            'Gain this bonus when your Channeled Essence spends maximum Charge. Included in the Charge examples above.',
        },
        {
          label: 'Emergency Channel',
          value: '+5% flat increase to healing and Barrier on yourself',
          condition:
            'Gain this bonus to the healing and Barrier your Channeled Essence gives you immediately when it spends at least 1 Charge and you start the cast at 35% Health or lower.',
        },
        {
          label: 'Prepared Wall',
          value: '+7.5% flat increase · +15 Barrier',
          condition:
            'Gain extra Barrier when you receive healing at 80% Health or higher.',
        },
        {
          label: 'Measured Recovery',
          value: '+20% Health recovery (×1.2) · 60 Health',
          condition:
            'Recover more Health when Fortification splits healing between Health and Barrier. Included in the healing example above.',
        },
        {
          label: 'Hold the Breach mastery',
          value: '+20% Health recovery (×1.2) · 72 Health',
          condition:
            'Recover more Health when you receive healing with no Barrier.',
        },
      ],
    }));
    fixture.detectChanges();
    const frame = document.createElement('iframe');
    frame.style.height = '844px';
    document.body.appendChild(frame);
    try {
      const doc = frame.contentDocument!;
      const styles = doc.createElement('style');
      styles.textContent =
        Array.from(document.styleSheets)
          .flatMap((sheet) =>
            Array.from(sheet.cssRules, (rule) => rule.cssText),
          )
          .join('\n') +
        '\nhtml, body { margin: 0; height: 100%; } body { padding: 12px; box-sizing: border-box; }';
      doc.head.appendChild(styles);
      doc.body.appendChild(element.cloneNode(true));
      for (const width of [320, 390, 1440]) {
        frame.style.width = `${width}px`;
        const notice = doc.querySelector<HTMLElement>('.preview-notice')!;
        const copy = doc.querySelector<HTMLElement>('.battle-notice-copy')!;
        const action = doc.querySelector<HTMLElement>('.battle-action button')!;
        expect(doc.documentElement.scrollWidth).toBeLessThanOrEqual(
          doc.documentElement.clientWidth,
        );
        for (const item of [
          notice,
          copy,
          action,
          ...Array.from(
            doc.querySelectorAll<HTMLElement>('.style-tabs [role="tab"]'),
          ),
        ]) {
          expect(item.scrollWidth)
            .withContext(`${width}px: ${item.className}`)
            .toBeLessThanOrEqual(item.clientWidth);
        }
        const mechanic = doc.querySelector<HTMLElement>(
          '[aria-labelledby="style-mechanic-heading"]',
        )!;
        const detailValues = mechanic.querySelectorAll<HTMLElement>(
          '.mechanic-fact-value',
        );
        expect(detailValues.length).toBe(6);
        for (const value of Array.from(detailValues)) {
          const row = value.parentElement!;
          const rowBounds = row.getBoundingClientRect();
          expect(row.scrollWidth)
            .withContext(`${width}px preview row`)
            .toBeLessThanOrEqual(row.clientWidth);
          for (const part of Array.from(row.children) as HTMLElement[]) {
            const bounds = part.getBoundingClientRect();
            expect(bounds.left)
              .withContext(`${width}px preview content left`)
              .toBeGreaterThanOrEqual(rowBounds.left - 1);
            expect(bounds.right)
              .withContext(`${width}px preview content right`)
              .toBeLessThanOrEqual(rowBounds.right + 1);
            expect(part.scrollWidth)
              .withContext(`${width}px preview content width`)
              .toBeLessThanOrEqual(part.clientWidth);
          }
          const condition = row.querySelector('.mechanic-fact-condition');
          if (row.querySelector('dt')!.textContent!.trim() === 'Charge limit') {
            expect(condition).toBeNull();
            continue;
          }
          expect(condition).not.toBeNull();
          expect(condition!.getBoundingClientRect().top).toBeGreaterThanOrEqual(
            Math.max(
              value.getBoundingClientRect().bottom,
              row.querySelector('dt')!.getBoundingClientRect().bottom,
            ) - 1,
          );
        }
        expect(copy.textContent).toContain('Bastion stays in battle');
        expect(action.textContent).toContain('Take Conduit into battle');
        const actionBounds = action.getBoundingClientRect();
        const copyBounds = copy.getBoundingClientRect();
        if (width === 1440) {
          expect(actionBounds.left).toBeGreaterThan(copyBounds.right);
          expect(actionBounds.top).toBeLessThan(copyBounds.bottom);
        } else {
          expect(actionBounds.top).toBeGreaterThan(copyBounds.bottom);
        }
      }
    } finally {
      frame.remove();
    }
  });

  it('fits the overview, refinement sheet and two upgrade slots into narrow mobile viewports', async () => {
    const { fixture, element, state } = await createPage(10);
    state.data.update((data) => ({
      ...data,
      styles: [
        ...data.styles,
        {
          ...data.styles[0],
          definition: {
            ...data.styles[0].definition,
            id: 'bastion',
            name: 'Bastion',
            kind: 'Bastion' as const,
          },
        },
      ],
    }));
    fixture.detectChanges();
    // Render the real Angular markup and styles in an independently sized viewport.
    const frame = document.createElement('iframe');
    frame.style.height = '844px';
    document.body.appendChild(frame);
    try {
      const doc = frame.contentDocument!;
      const styles = document.createElement('style');
      styles.textContent =
        Array.from(document.styleSheets)
          .flatMap((sheet) =>
            Array.from(sheet.cssRules, (rule) => rule.cssText),
          )
          .join('\n') +
        '\nhtml, body { margin: 0; height: 100%; } body { padding: 12px; box-sizing: border-box; }';
      doc.head.appendChild(styles);
      doc.body.appendChild(element.cloneNode(true));
      for (const width of [320, 390]) {
        frame.style.width = `${width}px`;
        const viewport = doc.documentElement.clientWidth;
        const controls = doc.querySelector<HTMLElement>('.battle-actions')!;
        expect(frame.contentWindow!.getComputedStyle(controls).display).toBe(
          'grid',
        );
        expect(doc.documentElement.scrollWidth).toBeLessThanOrEqual(viewport);
        expect(controls.getBoundingClientRect().bottom).toBeLessThanOrEqual(
          844,
        );
        expect(controls.scrollWidth).toBeLessThanOrEqual(controls.clientWidth);
        const equipped = doc.querySelector<HTMLElement>(
          '.equipped-style-status',
        )!;
        expect(
          frame.contentWindow!.getComputedStyle(equipped).display,
        ).not.toBe('none');
        expect(equipped.textContent).toContain('Conduit');
        expect(equipped.getBoundingClientRect().height).toBeGreaterThan(0);
        const tabs = doc.querySelectorAll<HTMLElement>(
          '.style-tabs [role="tab"]',
        );
        expect(tabs.length).toBe(2);
        for (const tab of Array.from(tabs)) {
          expect(tab.scrollWidth).toBeLessThanOrEqual(tab.clientWidth);
          const badge = tab.querySelector<HTMLElement>(
            '.navigation-tab-status',
          );
          if (badge) {
            expect(badge.textContent?.trim()).toBe('Equipped');
            expect(badge.getBoundingClientRect().left).toBeGreaterThanOrEqual(
              tab.getBoundingClientRect().left,
            );
            expect(badge.getBoundingClientRect().right).toBeLessThanOrEqual(
              tab.getBoundingClientRect().right,
            );
          }
        }
        const masteryBenefit = doc.querySelector<HTMLElement>(
          '.milestone-mastery-benefit',
        )!;
        expect(
          frame.contentWindow!.getComputedStyle(masteryBenefit).display,
        ).not.toBe('none');
        expect(masteryBenefit.getBoundingClientRect().height).toBeGreaterThan(
          0,
        );
        expect(masteryBenefit.scrollWidth).toBeLessThanOrEqual(
          masteryBenefit.clientWidth,
        );
        expect(masteryBenefit.textContent).toContain('flat increase');
        expect(masteryBenefit.textContent).toContain(
          'Mastery 10: +10% flat increase',
        );
        expect(masteryBenefit.textContent).toContain(
          '1 Charge: 100% → 110% of normal strength before upgrades.',
        );
        expect(masteryBenefit.textContent).not.toContain('Next level');
        for (const selector of ['.refinement-sheet', '.upgrade-screen']) {
          const dialog = doc.querySelector<HTMLDialogElement>(selector)!;
          dialog.showModal();
          expect(dialog.getBoundingClientRect().width).toBeLessThanOrEqual(
            viewport,
          );
          const content = dialog.querySelector<HTMLElement>('.editor-scroll')!;
          expect(content.scrollWidth).toBeLessThanOrEqual(content.clientWidth);
          if (selector === '.upgrade-screen') {
            const slots = dialog.querySelectorAll<HTMLElement>(
              '.upgrade-slots > div',
            );
            expect(slots[0].getBoundingClientRect().top).toBe(
              slots[1].getBoundingClientRect().top,
            );
            for (const row of Array.from(
              dialog.querySelectorAll<HTMLElement>('.upgrade-option'),
            )) {
              const name = row
                .querySelector<HTMLElement>('.upgrade-option-name')!
                .getBoundingClientRect();
              const description = row
                .querySelector<HTMLElement>('.upgrade-option-description')!
                .getBoundingClientRect();
              const status = row
                .querySelector<HTMLElement>('.upgrade-option-status')!
                .getBoundingClientRect();
              expect(status.width).toBeGreaterThan(0);
              expect(status.left).toBeGreaterThan(name.right);
              expect(status.top).toBeLessThanOrEqual(name.bottom);
              expect(status.bottom).toBeGreaterThanOrEqual(name.top);
              expect(description.top).toBeGreaterThanOrEqual(
                Math.max(name.bottom, status.bottom),
              );
              expect(description.left).toBe(name.left);
              expect(description.right).toBe(status.right);
            }
          }
          dialog.close();
        }
      }
    } finally {
      frame.remove();
    }
  });
});
