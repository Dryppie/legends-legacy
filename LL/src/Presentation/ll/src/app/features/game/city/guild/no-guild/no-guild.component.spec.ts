import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NoGuildComponent } from './no-guild.component';

describe('NoGuildComponent', () => {
  let component: NoGuildComponent;
  let fixture: ComponentFixture<NoGuildComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NoGuildComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NoGuildComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
