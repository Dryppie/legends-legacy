import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AnalyticsState } from './analytics-state.service';
import { ColumnHelpComponent } from '../../shared/column-help.component';
import {
  itemizationColumns, itemizationDistributionColumns, itemizationCombinationColumns,
  itemizationEssenceColumns, itemizationOutcomeColumns, itemizationChoiceColumns,
} from './analytics-column-help';

@Component({
  selector: 'app-analytics-itemization',
  standalone: true,
  imports: [CommonModule, FormsModule, ColumnHelpComponent],
  templateUrl: './itemization-page.component.html',
  styleUrl: './analytics-page.css',
})
export class ItemizationPageComponent {
  readonly columns = itemizationColumns;
  readonly distributionColumns = itemizationDistributionColumns;
  readonly combinationColumns = itemizationCombinationColumns;
  readonly essenceColumns = itemizationEssenceColumns;
  readonly outcomeColumns = itemizationOutcomeColumns;
  readonly choiceColumns = itemizationChoiceColumns;
  constructor(public readonly state: AnalyticsState) {}
}
