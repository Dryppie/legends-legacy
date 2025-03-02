import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CombatFiltersModalComponent } from './combat-filters-modal.component';

describe('CombatFiltersModalComponent', () => {
  let component: CombatFiltersModalComponent;
  let fixture: ComponentFixture<CombatFiltersModalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatFiltersModalComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CombatFiltersModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
