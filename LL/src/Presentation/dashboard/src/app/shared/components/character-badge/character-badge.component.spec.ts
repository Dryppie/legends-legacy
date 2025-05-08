import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CharacterBadgeComponent } from './character-badge.component';

describe('CharacterBadgeComponent', () => {
  let component: CharacterBadgeComponent;
  let fixture: ComponentFixture<CharacterBadgeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CharacterBadgeComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CharacterBadgeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
