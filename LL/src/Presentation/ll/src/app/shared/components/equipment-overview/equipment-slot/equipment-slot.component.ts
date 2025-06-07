import { Component, Input } from '@angular/core';
import { InfoBoxComponent } from '../../info-box/info-box.component';

@Component({
  selector: 'app-equipment-slot',
  standalone: true,
  imports: [InfoBoxComponent],
  templateUrl: './equipment-slot.component.html',
})
export class EquipmentSlotComponent {
  @Input() icon: string = '';
  @Input() name: string = '';
  @Input() description: string = '';
}
