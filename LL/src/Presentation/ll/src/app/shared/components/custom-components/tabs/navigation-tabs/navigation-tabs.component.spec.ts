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
});
