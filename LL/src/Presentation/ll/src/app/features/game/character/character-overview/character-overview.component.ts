import { NgFor, NgIf } from '@angular/common';
import { Component } from '@angular/core';
import { EquipmentSlotComponent } from '../../../../shared/components/equipment-slot/equipment-slot.component';

@Component({
  selector: 'app-character-overview',
  standalone: true,
  imports: [NgIf, NgFor, EquipmentSlotComponent],
  templateUrl: './character-overview.component.html',
  styleUrl: './character-overview.component.css',
})
export class CharacterOverviewComponent {
  showItemInfo = false;
  itemName = '';
  itemDescription = '';
  itemImage = '';

  equipmentSlots = [
    {
      name: 'Helmet',
      description: 'A sturdy helmet made of iron.',
      icon: 'helmet-icon.png',
      image: 'helmet.png',
    },
    {
      name: 'Amulet',
      description: 'A magical amulet that boosts mana.',
      icon: 'amulet-icon.png',
      image: 'amulet.png',
    },
    {
      name: 'Armor',
      description: 'A heavy armor for protection.',
      icon: 'armor-icon.png',
      image: 'armor.png',
    },
    {
      name: 'Gloves',
      description: 'Leather gloves for dexterity.',
      icon: 'gloves-icon.png',
      image: 'gloves.png',
    },
    {
      name: 'Sword',
      description: 'A sharp steel sword.',
      icon: 'sword-icon.png',
      image: 'sword.png',
    },
    {
      name: 'Shield',
      description: 'A sturdy wooden shield.',
      icon: 'shield-icon.png',
      image: 'shield.png',
    },
    {
      name: 'Belt',
      description: 'A strong belt for carrying items.',
      icon: 'belt-icon.png',
      image: 'belt.png',
    },
    {
      name: 'Ring of Strength',
      description: 'A ring that increases strength.',
      icon: 'ring-icon.png',
      image: 'ring1.png',
    },
    {
      name: 'Ring of Agility',
      description: 'A ring that increases agility.',
      icon: 'ring-icon.png',
      image: 'ring2.png',
    },
    {
      name: 'Boots',
      description: 'Leather boots for agility.',
      icon: 'boots-icon.png',
      image: 'boots.png',
    },
  ];
}
