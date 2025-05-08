import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CombatAvatarComponent } from './combat-avatar.component';

describe('CombatAvatarComponent', () => {
  let component: CombatAvatarComponent;
  let fixture: ComponentFixture<CombatAvatarComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CombatAvatarComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CombatAvatarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
