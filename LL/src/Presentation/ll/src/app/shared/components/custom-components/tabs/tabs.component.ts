import {
  AfterContentInit,
  Component,
  computed,
  ContentChildren,
  QueryList,
  signal,
} from '@angular/core';
import { NgClass, NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import { TabComponent } from './tab/tab.component';
import { NotificationIndicatorComponent } from '../../notification-indicator/notification-indicator.component';

@Component({
  selector: 'app-tabs',
  standalone: true,
  imports: [
    NgFor,
    NgIf,
    NgTemplateOutlet,
    NgClass,
    NotificationIndicatorComponent,
  ],
  templateUrl: './tabs.component.html',
})
export class TabsComponent implements AfterContentInit {
  @ContentChildren(TabComponent) panes!: QueryList<TabComponent>;

  /** Which pane is visible */
  private readonly _activeIndex = signal(0);
  readonly activeIndex = computed(() => this._activeIndex());

  ngAfterContentInit() {
    // Activate the first tab when the content children are ready
    if (this.panes.length) this.select(0);
  }

  select(i: number) {
    this._activeIndex.set(i);
  }

  onTabKeydown(event: KeyboardEvent, index: number): void {
    const count = this.panes.length;
    if (!count) return;

    let nextIndex: number | null = null;

    switch (event.key) {
      case 'ArrowRight':
      case 'ArrowDown':
        nextIndex = (index + 1) % count;
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
        nextIndex = (index - 1 + count) % count;
        break;
      case 'Home':
        nextIndex = 0;
        break;
      case 'End':
        nextIndex = count - 1;
        break;
      default:
        return;
    }

    event.preventDefault();
    this.select(nextIndex);
  }
}
