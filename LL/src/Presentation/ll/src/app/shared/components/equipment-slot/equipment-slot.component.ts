import { NgIf } from '@angular/common';
import { Component, Input, ViewChild } from '@angular/core';
import { InfoBoxComponent } from '../info-box/info-box.component';

@Component({
  selector: 'app-equipment-slot',
  standalone: true,
  imports: [NgIf, InfoBoxComponent],
  templateUrl: './equipment-slot.component.html',
  styleUrl: './equipment-slot.component.css',
})
export class EquipmentSlotComponent {
  @Input() icon: string = '';
  @Input() name: string = '';
  @Input() description: string = '';
}
