import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-profession-card',
  standalone: true,
  imports: [],
  templateUrl: './profession-card.component.html',
  styleUrl: './profession-card.component.css',
})
export class ProfessionCardComponent {
  @Input() gatheringNodeId!: string;
  @Input() gatheringNodeName!: string;
}
