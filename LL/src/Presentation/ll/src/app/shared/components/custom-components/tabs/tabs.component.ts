import {
  AfterContentInit,
  Component,
  computed,
  ContentChildren,
  EventEmitter,
  Input,
  OnChanges,
  Output,
  QueryList,
  signal,
  SimpleChanges,
} from '@angular/core';
import { NgClass, NgFor, NgIf, NgTemplateOutlet } from '@angular/common';
import { TabComponent } from './tab/tab.component';
import { NotificationIndicatorComponent } from '../../notification-indicator/notification-indicator.component';

@Component({
  selector: 'app-tabs',
  imports: [
    NgFor,
    NgIf,
    NgTemplateOutlet,
    NgClass,
    NotificationIndicatorComponent,
  ],
  templateUrl: './tabs.component.html',
})
export class TabsComponent implements AfterContentInit, OnChanges {
  @ContentChildren(TabComponent) panes!: QueryList<TabComponent>;
  @Input() selectedIndex = 0;
  @Output() selectedIndexChange = new EventEmitter<number>();

  /** Which pane is visible */
  private readonly _activeIndex = signal(0);
  readonly activeIndex = computed(() => this._activeIndex());

  ngAfterContentInit() {
    if (this.panes.length) this.activate(this.selectedIndex);
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['selectedIndex'] && this.panes?.length) {
      this.activate(this.selectedIndex);
    }
  }

  select(index: number): void {
    if (!this.activate(index)) return;
    this.selectedIndexChange.emit(this._activeIndex());
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

  private activate(index: number): boolean {
    const lastIndex = Math.max((this.panes?.length ?? 1) - 1, 0);
    const nextIndex = Math.max(0, Math.min(index, lastIndex));
    if (this._activeIndex() === nextIndex) return false;

    this._activeIndex.set(nextIndex);
    return true;
  }
}
