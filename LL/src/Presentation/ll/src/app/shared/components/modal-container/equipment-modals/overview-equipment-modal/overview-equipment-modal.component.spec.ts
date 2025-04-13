import { ComponentFixture, TestBed } from '@angular/core/testing';

import { OverviewEquipmentModalComponent } from './overview-equipment-modal.component';

describe('OverviewEquipmentModalComponent', () => {
  let component: OverviewEquipmentModalComponent;
  let fixture: ComponentFixture<OverviewEquipmentModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [OverviewEquipmentModalComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(OverviewEquipmentModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
