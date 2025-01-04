import { NgFor } from '@angular/common';
import { Component, Input } from '@angular/core';
import { Essence } from '../../../models/essence';

@Component({
  selector: 'app-equipped-essences',
  standalone: true,
  imports: [NgFor],
  templateUrl: './equipped-essences.component.html',
  styleUrl: './equipped-essences.component.css',
})
export class EquippedEssencesComponent {
  @Input() essences: Essence[] = [];
}
