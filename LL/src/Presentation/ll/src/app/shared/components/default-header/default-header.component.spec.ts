import { Component } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HelpOverlayService } from '../../help/help-overlay.service';
import { DefaultHeaderComponent } from './default-header.component';

@Component({
  imports: [DefaultHeaderComponent],
  template: `
    <app-default-header
      title="Test page"
      icon="sidebar/world/prophecies"
      [showGuide]="showGuide"
    >
      <button header-actions type="button" data-testid="page-action">
        Refresh
      </button>
    </app-default-header>
  `,
})
class TestHostComponent {
  showGuide = true;
}

describe('DefaultHeaderComponent', () => {
  let fixture: ComponentFixture<TestHostComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [
        {
          provide: HelpOverlayService,
          useValue: jasmine.createSpyObj<HelpOverlayService>(
            'HelpOverlayService',
            ['open'],
          ),
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
  });

  it('places the guide after projected page actions', () => {
    const action: HTMLButtonElement = fixture.nativeElement.querySelector(
      '[data-testid="page-action"]',
    );
    const guide: HTMLButtonElement = fixture.nativeElement.querySelector(
      '[data-tour="page-helper"]',
    );

    expect(
      action.compareDocumentPosition(guide) & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it('omits the guide when the page opts out', () => {
    fixture.componentInstance.showGuide = false;
    fixture.detectChanges();

    expect(
      fixture.nativeElement.querySelector('[data-tour="page-helper"]'),
    ).toBeNull();
  });
});
