import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CharacterAttributesComponent } from './character-attributes.component';

describe('CharacterAttributesComponent', () => {
  let component: CharacterAttributesComponent;
  let fixture: ComponentFixture<CharacterAttributesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CharacterAttributesComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CharacterAttributesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
