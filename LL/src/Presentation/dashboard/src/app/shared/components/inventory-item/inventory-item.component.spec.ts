import { ComponentFixture, TestBed } from '@angular/core/testing';

import { InventoryItemComponent } from './inventory-item.component';
import { testItem } from '../../testing/model-fixtures';

describe('InventoryItemComponent', () => {
  let component: InventoryItemComponent;
  let fixture: ComponentFixture<InventoryItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InventoryItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(InventoryItemComponent);
    component = fixture.componentInstance;
    component.inventoryItem = {
      id: 'test-inventory-item',
      itemInstance: testItem,
      quantity: 1,
    };
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
