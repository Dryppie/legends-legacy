import { computed, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';
import { CharacterActionsStateService } from '../../../../core/services/api/character-actions/character-actions.state.service';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { EssenceItemViewService } from '../../../../core/services/api/essences/essence-item-view.service';
import { EssenceStateService } from '../../../../core/services/api/essences/essence-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { QuestPresenterService } from '../../../../core/services/api/quest/quest-presenter.service';
import { QuestStateService } from '../../../../core/services/api/quest/quest-state.service';
import { HelpOverlayService } from '../../../../shared/help/help-overlay.service';
import {
  CreatureArchiveDto,
  CreatureArchiveEntryDto,
  EssenceDefinitionDto,
  EssenceLoadoutDto,
  PlayerEssenceDto,
} from '../../../../shared/models/essence-system';
import { EssencesComponent } from './essences.component';

describe('Essence loadout Channeled Essence', () => {
  async function createPage() {
    const options = [
      {
        id: 'passive',
        name: 'Ancient Guardian Essence',
        isChanneledEssenceEligible: false,
      },
      {
        id: 'channeled-essence',
        name: 'Greater Volcanic Salamander Essence',
        isChanneledEssenceEligible: true,
      },
    ] as PlayerEssenceDto[];
    const loadouts: EssenceLoadoutDto[] = [
      {
        id: 'default',
        name: 'Default',
        autoUseActivities: [],
        slots: [
          { slotIndex: 1, playerEssenceId: 'passive' },
          { slotIndex: 3, playerEssenceId: 'channeled-essence' },
        ],
      },
      {
        id: 'dungeon',
        name: 'Dungeon',
        autoUseActivities: ['Dungeon'],
        slots: [{ slotIndex: 2, playerEssenceId: 'channeled-essence' }],
      },
    ];
    const draftSlots = signal<(string | null)[]>([
      null,
      'passive',
      null,
      'channeled-essence',
    ]);
    const selectedLoadoutId = signal('default');
    const firstOccupiedDraftSlot = computed(() =>
      draftSlots().findIndex(Boolean),
    );
    const savingLoadout = signal(false);
    const creatureArchive = signal<CreatureArchiveDto | null>(null);
    const selectedLoadout = computed(
      () => loadouts.find((entry) => entry.id === selectedLoadoutId())!,
    );
    const state = {
      archive: signal({ essences: [], essenceDust: 0 }),
      archiveText: () => '',
      activeView: signal('archive'),
      loading: () => false,
      error: signal<string | null>(null),
      selected: () => null,
      creatureArchive,
      currentTime: () => Date.now(),
      canChangeCreatureFocus: computed(
        () => creatureArchive()?.canChangeCreatureFocus ?? false,
      ),
      focusedCreature: computed(
        () =>
          creatureArchive()?.creatures.find(
            (creature) => creature.isCreatureFocus,
          ) ?? null,
      ),
      highlightCreatureFocus: () => false,
      setCreatureFocus: jasmine.createSpy('setCreatureFocus'),
      codex: () => null,
      creatureFocusReady: () => false,
      essenceOptions: () => options,
      loadouts: () => ({ loadouts, unlockedSlots: 4, limit: 3 }),
      selectedLoadout,
      selectedLoadoutId,
      draftLoadoutName: computed(() => selectedLoadout().name),
      draftSlots,
      firstOccupiedDraftSlot,
      savingLoadout,
      slotIndexes: () => [0, 1, 2, 3],
      canSaveDraft: () => false,
      refreshIfDirty: jasmine.createSpy('refreshIfDirty'),
      canChannelEssence: (id: string) =>
        !savingLoadout() &&
        options.some(
          (essence) => essence.id === id && essence.isChanneledEssenceEligible,
        ) &&
        draftSlots().indexOf(id) >= 0 &&
        draftSlots().indexOf(id) !== firstOccupiedDraftSlot(),
      channelEssence: jasmine.createSpy('channelEssence'),
      selectLoadout: (loadout: EssenceLoadoutDto) => {
        selectedLoadoutId.set(loadout.id);
        draftSlots.set(
          [0, 1, 2, 3].map(
            (index) =>
              loadout.slots.find((slot) => slot.slotIndex === index)
                ?.playerEssenceId ?? null,
          ),
        );
      },
    };
    const styles = {
      data: signal({ selection: { combatStyleId: 'conduit' } }),
      dirty: signal(true),
      refreshIfDirty: jasmine.createSpy('refreshStylesIfDirty'),
    };
    await TestBed.configureTestingModule({
      imports: [EssencesComponent],
      providers: [
        { provide: EssenceStateService, useValue: state },
        { provide: CombatStyleStateService, useValue: styles },
        {
          provide: QuestStateService,
          useValue: { pinnedOnboardingObjective: () => null },
        },
        { provide: QuestPresenterService, useValue: {} },
        { provide: InventoryStateService, useValue: { items: () => [] } },
        { provide: EssenceItemViewService, useValue: {} },
        {
          provide: CharacterActionsStateService,
          useValue: {
            resolvingOfflineProgress: () => false,
            currentAction: () => null,
          },
        },
        {
          provide: HelpOverlayService,
          useValue: { open: jasmine.createSpy() },
        },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({})),
            queryParamMap: of(convertToParamMap({})),
          },
        },
        { provide: Router, useValue: { navigate: jasmine.createSpy() } },
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(EssencesComponent);
    fixture.detectChanges();
    fixture.componentInstance.mobileLoadoutOpen.set(true);
    fixture.detectChanges();
    return {
      fixture,
      element: fixture.nativeElement as HTMLElement,
      state,
      styles,
      options,
    };
  }

  it('marks the first occupied slot even when ineligible and offers Channel Essence only on eligible equipped Essences', async () => {
    const { fixture, element, state, styles } = await createPage();
    const slots = element.querySelectorAll('.soul-loadout-slot');
    expect(
      element.querySelectorAll('.soul-channeled-essence-badge').length,
    ).toBe(1);
    expect(
      slots[1].querySelector('.soul-channeled-essence-badge'),
    ).not.toBeNull();
    expect(slots[3].querySelector('.soul-channeled-essence-badge')).toBeNull();
    expect(
      element.querySelector('.soul-channeled-essence-hint')?.textContent,
    ).toContain('has no direct damage');
    const channelButton = slots[3].querySelector<HTMLButtonElement>(
      '.soul-channel-essence',
    )!;
    expect(channelButton.disabled).toBeFalse();
    channelButton.click();
    expect(state.channelEssence).toHaveBeenCalledOnceWith('channeled-essence');
    expect(styles.refreshIfDirty).toHaveBeenCalledTimes(1);
    state.savingLoadout.set(true);
    fixture.detectChanges();
    expect(channelButton.disabled).toBeTrue();
    expect(
      Array.from(
        element.querySelectorAll<HTMLButtonElement>('.soul-loadout-choice'),
      ).every((button) => button.disabled),
    ).toBeTrue();
    state.draftSlots.set([null, 'channeled-essence', null, 'passive']);
    fixture.detectChanges();
    expect(
      slots[1].querySelector('.soul-channeled-essence-badge'),
    ).not.toBeNull();
    expect(
      element.querySelector('.soul-channeled-essence-hint')?.textContent,
    ).toContain('Your other Essences build Charge');
    state.draftSlots.set([null, 'passive', null, 'channeled-essence']);
    state.error.set('Loadout could not be saved.');
    state.savingLoadout.set(false);
    fixture.detectChanges();
    expect(
      element.querySelector('.soul-channeled-essence-hint')?.textContent,
    ).toContain('has no direct damage');
    expect(element.querySelector('.ll-state-danger')?.textContent).toContain(
      'Loadout could not be saved.',
    );
  });

  it('updates Channeled Essence for each displayed loadout, empty slots and the saved Combat Style', async () => {
    const { fixture, element, state, styles } = await createPage();
    const dungeon = Array.from(
      element.querySelectorAll<HTMLButtonElement>('.soul-loadout-choice'),
    ).find((button) => button.textContent?.trim() === 'Dungeon')!;
    dungeon.click();
    fixture.detectChanges();
    expect(
      element
        .querySelectorAll('.soul-loadout-slot')[2]
        .querySelector('.soul-channeled-essence-badge'),
    ).not.toBeNull();
    expect(element.querySelector('.soul-channel-essence')).toBeNull();
    expect(
      element.querySelector('.soul-channeled-essence-hint')?.textContent,
    ).toContain('The first Essence in your battle loadout');
    styles.data.set({ selection: { combatStyleId: 'bastion' } });
    fixture.detectChanges();
    expect(element.querySelector('.soul-channeled-essence-badge')).toBeNull();
    expect(element.querySelector('.soul-channeled-essence-hint')).toBeNull();
    styles.data.set({ selection: { combatStyleId: 'conduit' } });
    state.draftSlots.set([null, null, null, null]);
    fixture.detectChanges();
    expect(element.querySelector('.soul-channeled-essence-badge')).toBeNull();
    expect(
      element.querySelector('.soul-channeled-essence-hint')?.textContent,
    ).toContain('Equip an Essence');
    expect(styles.refreshIfDirty).toHaveBeenCalledTimes(1);
  });

  it('keeps the Channeled Essence badge, action and eligibility hint readable in desktop and mobile loadouts', async () => {
    const { element } = await createPage();
    for (const width of [320, 390, 1440]) {
      const frame = document.createElement('iframe');
      frame.style.cssText = `width:${width}px;height:900px;border:0;`;
      document.body.appendChild(frame);
      try {
        const doc = frame.contentDocument!;
        const style = doc.createElement('style');
        style.textContent =
          Array.from(document.styleSheets)
            .flatMap((sheet) =>
              Array.from(sheet.cssRules, (rule) => rule.cssText),
            )
            .join('\n') + '\nhtml, body { margin:0; height:100%; }';
        doc.head.appendChild(style);
        doc.body.appendChild(element.cloneNode(true));
        const panel = doc.querySelector<HTMLElement>('.soul-archive-loadout')!;
        const panelBounds = panel.getBoundingClientRect();
        for (const selector of [
          '.soul-channeled-essence-badge',
          '.soul-channel-essence',
          '.soul-channeled-essence-hint',
        ]) {
          const control = panel.querySelector<HTMLElement>(selector)!;
          const bounds = control.getBoundingClientRect();
          expect(bounds.width)
            .withContext(`${width}: ${selector} visible`)
            .toBeGreaterThan(0);
          expect(bounds.height)
            .withContext(`${width}: ${selector} visible`)
            .toBeGreaterThan(0);
          expect(control.scrollWidth)
            .withContext(`${width}: ${selector} wraps`)
            .toBeLessThanOrEqual(control.clientWidth + 1);
          if (selector === '.soul-channeled-essence-hint') {
            expect(bounds.left).toBeGreaterThanOrEqual(panelBounds.left);
            expect(bounds.right).toBeLessThanOrEqual(panelBounds.right + 1);
          } else {
            const slotBounds = control
              .closest('.soul-loadout-slot')!
              .getBoundingClientRect();
            expect(bounds.left).toBeGreaterThanOrEqual(slotBounds.left);
            expect(bounds.right).toBeLessThanOrEqual(slotBounds.right + 1);
          }
        }
      } finally {
        frame.remove();
      }
    }
  });

  it('renders Creature Focus independently from the Channeled Essence and honors the returned cooldown state', async () => {
    const { fixture, element, state } = await createPage();
    const creature = {
      creatureId: 'creature-1',
      name: 'Ember Wolf',
      killCount: 10,
      firstDefeatedAtUtc: new Date().toISOString(),
      lastDefeatedAtUtc: new Date().toISOString(),
      isCreatureFocus: false,
      creatureFocusSetAtUtc: null,
      creatureFocusTotalDurationSeconds: 120,
      currentCreatureFocusDurationSeconds: 0,
      essences: [
        {
          essenceDefinitionId: 'ember-wolf',
          name: 'Ember Wolf Essence',
          isAbsorbed: false,
          tags: [],
          definition: {} as EssenceDefinitionDto,
        },
      ],
      locations: [],
      tags: [],
    } as CreatureArchiveEntryDto;
    state.creatureArchive.set({
      creatures: [creature],
      canChangeCreatureFocus: false,
    });
    state.activeView.set('creatures');
    fixture.detectChanges();
    expect(element.textContent).toContain('Creature Focus');
    expect(element.textContent).not.toContain('Essence Focus');
    expect(element.querySelector('.soul-channeled-essence-badge')).toBeNull();
    const action = Array.from(
      element.querySelectorAll<HTMLButtonElement>('article button'),
    ).find((button) => button.textContent?.trim() === 'Set Creature Focus')!;
    expect(action.disabled).toBeTrue();
    state.creatureArchive.set({
      creatures: [creature],
      canChangeCreatureFocus: true,
    });
    fixture.detectChanges();
    expect(action.disabled).toBeFalse();
    action.click();
    expect(state.setCreatureFocus).toHaveBeenCalledOnceWith('creature-1');
    state.creatureArchive.set({
      creatures: [{ ...creature, isCreatureFocus: true }],
      canChangeCreatureFocus: false,
    });
    fixture.detectChanges();
    expect(action.textContent?.trim()).toBe('Creature Focus');
    expect(action.disabled).toBeTrue();
    expect(element.textContent).toContain('Ember Wolf has 3×');
  });
});
