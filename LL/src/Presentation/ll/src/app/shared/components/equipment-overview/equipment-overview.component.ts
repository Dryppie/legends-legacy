import { NgFor } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'app-equipment-overview',
  standalone: true,
  imports: [NgFor],
  templateUrl: './equipment-overview.component.html',
  styleUrl: './equipment-overview.component.css'
})
export class EquipmentOverviewComponent {
  slots = [
    { name: 'Head',       icon: 'empty_helmet' },
    { name: 'Clock',       icon: 'empty_cloak' },
    { name: 'Chest',      icon: 'empty_armor' },
    { name: 'Accessory',  icon: 'empty_necklace' },
    { name: 'Legs',       icon: 'empty_legs' },
    { name: 'Accessory',  icon: 'empty_ring' },
    { name: 'Main Hand',  icon: 'empty_mainhand' },
    { name: 'Off Hand',   icon: 'empty_offhand' },
  ];
}
