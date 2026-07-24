import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { EventType } from '../../../../models/Dtos/combatEventDto';
import { NgFor } from '@angular/common';

@Component({
    selector: 'app-combat-filters-modal',
    imports: [NgFor],
    templateUrl: './combat-filters-modal.component.html'
})
export class CombatFiltersModalComponent implements OnInit {
  deselectAll(): void {
    this.selectedEventTypes = [];
  }
  selectAll(): void {
    this.selectedEventTypes = [...this.eventTypes];
  }

  @Output() close = new EventEmitter<void>();
  eventTypes = Object.values(EventType);

  // Keep track of which events are currently selected
  selectedEventTypes: EventType[] = [];

  constructor() {}

  ngOnInit(): void {
    // 1) Load any stored filters from localStorage
    const storedFilters = localStorage.getItem('combatLogFilters');
    if (storedFilters) {
      // Parse the stored JSON into our selected event array
      this.selectedEventTypes = JSON.parse(storedFilters);
    } else {
      // If nothing is in storage, default to all selected
      this.selectedEventTypes = [...this.eventTypes];
    }
  }

  /**
   * Called whenever a checkbox is toggled (checked/unchecked).
   */
  onToggleEventType(eventType: EventType, event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const isChecked = inputElement.checked;

    if (isChecked) {
      this.selectedEventTypes.push(eventType);
    } else {
      this.selectedEventTypes = this.selectedEventTypes.filter(
        (type) => type !== eventType,
      );
    }
  }

  /**
   * Save the current filter selection to localStorage.
   */
  onSave(): void {
    localStorage.setItem(
      'combatLogFilters',
      JSON.stringify(this.selectedEventTypes),
    );
    this.close.emit(); // Optionally close the modal after saving
  }

  onClose() {
    this.close.emit();
  }
}
