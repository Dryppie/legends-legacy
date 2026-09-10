import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { CombatStylesComponent } from '../../../../features/game/character/combat-styles/combat-styles.component';
import { HelpOverlayService } from '../../../../shared/help/help-overlay.service';
import { CombatStyleOverview } from '../../../../shared/models/combat-styles';
import { EventBusService } from '../../client-side/event-bus/event-bus.service';
import { DomainVersionTracker } from '../../real-time/game-realtime/domain-version-tracker.service';
import { StateSyncCoordinator } from '../../real-time/game-realtime/state-sync-coordinator.service';
import { VersionedMutationResult } from '../api.service';
import { CharacterStateService } from '../character/character-state.service';
import { CombatStylesService } from './combat-styles.service';
import { CombatStyleStateService } from './combat-style-state.service';

export function styleOverview(id: string | null = null): CombatStyleOverview {
  return {
    contentVersion: 'test',
    selection: {
      combatStyleId: id,
      refinementId: null,
      upgradeIds: [],
      masteredUpgradeId: null,
    },
    effectiveStyle: null,
    validationIssue: null,
    previewFacts: [],
    styles: ['bastion', 'conduit'].map((styleId, index) => ({
      definition: {
        id: styleId,
        name: styleId,
        kind: index === 0 ? 'Bastion' : 'Conduit',
        description: '',
        tuning: {
          barrierFraction: 0.75,
          barrierPerMasteryLevel: 0.01,
          channeledBaseMultiplier: 0.8,
          channeledPerCharge: 0.2,
          channeledPerMasteryLevel: 0.01,
        },
        openingTechnique: {
          name: index === 0 ? 'Entrenched' : 'Primed Circuit',
          description:
            index === 0
              ? 'Start with 5% maximum Health as Barrier.'
              : 'Start with 1 Charge.',
        },
        refinements: [],
        upgrades: ['prepared-wall', 'hold-the-breach'].map((upgradeId) => ({
          id: upgradeId,
          name: upgradeId,
          description: 'Upgrade effect.',
          masteryDescription: 'Empowered upgrade effect.',
        })),
      },
      level: index === 0 ? 8 : 1,
      currentXp: index === 0 ? 500 : 0,
      xpRequired: 1000,
      upgradeSlots: index === 0 ? 2 : 0,
      refinementId: null,
      upgradeIds: [],
      masteredUpgradeId: null,
    })),
  };
}

describe('CombatStyleStateService', () => {
  let service: CombatStyleStateService;
  let api: jasmine.SpyObj<CombatStylesService>;
  let versions: DomainVersionTracker;
  let logout = signal(false);
  let sync: jasmine.SpyObj<StateSyncCoordinator>;

  beforeEach(() => {
    logout = signal(false);
    api = jasmine.createSpyObj('CombatStylesService', [
      'get',
      'preview',
      'select',
    ]);
    api.get.and.returnValue(of(styleOverview()));
    api.preview.and.callFake((selection) =>
      of({ ...styleOverview(selection.combatStyleId), selection }),
    );
    sync = jasmine.createSpyObj('StateSyncCoordinator', [
      'register',
      'activate',
      'latestRevision',
    ]);
    sync.latestRevision.and.returnValue(0);
    TestBed.configureTestingModule({
      imports: [CombatStylesComponent],
      providers: [
        CombatStyleStateService,
        DomainVersionTracker,
        { provide: CombatStylesService, useValue: api },
        { provide: EventBusService, useValue: { logout } },
        { provide: StateSyncCoordinator, useValue: sync },
        {
          provide: HelpOverlayService,
          useValue: { open: jasmine.createSpy() },
        },
        {
          provide: CharacterStateService,
          useValue: { markOverviewDirty: jasmine.createSpy() },
        },
      ],
    });
    service = TestBed.inject(CombatStyleStateService);
    versions = TestBed.inject(DomainVersionTracker);
    TestBed.flushEffects();
  });

  function invalidateMastery(revision: number) {
    sync.latestRevision.and.returnValue(revision);
    versions.observe({ 'combat-styles': revision });
    const registration = sync.register.calls
      .allArgs()
      .find((args) => args[1] === 'combat-styles')!;
    registration[2]({
      scope: 'combat-styles',
      key: 'combat-styles',
      targetRevision: revision,
    });
  }

  it('updates the open page from mastery notifications while preserving unsaved choices', () => {
    api.get.and.returnValue(of(styleOverview('bastion')));
    const fixture = TestBed.createComponent(CombatStylesComponent);
    fixture.detectChanges();
    service.toggleUpgrade('prepared-wall');

    const progressed = styleOverview('bastion');
    progressed.styles[0].level = 9;
    progressed.styles[0].currentXp = 640;
    api.get.and.returnValue(of(progressed));
    invalidateMastery(1);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('progress')?.value).toBe(640);
    expect(element.querySelector('.header-stats')?.textContent).toContain(
      '640 / 1,000',
    );
    expect(element.querySelector('.mastery-bonus-stat')?.textContent).toContain(
      '+9% Barrier',
    );
    expect(
      element.querySelector('.milestone-mastery-benefit')?.textContent,
    ).toContain('Mastery 9: +9% converted Barrier');
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(service.edited()).toBeTrue();

    const mastered = styleOverview('bastion');
    mastered.styles[0] = {
      ...mastered.styles[0],
      level: 10,
      currentXp: 0,
    };
    api.get.and.returnValue(of(mastered));
    invalidateMastery(2);
    fixture.detectChanges();

    expect(element.querySelector('.header-stats')?.textContent).toContain(
      'Mastered',
    );
    expect(element.querySelector('.mastery-bonus-stat')?.textContent).toContain(
      '+10% Barrier',
    );
    expect(element.querySelector('.milestone-panel')?.textContent).toContain(
      '10 / 10 · mastered',
    );
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(api.get).toHaveBeenCalledTimes(3);
  });

  it('catches up when mastery changes during an in-flight refresh', () => {
    api.get.and.returnValue(of(styleOverview('bastion')));
    service.refresh();
    service.toggleUpgrade('prepared-wall');
    const stale = new Subject<CombatStyleOverview>();
    const current = new Subject<CombatStyleOverview>();
    api.get.and.returnValues(stale, current);
    service.refresh();
    invalidateMastery(1);
    service.refreshIfDirty();
    expect(api.get).toHaveBeenCalledTimes(2);

    stale.next(styleOverview('bastion'));
    expect(api.get).toHaveBeenCalledTimes(3);
    expect(service.loading()).toBeTrue();

    const progressed = styleOverview('bastion');
    progressed.styles[0].currentXp = 640;
    current.next(progressed);
    expect(service.selected()?.currentXp).toBe(640);
    expect(service.loading()).toBeFalse();
    expect(service.dirty()).toBeFalse();
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(api.preview.calls.mostRecent().args[0].upgradeIds).toEqual([
      'prepared-wall',
    ]);
  });

  it('keeps independent earned levels and allows Conduit without loadout data', () => {
    service.refresh();
    service.chooseStyle('conduit');
    expect(service.selected()?.level).toBe(1);
    expect(service.selected()?.currentXp).toBe(0);
    expect(service.canSave()).toBeTrue();
    expect(api.preview.calls.mostRecent().args[0]).toEqual({
      combatStyleId: 'conduit',
      refinementId: null,
      upgradeIds: [],
      masteredUpgradeId: null,
    });
    service.chooseStyle('bastion');
    expect(service.selected()?.level).toBe(8);
    expect(service.selected()?.currentXp).toBe(500);
  });

  it('uses one global selection and makes a level-one style immediately available', () => {
    const data = styleOverview();
    data.styles[0] = {
      ...data.styles[0],
      level: 1,
      currentXp: 0,
      upgradeSlots: 0,
    };
    api.get.and.returnValue(of(data));
    service.refresh();
    service.chooseStyle('bastion');
    expect(api.get).toHaveBeenCalledOnceWith();
    expect(service.canSave()).toBeTrue();
    expect(service.draft()).toEqual({
      combatStyleId: 'bastion',
      refinementId: null,
      upgradeIds: [],
      masteredUpgradeId: null,
    });
    service.toggleUpgrade('prepared-wall');
    expect(service.draft().upgradeIds).toEqual([]);
  });

  it('saves an empty style selection and clears pending edits', () => {
    api.get.and.returnValue(of(styleOverview('bastion')));
    const updated = styleOverview();
    api.select.and.returnValue(
      of({ data: updated, domainVersions: { 'combat-styles': 1 } }),
    );
    service.refresh();
    service.chooseStyle(null);
    expect(service.canSave()).toBeTrue();
    service.save();
    expect(api.select).toHaveBeenCalledOnceWith(updated.selection);
    expect(service.data()?.selection.combatStyleId).toBeNull();
    expect(service.edited()).toBeFalse();
  });

  it('keeps equipped indicators tied to the saved style throughout browsing, failed saves, discard and unequipping', () => {
    const initial = styleOverview('bastion');
    initial.styles = initial.styles.map((entry) => ({
      ...entry,
      definition: {
        ...entry.definition,
        name: entry.definition.id === 'bastion' ? 'Bastion' : 'Conduit',
      },
    }));
    api.get.and.returnValue(of(initial));
    api.preview.and.callFake((selection) => of({ ...initial, selection }));
    const failedSave = new Subject<
      VersionedMutationResult<CombatStyleOverview>
    >();
    const successfulSave = new Subject<
      VersionedMutationResult<CombatStyleOverview>
    >();
    const emptySave = new Subject<
      VersionedMutationResult<CombatStyleOverview>
    >();
    api.select.and.returnValues(failedSave, successfulSave, emptySave);
    const fixture = TestBed.createComponent(CombatStylesComponent);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const expectEquipped = (id: string | null, name: string) => {
      expect(service.data()?.selection.combatStyleId).toBe(id);
      expect(fixture.componentInstance.equippedStyleName()).toBe(name);
      expect(
        element.querySelector('.equipped-style-status')?.textContent,
      ).toContain(name);
      expect(element.querySelector('.navigation-tab-status')).toBeNull();
    };
    expectEquipped('bastion', 'Bastion');
    const expectPendingStyle = (message: string) => {
      expect(element.querySelector('.preview-notice')?.textContent).toContain(
        message,
      );
      expect(
        element.querySelector('.battle-notice-label')?.textContent,
      ).toContain('Preview only');
      const selectedTab = element.querySelector(
        '[role="tab"][aria-selected="true"]',
      )!;
      expect(getComputedStyle(selectedTab).boxShadow).toContain('inset');
    };

    service.chooseStyle('conduit');
    fixture.detectChanges();
    expect(service.draft().combatStyleId).toBe('conduit');
    expect(service.canSave()).toBeTrue();
    expectEquipped('bastion', 'Bastion');
    expectPendingStyle('Conduit is not your selected Combat Style.');
    expectPendingStyle('Bastion stays in battle until you equip this.');
    const equipButton = element.querySelector<HTMLButtonElement>(
      '.battle-action button',
    )!;
    expect(equipButton.textContent).toContain('Take Conduit into battle');
    expect(equipButton.disabled).toBeFalse();
    expect(api.select).not.toHaveBeenCalled();
    equipButton.click();
    fixture.detectChanges();
    expect(service.busy()).toBeTrue();
    expect(equipButton.disabled).toBeTrue();
    expectEquipped('bastion', 'Bastion');
    failedSave.error({ errorMessage: 'The style could not be saved.' });
    fixture.detectChanges();
    expect(service.busy()).toBeFalse();
    expect(service.draft().combatStyleId).toBe('conduit');
    expectEquipped('bastion', 'Bastion');
    expectPendingStyle('Conduit is not your selected Combat Style.');

    service.save();
    fixture.detectChanges();
    expectEquipped('bastion', 'Bastion');
    const savedConduit = { ...initial, selection: { ...service.draft() } };
    successfulSave.next({
      data: savedConduit,
      domainVersions: { 'combat-styles': 1 },
    });
    fixture.detectChanges();
    expect(service.edited()).toBeFalse();
    expectEquipped('conduit', 'Conduit');
    expect(element.querySelector('.preview-notice')).toBeNull();
    expect(element.querySelector('.battle-action')).toBeNull();
    expect(element.querySelector('.battle-notice')?.textContent).toContain(
      'Conduit is in your slot.',
    );

    service.chooseStyle(null);
    fixture.detectChanges();
    expect(service.draft().combatStyleId).toBeNull();
    expectEquipped('conduit', 'Conduit');
    expectPendingStyle('Conduit stays in battle until you unequip it.');
    expect(
      element.querySelector('.battle-action button')?.textContent,
    ).toContain('Unequip Combat Style');
    service.resetDraft();
    fixture.detectChanges();
    expect(service.draft().combatStyleId).toBe('conduit');
    expectEquipped('conduit', 'Conduit');
    expect(element.querySelector('.preview-notice')).toBeNull();

    service.chooseStyle(null);
    service.save();
    fixture.detectChanges();
    expect(service.busy()).toBeTrue();
    expectEquipped('conduit', 'Conduit');
    emptySave.next({
      data: { ...savedConduit, selection: { ...service.draft() } },
      domainVersions: { 'combat-styles': 2 },
    });
    fixture.detectChanges();
    expectEquipped(null, 'None');
    expect(element.querySelector('.battle-notice')?.textContent).toContain(
      'No Combat Style is equipped.',
    );
    service.chooseStyle('bastion');
    fixture.detectChanges();
    expectEquipped(null, 'None');
    expectPendingStyle('Your slot stays empty until you equip this.');
    service.resetDraft();
    fixture.detectChanges();
    expect(service.draft().combatStyleId).toBeNull();
    expectEquipped(null, 'None');
  });

  it('unlocks openings and mastery from live XP updates without losing a pending upgrade choice', () => {
    const initial = styleOverview('bastion');
    initial.styles[0] = { ...initial.styles[0], level: 6, upgradeSlots: 1 };
    api.get.and.returnValue(of(initial));
    const fixture = TestBed.createComponent(CombatStylesComponent);
    fixture.detectChanges();
    service.toggleUpgrade('prepared-wall');
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.opening-technique')!.textContent).toContain(
      'Unlocks at level 7',
    );
    service.changeMastery('prepared-wall');
    expect(service.draft().masteredUpgradeId).toBeNull();

    const opened = styleOverview('bastion');
    opened.styles[0] = { ...opened.styles[0], level: 7, upgradeSlots: 1 };
    api.get.and.returnValue(of(opened));
    invalidateMastery(1);
    fixture.detectChanges();
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);

    const progressed = styleOverview('bastion');
    progressed.styles[0].level = 9;
    api.get.and.returnValue(of(progressed));
    invalidateMastery(2);
    fixture.detectChanges();
    const radio = element.querySelector<HTMLInputElement>(
      '.desktop-configuration .mastery-options input',
    )!;
    expect(radio.matches(':disabled')).toBeFalse();
    radio.click();
    fixture.detectChanges();
    expect(service.draft().masteredUpgradeId).toBe('prepared-wall');
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(api.preview.calls.mostRecent().args[0].masteredUpgradeId).toBe(
      'prepared-wall',
    );
    expect(service.edited()).toBeTrue();

    invalidateMastery(3);
    fixture.detectChanges();
    expect(service.draft().masteredUpgradeId).toBe('prepared-wall');
    expect(radio.checked).toBeTrue();
  });

  it('saves one mastery with the existing choices and restores it through discard and style switches', () => {
    const initial = styleOverview('bastion');
    initial.styles[0].level = 9;
    api.get.and.returnValue(of(initial));
    service.refresh();
    service.toggleUpgrade('prepared-wall');
    service.toggleUpgrade('hold-the-breach');
    service.changeMastery('prepared-wall');
    const updated = styleOverview('bastion');
    updated.selection = { ...service.draft() };
    updated.styles[0] = {
      ...updated.styles[0],
      level: 9,
      upgradeIds: [...updated.selection.upgradeIds],
      masteredUpgradeId: 'prepared-wall',
    };
    api.select.and.returnValue(
      of({ data: updated, domainVersions: { 'combat-styles': 1 } }),
    );
    service.save();
    expect(api.select).toHaveBeenCalledOnceWith(
      jasmine.objectContaining({
        combatStyleId: 'bastion',
        upgradeIds: ['prepared-wall', 'hold-the-breach'],
        masteredUpgradeId: 'prepared-wall',
      }),
    );
    expect(service.edited()).toBeFalse();
    service.changeMastery('hold-the-breach');
    expect(service.draft().masteredUpgradeId).toBe('hold-the-breach');
    service.resetDraft();
    expect(service.draft().masteredUpgradeId).toBe('prepared-wall');
    service.chooseStyle('conduit');
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.chooseStyle('bastion');
    expect(service.draft().masteredUpgradeId).toBe('prepared-wall');
    expect(service.draft().upgradeIds).toEqual([
      'prepared-wall',
      'hold-the-breach',
    ]);
    service.chooseStyle(null);
    expect(service.draft().masteredUpgradeId).toBeNull();
  });

  it('clears mastery when its upgrade is removed, but preserves it when a different upgrade is removed', () => {
    const initial = styleOverview('bastion');
    initial.styles[0].level = 9;
    api.get.and.returnValue(of(initial));
    service.refresh();
    service.toggleUpgrade('prepared-wall');
    service.toggleUpgrade('hold-the-breach');
    service.changeMastery('prepared-wall');
    service.toggleUpgrade('hold-the-breach');
    expect(service.draft().masteredUpgradeId).toBe('prepared-wall');
    service.toggleUpgrade('prepared-wall');
    expect(service.draft().masteredUpgradeId).toBeNull();
    expect(service.preview()?.selection.masteredUpgradeId).toBeNull();
    service.toggleUpgrade('prepared-wall');
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.changeMastery('prepared-wall');
    service.changeMastery(null);
    expect(service.draft().masteredUpgradeId).toBeNull();
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
  });

  it('blocks invalid, unequipped and locked mastery choices even when the preview has no validation issue', () => {
    api.get.and.returnValue(of(styleOverview('bastion')));
    service.refresh();
    service.toggleUpgrade('prepared-wall');
    service.changeMastery('prepared-wall');
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.draft.update((selection) => ({
      ...selection,
      masteredUpgradeId: 'prepared-wall',
    }));
    expect(service.masteryInvalid()).toBeTrue();
    expect(service.canSave()).toBeFalse();
    service.changeMastery(null);
    service.data.update((data) => ({
      ...data!,
      styles: data!.styles.map((entry) => ({ ...entry, level: 9 })),
    }));
    service.changeMastery('hold-the-breach');
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.draft.update((selection) => ({
      ...selection,
      upgradeIds: ['unknown'],
      masteredUpgradeId: 'unknown',
    }));
    expect(service.masteryInvalid()).toBeTrue();
    expect(service.canSave()).toBeFalse();
    service.save();
    expect(api.select).not.toHaveBeenCalled();
    service.draft.update((selection) => ({
      ...selection,
      upgradeIds: ['prepared-wall'],
      masteredUpgradeId: null,
    }));
    service.busy.set(true);
    service.changeMastery('prepared-wall');
    expect(service.draft().masteredUpgradeId).toBeNull();
  });

  it('normalizes missing mastery from legacy responses to an empty choice', () => {
    const legacy = styleOverview('bastion');
    delete legacy.selection.masteredUpgradeId;
    delete legacy.styles[0].masteredUpgradeId;
    api.get.and.returnValue(of(legacy));
    service.refresh();
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.chooseStyle('bastion');
    expect(service.draft().masteredUpgradeId).toBeNull();
    service.resetDraft();
    expect(service.draft().masteredUpgradeId).toBeNull();
  });

  it('ignores previews for a previous draft', () => {
    service.refresh();
    const first = new Subject<CombatStyleOverview>();
    const second = new Subject<CombatStyleOverview>();
    api.preview.and.returnValues(first, second);
    service.chooseStyle('bastion');
    expect(first.observed).toBeTrue();
    service.chooseStyle('conduit');
    expect(first.observed).toBeFalse();
    expect(second.observed).toBeTrue();
    expect(service.preview()).toBeNull();
    second.next(styleOverview('conduit'));
    first.next(styleOverview('bastion'));
    expect(service.preview()?.selection.combatStyleId).toBe('conduit');
  });

  it('rejects an old fetch after a committed mutation response', () => {
    service.refresh();
    service.chooseStyle('bastion');
    const old = new Subject<CombatStyleOverview>();
    const mutation = new Subject<
      VersionedMutationResult<CombatStyleOverview>
    >();
    api.select.and.returnValue(mutation);
    service.save();
    api.get.and.returnValue(old);
    service.refresh();
    mutation.next({
      data: styleOverview('bastion'),
      domainVersions: { 'combat-styles': 2 },
    });
    old.next(styleOverview('conduit'));
    expect(service.data()?.selection.combatStyleId).toBe('bastion');
  });

  it('keeps the displayed preview during invalidation but blocks saving until it is current', () => {
    service.refresh();
    service.chooseStyle('bastion');
    service.toggleUpgrade('prepared-wall');
    const displayed = service.preview();
    const registration = sync.register.calls
      .allArgs()
      .find((args) => args[1] === 'combat-styles')!;
    registration[2]({ targetRevision: 5 } as any);
    expect(service.preview()).toBe(displayed);
    expect(service.dirty()).toBeTrue();
    expect(service.canSave()).toBeFalse();
    service.refresh();
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(service.canSave()).toBeTrue();
  });

  it('retains the current example during edits and only replaces it when the new preview arrives', () => {
    const initial = styleOverview('bastion');
    initial.previewFacts = [
      {
        label: '200 healing received',
        value: '50 Health + 162 Barrier',
        condition: null,
      },
    ];
    api.get.and.returnValue(of(initial));
    service.refresh();
    const pending = new Subject<CombatStyleOverview>();
    api.preview.and.returnValue(pending);

    service.toggleUpgrade('prepared-wall');
    expect(service.draft().upgradeIds).toEqual(['prepared-wall']);
    expect(service.preview()).toBe(initial);
    expect(service.previewing()).toBeTrue();
    expect(service.loading()).toBeFalse();
    expect(service.canSave()).toBeFalse();
    expect(api.get).toHaveBeenCalledTimes(1);

    const updated = {
      ...initial,
      selection: service.draft(),
      previewFacts: [
        ...initial.previewFacts,
        {
          label: 'Prepared Wall',
          value: '+7.5% flat increase · +15 Barrier',
          condition:
            'Gain extra Barrier when you receive healing at 80% Health or higher.',
        },
      ],
    };
    pending.next(updated);
    expect(service.preview()).toBe(updated);
    expect(service.previewing()).toBeFalse();
    expect(service.canSave()).toBeTrue();
  });

  it('uses overview, discard and save response previews without sending duplicate requests', () => {
    const initial = styleOverview('bastion');
    initial.styles[0].level = 9;
    initial.selection.upgradeIds = ['prepared-wall'];
    initial.styles[0].upgradeIds = ['prepared-wall'];
    api.get.and.returnValue(of(initial));
    service.refresh();
    expect(service.preview()).toBe(initial);
    expect(api.preview).not.toHaveBeenCalled();

    service.changeMastery('prepared-wall');
    expect(api.preview).toHaveBeenCalledTimes(1);
    service.resetDraft();
    expect(service.preview()).toBe(initial);
    expect(api.preview).toHaveBeenCalledTimes(1);
    service.changeMastery('prepared-wall');
    const saved = { ...initial, selection: service.draft() };
    api.select.and.returnValue(
      of({ data: saved, domainVersions: { 'combat-styles': 1 } }),
    );
    service.save();
    expect(service.preview()).toBe(saved);
    expect(api.preview).toHaveBeenCalledTimes(2);
    service.changeMastery('prepared-wall');
    expect(api.preview).toHaveBeenCalledTimes(2);
    expect(service.edited()).toBeFalse();
  });

  it('preserves the displayed example on preview failure without allowing stale validation to save', () => {
    const initial = styleOverview('bastion');
    api.get.and.returnValue(of(initial));
    service.refresh();
    api.preview.and.returnValue(
      throwError(() => ({ message: 'Preview unavailable.' })),
    );
    service.toggleUpgrade('prepared-wall');
    expect(service.preview()).toBe(initial);
    expect(service.previewError()).toBe('Preview unavailable.');
    expect(service.previewing()).toBeFalse();
    expect(service.canSave()).toBeFalse();
    service.save();
    expect(api.select).not.toHaveBeenCalled();

    service.resetDraft();
    expect(service.previewError()).toBeNull();
    expect(service.canSave()).toBeTrue();
  });

  it('cancels an obsolete preview when discarding, invalidating or logging out', () => {
    const initial = styleOverview('bastion');
    api.get.and.returnValue(of(initial));
    service.refresh();
    const pending = new Subject<CombatStyleOverview>();
    api.preview.and.returnValue(pending);
    service.toggleUpgrade('prepared-wall');
    expect(pending.observed).toBeTrue();
    service.resetDraft();
    expect(pending.observed).toBeFalse();
    expect(service.preview()).toBe(initial);

    service.toggleUpgrade('prepared-wall');
    expect(pending.observed).toBeTrue();
    service.invalidate();
    expect(pending.observed).toBeFalse();
    expect(service.preview()).toBe(initial);
    expect(service.canSave()).toBeFalse();

    service.refresh();
    expect(pending.observed).toBeTrue();
    logout.set(true);
    TestBed.flushEffects();
    expect(pending.observed).toBeFalse();
    pending.next(initial);
    expect(service.preview()).toBeNull();
  });

  it('does not apply mutations or requests returned after logout', () => {
    service.refresh();
    service.chooseStyle('bastion');
    const mutation = new Subject<
      VersionedMutationResult<CombatStyleOverview>
    >();
    api.select.and.returnValue(mutation);
    service.save();
    logout.set(true);
    TestBed.flushEffects();
    mutation.next({
      data: styleOverview('bastion'),
      domainVersions: { 'combat-styles': 2 },
    });
    expect(service.data()).toBeNull();
    expect(service.preview()).toBeNull();
    expect(service.busy()).toBeFalse();
  });

  it('preserves saved state and draft when a mutation fails', () => {
    service.refresh();
    service.chooseStyle('bastion');
    api.select.and.returnValue(
      throwError(() => ({ errorMessage: 'The activity is committed.' })),
    );
    service.save();
    expect(service.data()?.selection.combatStyleId).toBeNull();
    expect(service.draft().combatStyleId).toBe('bastion');
    expect(service.error()).toBe('The activity is committed.');
  });

  it('refreshes rather than applying a mutation older than the observed revision', () => {
    service.refresh();
    service.chooseStyle('bastion');
    versions.observe({ 'combat-styles': 5 });
    api.get.and.returnValue(of(styleOverview('conduit')));
    api.select.and.returnValue(
      of({
        data: styleOverview('bastion'),
        domainVersions: { 'combat-styles': 3 },
      }),
    );
    service.save();
    expect(service.data()?.selection.combatStyleId).toBe('conduit');
  });
});
