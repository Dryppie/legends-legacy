import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EssenceviewComponent } from './essenceview.component';

describe('EssenceviewComponent', () => {
  let component: EssenceviewComponent;
  let fixture: ComponentFixture<EssenceviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EssenceviewComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(EssenceviewComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
