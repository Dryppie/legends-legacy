import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventorySlotComponent } from './inventory-slot.component';

describe('InventorySlotComponent', () => {
  let component: InventorySlotComponent;
  let fixture: ComponentFixture<InventorySlotComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventorySlotComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventorySlotComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
