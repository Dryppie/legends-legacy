import { ComponentFixture, TestBed } from '@angular/core/testing';

import { RankingsGloryComponent } from './rankings-glory.component';

describe('RankingsGloryComponent', () => {
  let component: RankingsGloryComponent;
  let fixture: ComponentFixture<RankingsGloryComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RankingsGloryComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(RankingsGloryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
