import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SessionSummaryPopupComponent } from './session-summary-popup.component';

describe('SessionSummaryPopupComponent', () => {
  let component: SessionSummaryPopupComponent;
  let fixture: ComponentFixture<SessionSummaryPopupComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SessionSummaryPopupComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SessionSummaryPopupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
