import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { EquipmentSupportSnapshot, SupportSection } from '../../liveops.models';

@Component({
  selector: 'app-equipment-support',
  standalone: true,
  imports: [CommonModule],
  host: { class: 'support-card support-wide', '[class.unavailable]': '!section.isAvailable' },
  templateUrl: './equipment-support.component.html',
  styles: [`
    :host { min-width: 0; }
    .equipment-support-detail { padding: .65rem 0; border-bottom: 1px solid #272b31; }
    summary { cursor: pointer; overflow-wrap: anywhere; }
    dd, code, small { overflow-wrap: anywhere; }
  `],
})
export class EquipmentSupportComponent {
  @Input({ required: true }) section!: SupportSection<EquipmentSupportSnapshot>;
}
