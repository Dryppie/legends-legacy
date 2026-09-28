import { TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of } from 'rxjs';
import { EquipmentService } from '../../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { CharacterStateService } from '../../../../../core/services/api/character/character-state.service';
import { EquipmentSlot } from '../../../../models/Dtos/equipment-slots/equipmentSlot';
import { EquipmentType } from '../../../../models/enums/equipmentType';
import { EquipmentInstance } from '../../../../models/item';
import { InventoryEquipmentModalComponent } from './inventory-equipment-modal.component';
import { EquipmentProgressionItem } from '../../../../models/equipment-progression';
import { EquipmentDisplayComponent } from '../../../equipment/equipment-display/equipment-display.component';
import { EquipmentUpgradePanelComponent } from '../../../equipment/equipment-upgrade-panel/equipment-upgrade-panel.component';
import { InventoryTransferComponent } from '../../../inventory-transfer/inventory-transfer.component';

describe('InventoryEquipmentModalComponent', () => {
  for (const balanceVersion of [2, 3, 4, 5]) {
    it(`loads a server-approved free choice for equipment release ${balanceVersion}`, async () => {
      const { fixture, api } = await renderManagement(balanceVersion);

      expect(api.getMigratedSpecializationChoice).toHaveBeenCalledOnceWith('migrated-item');
      expect(fixture.nativeElement.textContent).toContain('Use my free choice');
    });
  }

  it('does not offer a free choice when the server reports no remaining credit', async () => {
    const { fixture, api } = await renderManagement(4, false, false);

    expect(api.getMigratedSpecializationChoice).toHaveBeenCalledOnceWith('migrated-item');
    expect(fixture.nativeElement.textContent).not.toContain('Use my free choice');
  });

  it('does not request a personal specialization choice for borrowed guild equipment', async () => {
    const { fixture, api } = await renderManagement(4, true);

    expect(api.getMigratedSpecializationChoice).not.toHaveBeenCalled();
    expect(fixture.nativeElement.textContent).not.toContain('Use my free choice');
  });

  it('opens management mode without comparison work', () => {
    const equipment = {
      id: 'equipped-item',
      itemBase: { equipmentType: EquipmentType.TwoHanded },
    } as unknown as EquipmentInstance;
    const equipmentState = {
      equipmentSlots: () => [] as EquipmentSlot[],
    } as EquipmentStateService;
    const equipmentApi = jasmine.createSpyObj<EquipmentService>(
      'EquipmentService',
      ['compareEquipment'],
    );
    const component = TestBed.runInInjectionContext(
      () =>
        new InventoryEquipmentModalComponent(
          equipmentState,
          { items: () => [] } as unknown as InventoryStateService,
          equipmentApi,
          {
            currentCharacter: () => null,
          } as unknown as CharacterStateService,
        ),
    );
    component.equipmentInstance = equipment;
    component.managementOnly = true;

    component.ngOnInit();

    expect(component.isEquipped).toBeFalse();
    expect(component.requiresHandSelection).toBeFalse();
    expect(equipmentApi.compareEquipment).not.toHaveBeenCalled();
  });
});

async function renderManagement(balanceVersion: number, isGuildBorrowed = false, hasChoice = true) {
  const api = jasmine.createSpyObj<EquipmentService>('EquipmentService', ['getMigratedSpecializationChoice']);
  api.getMigratedSpecializationChoice.and.returnValue(of(hasChoice ? {
    migrationId: 'migration',
    options: [{ definitionId: 'restoration', stats: {} } as EquipmentProgressionItem],
  } : null));
  TestBed.configureTestingModule({
    imports: [InventoryEquipmentModalComponent],
    providers: [
      { provide: EquipmentService, useValue: api },
      { provide: EquipmentStateService, useValue: { equipmentSlots: () => [] } },
      { provide: InventoryStateService, useValue: { items: () => [] } },
      { provide: CharacterStateService, useValue: { currentCharacter: () => null } },
    ],
  });
  // Keep the real modal and choice component together; unrelated panels have
  // their own tests and are not needed to exercise the release/ownership gate.
  TestBed.overrideComponent(InventoryEquipmentModalComponent, {
    remove: { imports: [EquipmentDisplayComponent, EquipmentUpgradePanelComponent, InventoryTransferComponent] },
    add: { schemas: [NO_ERRORS_SCHEMA] },
  });
  await TestBed.compileComponents();
  const fixture = TestBed.createComponent(InventoryEquipmentModalComponent);
  fixture.componentRef.setInput('managementOnly', true);
  fixture.componentRef.setInput('equipmentInstance', {
    id: 'migrated-item',
    itemBase: { equipmentType: EquipmentType.TwoHanded },
    progression: { balanceVersion },
    isGuildBorrowed,
  });
  fixture.detectChanges();
  return { fixture, api };
}
