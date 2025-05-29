import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SoulstoneUpgradeCardComponent } from './soulstone-upgrade-card.component';

describe('SoulstoneUpgradeCardComponent', () => {
  let component: SoulstoneUpgradeCardComponent;
  let fixture: ComponentFixture<SoulstoneUpgradeCardComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SoulstoneUpgradeCardComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SoulstoneUpgradeCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
