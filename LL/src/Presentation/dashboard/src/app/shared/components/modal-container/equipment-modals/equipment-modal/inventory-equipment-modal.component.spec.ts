import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryEquipmentModalComponent } from './inventory-equipment-modal.component';
import { Equipment } from '../../../../models/item';
import { testItem } from '../../../../testing/model-fixtures';
import { httpTestingProviders } from '../../../../testing/http-testing-providers';

describe('InventoryEquipmentModalComponent', () => {
  let component: InventoryEquipmentModalComponent;
  let fixture: ComponentFixture<InventoryEquipmentModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryEquipmentModalComponent],
      providers: httpTestingProviders,
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryEquipmentModalComponent);
    component = fixture.componentInstance;
    component.equipment = testItem.itemBase as Equipment;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
