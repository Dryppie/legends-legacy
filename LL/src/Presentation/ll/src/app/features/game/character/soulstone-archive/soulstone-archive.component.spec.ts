import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SoulstoneArchiveComponent } from './soulstone-archive.component';

describe('SoulstoneArchiveComponent', () => {
  let component: SoulstoneArchiveComponent;
  let fixture: ComponentFixture<SoulstoneArchiveComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SoulstoneArchiveComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(SoulstoneArchiveComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
