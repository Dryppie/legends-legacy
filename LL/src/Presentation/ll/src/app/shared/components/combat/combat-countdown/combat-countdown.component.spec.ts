import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CombatCountdownComponent } from './combat-countdown.component';

describe('CombatCountdownComponent', () => {
  let component: CombatCountdownComponent;
  let fixture: ComponentFixture<CombatCountdownComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatCountdownComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CombatCountdownComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
