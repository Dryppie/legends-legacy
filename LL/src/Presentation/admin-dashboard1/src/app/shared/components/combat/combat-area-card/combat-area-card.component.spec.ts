import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CombatAreaCardComponent } from './combat-area-card.component';

describe('CombatAreaCardComponent', () => {
  let component: CombatAreaCardComponent;
  let fixture: ComponentFixture<CombatAreaCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatAreaCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CombatAreaCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
