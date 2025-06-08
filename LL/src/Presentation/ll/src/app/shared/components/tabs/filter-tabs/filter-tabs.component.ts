import { NgIf, NgFor, NgClass } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
  selector: 'app-filter-tabs',
  standalone: true,
  imports: [NgIf, NgFor, NgClass],
  templateUrl: './filter-tabs.component.html',
})
export class FilterTabsComponent {
  @Input() tabs: string[] = [];
  @Input() activeTab: string = '';
  @Output() tabSelected = new EventEmitter<string>();

  // Holds the active tab's label

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
    this.tabSelected.emit(tabLabel); // Emit the selected tab's label
  }
}
