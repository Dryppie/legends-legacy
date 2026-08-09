import { NgIf, NgFor, NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-filter-tabs',
  imports: [NgIf, NgFor, NgClass],
  templateUrl: './filter-tabs.component.html',
})
export class FilterTabsComponent {
  @Input() tabs: readonly string[] = [];
  @Input() activeTab: string = '';
  @Input() tourTabPrefix: string | null = null;
  @Input() scrollable = false;
  @Output() tabSelected = new EventEmitter<string>();

  // Holds the active tab's label

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
    this.tabSelected.emit(tabLabel); // Emit the selected tab's label
  }

  getTourId(tab: string): string | null {
    if (!this.tourTabPrefix) {
      return null;
    }

    return `${this.tourTabPrefix}-${this.toKebabCase(tab)}`;
  }

  private toKebabCase(value: string): string {
    return value
      .trim()
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-');
  }
}
