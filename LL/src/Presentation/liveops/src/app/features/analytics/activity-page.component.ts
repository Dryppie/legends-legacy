import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsState } from './analytics-state.service';
import { ColumnHelpComponent } from '../../shared/column-help.component';
import { activityColumns, activityHelp } from './analytics-column-help';

@Component({
  selector: 'app-analytics-activity',
  standalone: true,
  imports: [CommonModule, FormsModule, ColumnHelpComponent],
  templateUrl: './activity-page.component.html',
  styleUrl: './analytics-page.css',
})
export class ActivityPageComponent {
  readonly columns = activityColumns;
  readonly help = activityHelp;
  constructor(public readonly state: AnalyticsState) {}
}
