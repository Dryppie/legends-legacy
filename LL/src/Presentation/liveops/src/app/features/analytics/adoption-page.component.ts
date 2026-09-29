import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsState } from './analytics-state.service';
import { AdoptionTableComponent } from './adoption-table.component';

@Component({
  selector: 'app-analytics-adoption',
  standalone: true,
  imports: [CommonModule, FormsModule, AdoptionTableComponent],
  templateUrl: './adoption-page.component.html',
  styleUrl: './analytics-page.css',
})
export class AdoptionPageComponent {
  constructor(public readonly state: AnalyticsState) {}
}
