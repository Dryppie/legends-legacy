import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { EquipmentService } from '../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentProgressionItem } from '../../../models/equipment-progression';
import { MigratedSpecializationComponent } from './migrated-specialization.component';
import { AttributeDefinition, setAttributeDefinitions } from '../../../models/attribute-definition';
import { AttributeType } from '../../../models/enums/attributeType';

describe('MigratedSpecializationComponent', () => {
  afterEach(() => setAttributeDefinitions([]));

  it('previews equipment ratings and percentage stats with their server-provided units', () => {
    setAttributeDefinitions([
      { attributeType: AttributeType.Armor, displayName: 'Physical Damage Reduction',
        displaySuffix: '%', equipmentDisplayName: 'Armor Rating',
        equipmentDisplaySuffix: '', equipmentDisplayPrecision: 2 } as AttributeDefinition,
      { attributeType: AttributeType.Restoration, displayName: 'Restoration',
        equipmentDisplayName: 'Restoration', equipmentDisplaySuffix: '%',
        equipmentDisplayPrecision: 2 } as AttributeDefinition,
    ]);
    const api = jasmine.createSpyObj<EquipmentService>('equipment', ['getMigratedSpecializationChoice']);
    api.getMigratedSpecializationChoice.and.returnValue(of({ migrationId: 'migration', options: [{
      definitionId: 'restoration', stats: { Armor: 40, Restoration: 12.5 },
    } as unknown as EquipmentProgressionItem] }));
    TestBed.configureTestingModule({
      imports: [MigratedSpecializationComponent],
      providers: [
        { provide: EquipmentService, useValue: api },
        { provide: EquipmentStateService, useValue: {} },
        { provide: InventoryStateService, useValue: {} },
      ],
    });
    const fixture = TestBed.createComponent(MigratedSpecializationComponent);
    fixture.componentRef.setInput('itemId', 'item');
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Armor Rating: 40');
    expect(fixture.nativeElement.textContent).toContain('Restoration: 12.5%');
    expect(fixture.nativeElement.textContent).not.toContain('Physical Damage Reduction: 40');
  });

  it('retains the same operation ID on a failed request retry and refreshes both views on success', () => {
    const api = jasmine.createSpyObj<EquipmentService>('equipment', ['getMigratedSpecializationChoice', 'chooseMigratedSpecialization']);
    const equipment = jasmine.createSpyObj<EquipmentStateService>('equipmentState', ['load']);
    const inventory = jasmine.createSpyObj<InventoryStateService>('inventoryState', ['load']);
    api.getMigratedSpecializationChoice.and.returnValue(of({ migrationId: 'migration', options: [{ definitionId: 'precision', stats: {} } as EquipmentProgressionItem] }));
    api.chooseMigratedSpecialization.and.returnValues(throwError(() => new Error('connection lost')), of({}));
    TestBed.configureTestingModule({ providers: [
      { provide: EquipmentService, useValue: api }, { provide: EquipmentStateService, useValue: equipment },
      { provide: InventoryStateService, useValue: inventory },
    ] });
    const component = TestBed.runInInjectionContext(() => new MigratedSpecializationComponent());
    component.itemId = 'item'; component.ngOnInit(); component.apply();
    expect(component.saving).toBeFalse(); expect(component.error).toBeTruthy();
    component.apply();
    expect(api.chooseMigratedSpecialization.calls.argsFor(0)).toEqual(api.chooseMigratedSpecialization.calls.argsFor(1));
    expect(equipment.load).toHaveBeenCalledOnceWith(true);
    expect(inventory.load).toHaveBeenCalledOnceWith(true);
  });
});
