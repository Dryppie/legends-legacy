import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryEquipmentModalComponent } from './inventory-equipment-modal.component';

describe('InventoryEquipmentModalComponent', () => {
  let component: InventoryEquipmentModalComponent;
  let fixture: ComponentFixture<InventoryEquipmentModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryEquipmentModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(InventoryEquipmentModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
