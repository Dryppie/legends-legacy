import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NobilityService } from '../../../core/services/api/nobility/nobility.service';
import { RegularButtonComponent } from '../../../shared/components/custom-components/buttons/regular-button/regular-button.component';

@Component({
  selector: 'app-nobility-panel',
  imports: [CommonModule, FormsModule, RegularButtonComponent],
  templateUrl: './nobility-panel.component.html',
  styleUrl: './nobility-panel.component.scss',
})
export class NobilityPanelComponent implements OnInit {
  readonly nobility = inject(NobilityService);
  quantity = 1;
  readonly benefitColumns = [
    [
      {
        label: 'Capacity',
        benefits: [
          { label: 'Essence / Equipment presets', value: '6 each (+3)' },
          { label: 'Arena ticket cap', value: '8 (+3), 1 per 3h' },
          { label: 'Market orders', value: '30 sell + 30 buy' },
        ],
      },
    ],
    [
      {
        label: 'Time & cost',
        benefits: [
          { label: 'Offline retention', value: '7 days' },
          { label: 'Focus changes', value: 'Every 2 hours' },
          {
            label: 'Prophecy reroll costs',
            value: '0 / 0 / 40 / 80 Fate Echo',
          },
        ],
      },
    ],
  ];

  get maxQuantity(): number {
    return Math.min(1200, this.nobility.status()?.availableSignets ?? 0);
  }

  get canRedeem(): boolean {
    return (
      !this.nobility.busy() &&
      (this.nobility.pendingQuantity() !== null ||
        (Number.isInteger(this.quantity) &&
          this.quantity >= 1 &&
          this.quantity <= this.maxQuantity))
    );
  }

  setQuantity(value: number): void {
    this.quantity = value;
  }

  stepQuantity(delta: number): void {
    if (
      this.nobility.busy() ||
      this.nobility.pendingQuantity() !== null ||
      this.maxQuantity < 1
    )
      return;
    const current = Number.isInteger(this.quantity) ? this.quantity : 1;
    this.setQuantity(Math.min(this.maxQuantity, Math.max(1, current + delta)));
  }

  ngOnInit(): void {
    this.quantity = this.nobility.pendingQuantity() ?? 1;
    this.nobility.load();
  }
}
