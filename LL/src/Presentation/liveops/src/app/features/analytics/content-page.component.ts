import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsState } from './analytics-state.service';
import { ColumnHelpComponent } from '../../shared/column-help.component';
import { contentColumns } from './analytics-column-help';

@Component({
  selector: 'app-analytics-content',
  standalone: true,
  imports: [CommonModule, FormsModule, ColumnHelpComponent],
  templateUrl: './content-page.component.html',
  styleUrl: './analytics-page.css',
})
export class ContentPageComponent {
  readonly columns = contentColumns;
  constructor(public readonly state: AnalyticsState) {}
}
