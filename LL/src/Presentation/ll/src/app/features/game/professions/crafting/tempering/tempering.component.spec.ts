import { ComponentFixture, TestBed } from '@angular/core/testing';

import { TemperingComponent } from './tempering.component';

describe('TemperingComponent', () => {
  let component: TemperingComponent;
  let fixture: ComponentFixture<TemperingComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TemperingComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(TemperingComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
