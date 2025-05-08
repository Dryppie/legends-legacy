import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AttributeDisplayComponent } from './attribute-display.component';

describe('AttributeDisplayComponent', () => {
  let component: AttributeDisplayComponent;
  let fixture: ComponentFixture<AttributeDisplayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AttributeDisplayComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(AttributeDisplayComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
