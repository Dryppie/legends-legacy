import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CurrentActionComponent } from './current-action.component';

describe('CurrentActionComponent', () => {
  let component: CurrentActionComponent;
  let fixture: ComponentFixture<CurrentActionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CurrentActionComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(CurrentActionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
