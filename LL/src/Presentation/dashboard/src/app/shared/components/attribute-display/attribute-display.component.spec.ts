import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AttributeDisplayComponent } from './attribute-display.component';
import { testAttribute } from '../../testing/model-fixtures';

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
    component.attribute = testAttribute;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
