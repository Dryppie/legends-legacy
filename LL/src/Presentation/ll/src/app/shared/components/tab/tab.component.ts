import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NgClass, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-tab',
  standalone: true,
  imports: [NgIf, NgFor, NgClass],
  templateUrl: './tab.component.html',
})
export class TabComponent {
  @Input() tabs: string[] = [];
  @Input() activeTab: string = '';
  @Output() tabSelected = new EventEmitter<string>();

  // Holds the active tab's label

  setActiveTab(tabLabel: string) {
    this.activeTab = tabLabel;
    this.tabSelected.emit(tabLabel); // Emit the selected tab's label
  }
}
