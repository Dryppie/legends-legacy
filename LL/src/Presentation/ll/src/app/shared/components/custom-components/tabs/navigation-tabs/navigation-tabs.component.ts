import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  Input,
  Output,
  QueryList,
  ViewChildren,
} from '@angular/core';
import { NotificationIndicatorComponent } from '../../../notification-indicator/notification-indicator.component';

export interface NavigationTab {
  key: string;
  label: string;
  disabled?: boolean;
  badgeCount?: number;
  badgeLabel?: string;
}

export type NavigationTabsAppearance = 'primary' | 'compact';

@Component({
  selector: 'app-navigation-tabs',
  standalone: true,
  imports: [NgClass, NgFor, NgIf, NotificationIndicatorComponent],
  templateUrl: './navigation-tabs.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavigationTabsComponent {
  @Input() tabs: readonly NavigationTab[] = [];
  @Input() activeKey = '';
  @Input() ariaLabel = 'Sections';
  @Input() appearance: NavigationTabsAppearance = 'primary';
  @Input() stretch = true;
  @Output() readonly tabSelected = new EventEmitter<string>();

  @ViewChildren('tabButton')
  private readonly tabButtons!: QueryList<ElementRef<HTMLButtonElement>>;

  select(tab: NavigationTab): void {
    if (tab.disabled || tab.key === this.activeKey) return;
    this.tabSelected.emit(tab.key);
  }

  onTabKeydown(event: KeyboardEvent, index: number): void {
    const enabledIndices = this.tabs
      .map((tab, tabIndex) => (tab.disabled ? -1 : tabIndex))
      .filter((tabIndex) => tabIndex >= 0);
    if (enabledIndices.length === 0) return;

    const enabledPosition = enabledIndices.indexOf(index);
    let targetPosition: number;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        targetPosition = (enabledPosition + 1) % enabledIndices.length;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        targetPosition =
          (enabledPosition - 1 + enabledIndices.length) % enabledIndices.length;
        break;
      case 'Home':
        targetPosition = 0;
        break;
      case 'End':
        targetPosition = enabledIndices.length - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    const targetIndex = enabledIndices[targetPosition];
    const targetTab = this.tabs[targetIndex];
    this.select(targetTab);
    this.tabButtons.get(targetIndex)?.nativeElement.focus();
  }
}
