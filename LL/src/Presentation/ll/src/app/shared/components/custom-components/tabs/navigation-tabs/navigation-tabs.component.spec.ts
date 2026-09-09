import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  NavigationTab,
  NavigationTabsComponent,
} from './navigation-tabs.component';

describe('NavigationTabsComponent', () => {
  let fixture: ComponentFixture<NavigationTabsComponent>;
  const tabs: NavigationTab[] = [
    { key: 'first', label: 'First' },
    { key: 'disabled', label: 'Disabled', disabled: true },
    { key: 'last', label: 'Last' },
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavigationTabsComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(NavigationTabsComponent);
    fixture.componentRef.setInput('tabs', tabs);
    fixture.componentRef.setInput('activeKey', 'first');
    fixture.detectChanges();
  });

  it('emits stable tab keys when selected', () => {
    const selected: string[] = [];
    fixture.componentInstance.tabSelected.subscribe((key) =>
      selected.push(key),
    );

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    buttons[2].click();

    expect(selected).toEqual(['last']);
  });

  it('supports keyboard navigation and skips disabled tabs', () => {
    const selected: string[] = [];
    fixture.componentInstance.tabSelected.subscribe((key) =>
      selected.push(key),
    );
    const firstButton = fixture.nativeElement.querySelector(
      'button',
    ) as HTMLButtonElement;

    firstButton.dispatchEvent(
      new KeyboardEvent('keydown', { key: 'ArrowRight' }),
    );

    expect(selected).toEqual(['last']);
  });

  it('adds stable tour selectors when a prefix is provided', () => {
    fixture.componentRef.setInput('tourTabPrefix', 'example-tab');
    fixture.detectChanges();

    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;

    expect(buttons[0].dataset['tour']).toBe('example-tab-first');
    expect(buttons[2].dataset['tour']).toBe('example-tab-last');
  });

  it('shows optional status independently of the tab being viewed', () => {
    expect(
      fixture.nativeElement.querySelector('.navigation-tab-status'),
    ).toBeNull();
    fixture.componentRef.setInput(
      'tabs',
      tabs.map((tab) => ({
        ...tab,
        statusLabel: tab.key === 'first' ? 'Equipped' : undefined,
      })),
    );
    fixture.componentRef.setInput('activeKey', 'last');
    fixture.detectChanges();
    const buttons = fixture.nativeElement.querySelectorAll(
      'button',
    ) as NodeListOf<HTMLButtonElement>;
    const badge = buttons[0].querySelector('.navigation-tab-status')!;
    expect(badge.textContent?.trim()).toBe('Equipped');
    expect(buttons[0].getAttribute('aria-selected')).toBe('false');
    expect(buttons[2].getAttribute('aria-selected')).toBe('true');
    expect(buttons[2].querySelector('.navigation-tab-status')).toBeNull();
    const selected: string[] = [];
    fixture.componentInstance.tabSelected.subscribe((key) =>
      selected.push(key),
    );
    buttons[0].click();
    expect(selected).toEqual(['first']);
  });
});
