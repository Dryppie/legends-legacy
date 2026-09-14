import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { EssenceStateService } from '../../../../../core/services/api/essences/essence-state.service';
import { Essence } from '../../../../../shared/models/essence';
import { InventoryItem } from '../../../../../shared/models/inventoryItem';
import { EssencesAbsorbComponent } from './essences-absorb.component';

describe('EssencesAbsorbComponent', () => {
  let fixture: ComponentFixture<EssencesAbsorbComponent>;
  let component: EssencesAbsorbComponent;
  let selectInventoryEssence: jasmine.Spy;
  let selectedInventoryItemState = signal<InventoryItem | null>(null);
  let inventoryEssencesState = signal<InventoryItem[]>([]);
  let isAbsorbed = true;

  const absorbedItem = {
    id: 'inventory-row-1',
    quantity: 6,
    itemInstance: {
      id: 'inventory-item-1',
      itemBase: {
        id: 'essence-item-1',
        dismantleDustAmount: 2,
      },
    },
  } as unknown as InventoryItem;

  beforeEach(async () => {
    isAbsorbed = true;
    selectedInventoryItemState = signal<InventoryItem | null>(null);
    inventoryEssencesState = signal<InventoryItem[]>([absorbedItem]);
    selectInventoryEssence = jasmine
      .createSpy('selectInventoryEssence')
      .and.callFake((item: InventoryItem) =>
        selectedInventoryItemState.set(item),
      );
    const essenceState = {
      inventoryEssences: inventoryEssencesState,
      selectedInventoryItem: selectedInventoryItemState,
      selectedInventoryEssence: signal<Essence | null>(null),
      isSelectedInventoryEssenceAbsorbed: signal(false),
      isInventoryEssenceAbsorbed: () => isAbsorbed,
      asEssence: () => ({ id: 'essence-1', name: 'Test Essence' }) as Essence,
      selectInventoryEssence,
    } as unknown as EssenceStateService;

    await TestBed.configureTestingModule({
      imports: [EssencesAbsorbComponent],
      providers: [{ provide: EssenceStateService, useValue: essenceState }],
    }).compileComponents();

    fixture = TestBed.createComponent(EssencesAbsorbComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('uses the checkbox for bulk selection without changing the detail row', () => {
    const checkbox = fixture.nativeElement.querySelector(
      '.absorb-row-selector',
    ) as HTMLInputElement;

    checkbox.click();
    fixture.detectChanges();

    expect(selectInventoryEssence).not.toHaveBeenCalled();
    expect(component.isSelectedForShatter(absorbedItem)).toBeTrue();
    expect(component.selectedShatterCount()).toBe(6);
    expect(component.selectedShatterDust()).toBe(12);
    expect(fixture.nativeElement.textContent).toContain('Shatter selected');
  });

  it('selects the detail only from the row content button', () => {
    const rowButton = fixture.nativeElement.querySelector(
      '.absorb-row-main',
    ) as HTMLButtonElement;

    rowButton.click();

    expect(selectInventoryEssence).toHaveBeenCalledOnceWith(absorbedItem);
    expect(component.isSelectedForShatter(absorbedItem)).toBeFalse();
    expect(component.mobileDetailOpen()).toBeTrue();
  });

  it('opens and closes the mobile detail sheet from an Essence row', () => {
    component.selectEssence(absorbedItem);
    expect(component.mobileDetailOpen()).toBeTrue();

    component.closeMobileDetail();
    expect(component.mobileDetailOpen()).toBeFalse();
  });

  it('shows mobile selection controls and clears selections when done', () => {
    component.toggleMobileSelectionMode();
    fixture.detectChanges();
    expect(component.mobileSelectionMode()).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('Done');
    expect(fixture.nativeElement.textContent).toContain('Duplicates');

    component.toggleShatterSelection(absorbedItem, {
      target: { checked: true },
    } as unknown as Event);
    expect(component.isSelectedForShatter(absorbedItem)).toBeTrue();

    component.toggleMobileSelectionMode();
    expect(component.mobileSelectionMode()).toBeFalse();
    expect(component.isSelectedForShatter(absorbedItem)).toBeFalse();
  });

  it('toggles all absorbed duplicates from the mobile action', () => {
    component.toggleMobileDuplicatesSelection();
    expect(component.allAbsorbedDuplicatesSelected()).toBeTrue();
    expect(component.selectedShatterCount()).toBe(6);

    component.toggleMobileDuplicatesSelection();
    expect(component.allAbsorbedDuplicatesSelected()).toBeFalse();
    expect(component.selectedShatterCount()).toBe(0);
  });

  it('clamps the single-shatter quantity to the selected spare copies', () => {
    component.selectEssence(absorbedItem);

    component.maximizeSingleShatterQuantity();
    expect(component.singleShatterQuantity()).toBe(6);

    component.adjustSingleShatterQuantity(-1);
    expect(component.singleShatterQuantity()).toBe(5);

    component.setSingleShatterQuantity({
      target: { value: '99' },
    } as unknown as Event);
    expect(component.singleShatterQuantity()).toBe(6);
  });

  it('reserves one copy only while the Essence is not absorbed', () => {
    isAbsorbed = false;
    expect(component.spareCopies(absorbedItem)).toBe(5);

    isAbsorbed = true;
    expect(component.spareCopies(absorbedItem)).toBe(6);
  });

  it('highlights unabsorbed Essences and keeps them above absorbed copies', () => {
    const unabsorbedItem = {
      ...absorbedItem,
      id: 'inventory-row-2',
      itemInstance: {
        ...absorbedItem.itemInstance,
        id: 'inventory-item-2',
      },
    } as InventoryItem;
    inventoryEssencesState.set([absorbedItem, unabsorbedItem]);
    spyOn(component.essenceState, 'isInventoryEssenceAbsorbed').and.callFake(
      (item) => item === absorbedItem,
    );
    spyOn(component.essenceState, 'asEssence').and.callFake(
      (item) =>
        ({
          id: item.itemInstance.id,
          name: item === absorbedItem ? 'Alpha Essence' : 'Zeta Essence',
        }) as Essence,
    );

    expect(component.visibleEssences()).toEqual([unabsorbedItem, absorbedItem]);

    fixture.detectChanges();
    const rows = fixture.nativeElement.querySelectorAll('.absorb-list-row');
    expect(rows[0].textContent).toContain('Not absorbed');
    expect(rows[1].textContent).toContain('Absorbed');
  });
});
