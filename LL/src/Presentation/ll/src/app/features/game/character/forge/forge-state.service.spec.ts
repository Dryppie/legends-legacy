import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';
import { ForgeStateService } from './forge-state.service';
import { EquipmentProgressionService } from '../../../../core/services/api/equipment/equipment-progression.service';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { EquipmentService } from '../../../../core/services/api/equipment/equipment.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import {
  EquipmentAccess,
  ForgeQuote,
} from '../../../../shared/models/equipment-progression';
import { EquipmentInstance } from '../../../../shared/models/item';
import { EquipmentSlotType } from '../../../../shared/models/Dtos/equipment-slots/equipmentSlot';
import { ForgeComponent } from './forge.component';
import { EquipmentType } from '../../../../shared/models/enums/equipmentType';
import {
  ActivatedRoute,
  convertToParamMap,
  provideRouter,
} from '@angular/router';

describe('Equipment progression Forge workflow', () => {
  let api: jasmine.SpyObj<EquipmentProgressionService>;
  let inventory: jasmine.SpyObj<InventoryService>;
  let equipment: jasmine.SpyObj<EquipmentService>;
  let state: ForgeStateService;
  const storageKey = 'll.forge.pending.forge-test-character';
  const access: EquipmentAccess = {
    starterAcquisitionEnabled: true,
    forgeEnabled: true,
    protectedAcquisitionEnabled: true,
    baselineRecoveryEnabled: true,
    ordinaryAcquisitionEnabled: true,
    starters: [],
  };
  const quote: ForgeQuote = {
    operationId: 'quoted-operation',
    token: 'quoted-token',
    expiresAtUtc: '2026-09-02T20:00:00Z',
    request: { kind: 'ImproveRank', itemInstanceId: 'equipment-id' },
    canExecute: true,
    unavailableReason: null,
    before: null,
    after: null,
    scrapCost: 5,
    cinderCost: 250,
    scrapReturned: 0,
    usesFreeApplication: false,
    isNoOp: false,
    equippedImpact: null,
  };

  beforeEach(() => {
    sessionStorage.removeItem(storageKey);
    api = jasmine.createSpyObj<EquipmentProgressionService>(
      'EquipmentProgressionService',
      [
        'access',
        'starters',
        'ordinary',
        'sources',
        'recovery',
        'plainRecovery',
        'styles',
        'preview',
        'mutate',
      ],
    );
    api.access.and.returnValue(of(access));
    api.starters.and.returnValue(of([]));
    api.ordinary.and.returnValue(
      of([
        {
          poolId: 'shenic',
          rulesVersion: 'v1',
          regionName: 'Shenic',
          equipmentTier: 1,
          hasEnteredRegion: true,
          selectedDefinitionId: null,
          plainVictories: 0,
          requiredPlainVictories: 360,
          selectedSigilFamilyId: null,
          sigilVictories: 0,
          requiredSigilVictories: 4320,
          scrapRemainder: 0,
          discoveryChance: 0.0003,
          targets: [],
          sigils: [],
        },
      ]),
    );
    api.sources.and.returnValue(of([]));
    api.recovery.and.returnValue(of([]));
    api.plainRecovery.and.returnValue(of([]));
    api.styles.and.returnValue(of([]));
    api.preview.and.returnValue(of(quote));
    api.mutate.and.returnValue(of({}));
    inventory = jasmine.createSpyObj<InventoryService>('InventoryService', [
      'getInventory',
    ]);
    inventory.getInventory.and.returnValue(of({ inventoryItems: [] }));
    equipment = jasmine.createSpyObj<EquipmentService>('EquipmentService', [
      'getEquipment',
    ]);
    equipment.getEquipment.and.returnValue(of([]));
    TestBed.configureTestingModule({
      providers: [
        ForgeStateService,
        provideRouter([]),
        { provide: EquipmentProgressionService, useValue: api },
        { provide: InventoryService, useValue: inventory },
        { provide: EquipmentService, useValue: equipment },
        {
          provide: CharacterStateService,
          useValue: {
            currentCharacterId: signal('forge-test-character'),
            currentCharacter: signal({ cinders: 500 }),
          },
        },
      ],
    });
    TestBed.overrideComponent(ForgeComponent, { set: { providers: [] } });
    state = TestBed.inject(ForgeStateService);
  });
  afterEach(() => sessionStorage.removeItem(storageKey));

  it('opens quest reward links and retries earned recovery with the original operation', async () => {
    spyOnProperty(
      TestBed.inject(ActivatedRoute).snapshot,
      'queryParamMap',
      'get',
    ).and.returnValue(convertToParamMap({ tab: 'rewards' }));
    api.plainRecovery.and.returnValue(
      of([
        {
          definitionId: 'plain.dagger',
          tier: 1,
          name: 'Dagger',
          entitled: 3,
          owned: 2,
          missing: 1,
        },
      ]),
    );
    const fixture = TestBed.createComponent(ForgeComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.componentInstance.activeTab).toBe('rewards');
    expect(fixture.nativeElement.textContent).toContain(
      'Recover earned plain equipment',
    );
    api.mutate.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 0 })),
    );
    await state.recoverPlain('plain.dagger', 1);
    const original = api.mutate.calls.mostRecent().args;
    expect(original[0]).toBe('equipmentacquisition/plain-recovery');
    expect(original[1]).toEqual(
      jasmine.objectContaining({
        definitionId: 'plain.dagger',
        tier: 1,
        operationId: jasmine.any(String),
      }),
    );
    api.mutate.and.returnValue(of({}));
    await state.retry();
    expect(api.mutate.calls.mostRecent().args).toEqual(original);
    expect(state.pending()).toBeNull();
  });

  it('renders saved ordinary choices and submits the edited pair together', async () => {
    api.access.and.returnValue(of({ ...access, forgeEnabled: false }));
    api.ordinary.and.returnValue(
      of([
        {
          poolId: 'shenic',
          rulesVersion: 'v1',
          regionName: 'Shenic',
          equipmentTier: 1,
          hasEnteredRegion: true,
          selectedDefinitionId: 'dagger',
          plainVictories: 30,
          requiredPlainVictories: 360,
          selectedSigilFamilyId: 'goblin',
          sigilVictories: 120,
          requiredSigilVictories: 4320,
          scrapRemainder: 0,
          discoveryChance: 0.0003,
          targets: [
            {
              definitionId: 'dagger',
              name: 'Dagger',
              equipmentType: EquipmentType.OneHanded,
              stats: {},
            },
          ],
          sigils: [
            {
              familyId: 'goblin',
              itemBaseId: 'sigil',
              canSelect: true,
              unavailableReason: null,
            },
          ],
        },
      ]),
    );
    const fixture = TestBed.createComponent(ForgeComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    await fixture.whenStable();
    const plain = fixture.nativeElement.querySelector(
      '#ordinary-plain',
    ) as HTMLSelectElement;
    const sigil = fixture.nativeElement.querySelector(
      '#ordinary-sigil',
    ) as HTMLSelectElement;
    expect(plain.value).toBe('dagger');
    expect(sigil.value).toBe('goblin');
    plain.value = '';
    plain.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const save = [...fixture.nativeElement.querySelectorAll('button')].find(
      (x: any) => x.textContent.includes('Save both choices'),
    ) as HTMLButtonElement;
    save.click();
    await fixture.whenStable();
    expect(api.mutate.calls.mostRecent().args).toEqual([
      'equipmentacquisition/ordinary',
      jasmine.objectContaining({
        definitionId: null,
        sigilFamilyId: 'goblin',
      }),
    ]);
  });

  it('shows regional stats and keeps the selected pool in an uncertain request', async () => {
    await state.initialize();
    const common = state.ordinary()!;
    state.selectedOrdinaryPoolId.set('');
    api.access.and.returnValue(of({ ...access, forgeEnabled: false }));
    const target = {
      definitionId: 'plain.staff',
      name: 'Staff',
      equipmentType: EquipmentType.TwoHanded,
      stats: { Health: 135 },
    };
    api.ordinary.and.returnValue(
      of([
        { ...common, targets: [{ ...target, stats: { Health: 100 } }] },
        {
          ...common,
          poolId: 'meran',
          regionName: 'Meran',
          equipmentTier: 2,
          targets: [target],
          selectedDefinitionId: target.definitionId,
        },
      ]),
    );
    const fixture = TestBed.createComponent(ForgeComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    const component = fixture.componentInstance;
    expect(component.state.ordinary()?.poolId).toBe('meran');
    expect(component.starterStats(target.definitionId, true)).toContain('135');
    await fixture.whenStable();
    fixture.detectChanges();
    const region = fixture.nativeElement.querySelector(
      '#ordinary-region',
    ) as HTMLSelectElement;
    expect(region.value).toBe('meran');
    region.value = 'shenic';
    region.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    await fixture.whenStable();
    expect(component.starterStats(target.definitionId, true)).toContain('100');
    api.mutate.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );
    await component.state.selectOrdinary(target.definitionId, '');
    fixture.detectChanges();
    await fixture.whenStable();
    expect(
      (
        fixture.nativeElement.querySelector(
          '#ordinary-region',
        ) as HTMLSelectElement
      ).disabled,
    ).toBeTrue();
    const request = api.mutate.calls.mostRecent().args;
    expect(request[1]).toEqual(jasmine.objectContaining({ poolId: 'shenic' }));
    api.mutate.and.returnValue(of({}));
    await component.state.retry();
    expect(api.mutate.calls.mostRecent().args).toEqual(request);
  });

  it('does not fetch disabled operations or inventory when all capabilities are off', async () => {
    api.access.and.returnValue(
      of({
        ...access,
        starterAcquisitionEnabled: false,
        forgeEnabled: false,
        protectedAcquisitionEnabled: false,
        baselineRecoveryEnabled: false,
        ordinaryAcquisitionEnabled: false,
      }),
    );
    await state.initialize();
    expect(state.enabled()).toBeFalse();
    expect(api.starters).not.toHaveBeenCalled();
    expect(api.ordinary).not.toHaveBeenCalled();
    expect(api.sources).not.toHaveBeenCalled();
    expect(api.recovery).not.toHaveBeenCalled();
    expect(inventory.getInventory).not.toHaveBeenCalled();
  });

  it('previews without mutation and requires another confirmation for a refreshed conflict quote', async () => {
    await state.initialize();
    await state.preview(quote.request);
    expect(api.mutate).not.toHaveBeenCalled();
    const fresh = { ...quote, token: 'new-price', cinderCost: 300 };
    api.mutate.and.returnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { detail: 'Price changed', freshQuote: fresh },
          }),
      ),
    );
    await state.confirmQuote();
    expect(api.mutate).toHaveBeenCalledTimes(1);
    expect(state.quote()?.cinderCost).toBe(300);
    expect(state.pending()).toBeNull();
    api.mutate.and.returnValue(
      of({ outcome: { scrapSpent: 5, cindersSpent: 300, scrapReturned: 0 } }),
    );
    await state.confirmQuote();
    expect(api.mutate.calls.mostRecent().args).toEqual([
      'forge/rank',
      jasmine.objectContaining({
        operationId: quote.operationId,
        quoteToken: 'new-price',
        itemInstanceId: 'equipment-id',
      }),
    ]);
    expect(state.quote()).toBeNull();
  });

  it('retains the exact mutation through an unknown outcome and page recreation', async () => {
    await state.initialize();
    await state.preview(quote.request);
    api.mutate.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 0 })),
    );
    await state.confirmQuote();
    const original = api.mutate.calls.mostRecent().args;
    expect(state.locked()).toBeTrue();
    await state.selectOrdinary('another-target', 'goblin');
    expect(api.mutate).toHaveBeenCalledTimes(1);
    const reopened = TestBed.runInInjectionContext(
      () => new ForgeStateService(),
    );
    await reopened.initialize();
    expect(reopened.quote()?.token).toBe('quoted-token');
    api.mutate.and.returnValue(
      of({ outcome: { scrapSpent: 5, cindersSpent: 250, scrapReturned: 0 } }),
    );
    await reopened.retry();
    expect(api.mutate.calls.mostRecent().args).toEqual(original);
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('reuses a target selection receipt on retry and sends both pause choices explicitly', async () => {
    await state.initialize();
    api.mutate.and.returnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );
    await state.selectOrdinary('', '');
    const original = api.mutate.calls.mostRecent().args;
    expect(original).toEqual([
      'equipmentacquisition/ordinary',
      jasmine.objectContaining({
        operationId: jasmine.any(String),
        definitionId: null,
        sigilFamilyId: null,
      }),
    ]);
    api.mutate.and.returnValue(of({}));
    await state.retry();
    expect(api.mutate.calls.mostRecent().args).toEqual(original);
  });

  it('clears a successful request even when the following inventory refresh fails', async () => {
    await state.initialize();
    inventory.getInventory.and.returnValue(
      throwError(() => new Error('Refresh failed')),
    );
    await state.recover('FirstWeapon');
    expect(state.pending()).toBeNull();
    expect(state.message()).toContain('restored');
    expect(state.error()).toBe('Refresh failed');
    await state.retry();
    expect(api.mutate).toHaveBeenCalledTimes(1);
  });

  it('deduplicates a two-handed item occupying both slots and preserves its equipped status', async () => {
    const item = { id: 'staff', progression: { rank: 0 } } as EquipmentInstance;
    inventory.getInventory.and.returnValue(
      of({ inventoryItems: [{ id: 'row', itemInstance: item, quantity: 1 }] }),
    );
    equipment.getEquipment.and.returnValue(
      of([
        {
          id: 'main',
          iconPath: '',
          equipmentSlotType: EquipmentSlotType.MainHand,
          equipmentInstance: item,
        },
        {
          id: 'off',
          iconPath: '',
          equipmentSlotType: EquipmentSlotType.OffHand,
          equipmentInstance: item,
        },
      ]),
    );
    await state.initialize();
    expect(state.equipment().length).toBe(1);
    expect(state.selectedItem()?.equipped).toBeTrue();
  });

  it('renders independent armor and legal duplicate one-handed choices with explicit kit confirmation', async () => {
    api.access.and.returnValue(
      of({
        ...access,
        forgeEnabled: false,
        starters: [
          {
            kind: 'FirstWeapon',
            canClaim: true,
            unavailableReason: null,
            grant: null,
          },
        ],
      }),
    );
    api.starters.and.returnValue(
      of([
        {
          definitionId: 'helm',
          name: 'Heavy Helm',
          equipmentType: EquipmentType.Head,
          stats: {},
        },
        {
          definitionId: 'vest',
          name: 'Light Vest',
          equipmentType: EquipmentType.Chest,
          stats: {},
        },
        {
          definitionId: 'pants',
          name: 'Cloth Pants',
          equipmentType: EquipmentType.Legs,
          stats: {},
        },
        {
          definitionId: 'dagger',
          name: 'Dagger',
          equipmentType: EquipmentType.OneHanded,
          stats: {},
        },
      ]),
    );
    const fixture = TestBed.createComponent(ForgeComponent);
    fixture.detectChanges();
    await fixture.whenStable();
    const component = fixture.componentInstance;
    component.hands = 'pair';
    component.mainHand = component.offHand = 'dagger';
    component.kit = { Head: 'helm', Chest: 'vest', Legs: 'pants' };
    fixture.detectChanges();
    const buttons = [
      ...fixture.nativeElement.querySelectorAll('button'),
    ] as HTMLButtonElement[];
    const review = buttons.find((x) =>
      x.textContent?.includes('Review starter kit'),
    )!;
    expect(review.disabled).toBeFalse();
    review.click();
    fixture.detectChanges();
    expect(api.mutate).not.toHaveBeenCalled();
    const confirm = [...fixture.nativeElement.querySelectorAll('button')].find(
      (x: any) => x.textContent?.includes('Confirm starter kit'),
    ) as HTMLButtonElement;
    confirm.click();
    await fixture.whenStable();
    expect(api.mutate.calls.mostRecent().args).toEqual([
      'equipment/starter-claim',
      {
        kind: 'FirstWeapon',
        definitionIds: ['helm', 'vest', 'pants', 'dagger', 'dagger'],
      },
    ]);
  });
});
