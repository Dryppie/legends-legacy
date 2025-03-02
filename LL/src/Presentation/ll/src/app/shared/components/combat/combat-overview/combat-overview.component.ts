import { CommonModule, NgFor } from '@angular/common';
import { Component, Input, SimpleChanges } from '@angular/core';
import { CombatOverviewItemComponent } from './combat-overview-item/combat-overview-item.component';
import { CombatEvent, EventType } from '../../../models/Dtos/combatEventDto';
import { StickyScrollDirective } from '../../../directives/sticky-scroll/sticky-scroll.directive';
import { ModalService } from '../../../../core/services/modal/modal.service';

@Component({
  selector: 'app-combat-overview',
  standalone: true,
  imports: [
    NgFor,
    CombatOverviewItemComponent,
    StickyScrollDirective,
    CommonModule,
  ],
  templateUrl: './combat-overview.component.html',
  styleUrl: './combat-overview.component.css',
})
export class CombatOverviewComponent {
  @Input() combatEvents: CombatEvent[] = [];
  @Input() isLoading = false;

  filteredCombatEvents: CombatEvent[] = [];

  constructor(private modalService: ModalService) {}

  ngOnInit(): void {
    // 1) Subscribe to the observable that notifies when the filter modal changes.
    //    Typically you'd do this after the user clicks "Save" or the modal closes.
    this.modalService.editCombatFiltersModalState$.subscribe((modalState) => {
      // If modalState indicates filters were changed or the modal just closed,
      // re-apply filters to show updated results.
      this.applyFilters();
    });

    // 2) Apply filters initially (in case there's something stored in localStorage)
    this.applyFilters();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['combatEvents']) {
      this.applyFilters();
    }
  }

  private applyFilters(): void {
    const storedFilters = localStorage.getItem('combatLogFilters');
    if (!storedFilters) {
      // If no filters are in storage, we default to showing all events
      this.filteredCombatEvents = this.combatEvents;
      return;
    }

    const selectedFilters: EventType[] = JSON.parse(storedFilters);

    // Filter out any event whose 'type' is not included in the user’s selection
    this.filteredCombatEvents = this.combatEvents.filter((event) =>
      selectedFilters.includes(event.eventType),
    );
  }

  openFilters() {
    this.modalService.toggleCombatFiltersModal(true);
  }
}
