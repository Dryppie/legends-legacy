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

@Component({
  selector: 'app-tabs',
  standalone: true,
  imports: [NgFor, NgIf, NgTemplateOutlet, NgClass],
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
}
