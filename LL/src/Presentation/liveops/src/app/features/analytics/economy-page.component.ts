import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsState } from './analytics-state.service';
import { ColumnHelpComponent } from '../../shared/column-help.component';

@Component({
  selector: 'app-analytics-economy',
  standalone: true,
  imports: [CommonModule, FormsModule, ColumnHelpComponent],
  templateUrl: './economy-page.component.html',
  styleUrl: './analytics-page.css',
})
export class EconomyPageComponent {
  constructor(public readonly state: AnalyticsState) {}
}
