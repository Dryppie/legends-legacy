import { Component } from '@angular/core';
import { ProfessionHeaderComponent } from '../../../../shared/components/professions/profession-header/profession-header.component';
import { ProfessionCardComponent } from '../../../../shared/components/professions/profession-card/profession-card.component';

@Component({
  selector: 'app-woodcutting',
  standalone: true,
  imports: [ProfessionHeaderComponent, ProfessionCardComponent],
  templateUrl: './woodcutting.component.html',
  styleUrl: './woodcutting.component.css',
})
export class WoodcuttingComponent {}
