import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { HelpLauncherComponent } from './help-launcher.component';
import { HelpOverlayService } from './help-overlay.service';

describe('HelpLauncherComponent', () => {
  let fixture: ComponentFixture<HelpLauncherComponent>;
  let component: HelpLauncherComponent;
  let overlay: jasmine.SpyObj<HelpOverlayService>;
  let router: {
    url: string;
    routerState: {
      snapshot: {
        root: {
          data: Record<string, unknown>;
          firstChild: null;
        };
      };
    };
  };

  beforeEach(async () => {
    overlay = jasmine.createSpyObj<HelpOverlayService>('HelpOverlayService', [
      'open',
    ]);
    router = {
      url: '/game/professions/crafting',
      routerState: {
        snapshot: {
          root: { data: { guidePageId: 'crafting' }, firstChild: null },
        },
      },
    };

    await TestBed.configureTestingModule({
      imports: [HelpLauncherComponent],
      providers: [
        { provide: Router, useValue: router },
        { provide: HelpOverlayService, useValue: overlay },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HelpLauncherComponent);
    component = fixture.componentInstance;
  });

  it('renders the inline guide control and opens the requested guide', () => {
    component.presentation = 'inline';
    component.pageId = 'crafting';
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector(
      '[data-tour="page-helper"]',
    );
    expect(button.textContent?.trim()).toBe('?');
    expect(button.classList).toContain('rounded-full');
    expect(button.classList).toContain('border-b');
    expect(button.classList).toContain('border-l');
    expect(button.classList).toContain('bg-texture');
    expect(button.classList).toContain('hover:scale-[1.1]');
    expect(button.parentElement?.classList).toContain('sm:h-14');
    expect(button.parentElement?.classList).toContain('sm:w-14');

    button.click();

    expect(overlay.open).toHaveBeenCalledOnceWith('crafting');
  });

  it('resolves the guide from route metadata when no page is specified', () => {
    component.presentation = 'inline';
    fixture.detectChanges();

    const button: HTMLButtonElement = fixture.nativeElement.querySelector(
      '[data-tour="page-helper"]',
    );
    button.click();

    expect(overlay.open).toHaveBeenCalledOnceWith('crafting');
  });
});
