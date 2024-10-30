import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CombatOverviewItemComponent } from './combat-overview-item.component';

describe('CombatOverviewItemComponent', () => {
  let component: CombatOverviewItemComponent;
  let fixture: ComponentFixture<CombatOverviewItemComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatOverviewItemComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CombatOverviewItemComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
