import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GenericLeaderboardComponent } from './generic-leaderboard.component';

describe('GenericLeaderboardComponent', () => {
  let component: GenericLeaderboardComponent;
  let fixture: ComponentFixture<GenericLeaderboardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [GenericLeaderboardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(GenericLeaderboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
