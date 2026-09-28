import { Component, DestroyRef, EventEmitter, Input, OnInit, Output, inject } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EquipmentService, MigratedSpecializationChoice } from '../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { EquipmentProgressionItem } from '../../../models/equipment-progression';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeTypeFormatPipe } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';

@Component({
  selector: 'app-migrated-specialization',
  imports: [NgIf, NgFor, AttributeTypeFormatPipe, AttributeValueFormatPipe],
  template: `
    <p *ngIf="error && !choice" class="mb-3 text-xs text-red-300" role="alert">{{ error }}</p>
    <section *ngIf="choice" class="mb-4 rounded border border-primary/30 bg-black/20 p-3" aria-label="Free specialization choice">
      <h3 class="ll-heading">Choose a specialization</h3>
      <p class="ll-copy mt-1">This item has one free choice after the attribute update. Preview its stats, then use the choice when you are ready.</p>
      <label class="mt-3 block text-xs text-secondary">Specialization
        <select class="ll-input mt-1 w-full" [value]="selected?.definitionId" (change)="select($event)" [disabled]="saving">
          <option *ngFor="let option of choice.options" [value]="option.definitionId">{{ label(option) }}</option>
        </select>
      </label>
      <div class="mt-3 grid grid-cols-2 gap-2 text-xs" *ngIf="selected">
        <span *ngFor="let stat of stats">{{ stat.attribute | attributeTypeFormat: true }}: {{ stat.value | attributeValueFormat: stat.attribute: false: true }}</span>
      </div>
      <p *ngIf="error" class="mt-2 text-xs text-red-300" role="alert">{{ error }}</p>
      <button type="button" class="ll-button mt-3" [disabled]="saving || !selected" (click)="apply()">
        {{ saving ? 'Applying…' : 'Use my free choice' }}
      </button>
    </section>
  `,
})
export class MigratedSpecializationComponent implements OnInit {
  @Input({ required: true }) itemId!: string;
  @Output() completed = new EventEmitter<void>();
  private readonly api = inject(EquipmentService);
  private readonly equipment = inject(EquipmentStateService);
  private readonly inventory = inject(InventoryStateService);
  private readonly destroyRef = inject(DestroyRef);
  choice: MigratedSpecializationChoice | null = null;
  selected: EquipmentProgressionItem | null = null;
  saving = false;
  error = '';
  private operationId = crypto.randomUUID();

  ngOnInit(): void {
    this.api.getMigratedSpecializationChoice(this.itemId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: choice => { this.choice = choice; this.selected = choice?.options[0] ?? null; },
      error: () => { this.error = 'The free choice could not be loaded. Reopen this item to retry.'; },
    });
  }

  label(option: EquipmentProgressionItem): string {
    const id = option.allocation?.specializationId ?? 'Default';
    return id === 'default' ? 'Original specialization' : id.replaceAll('_', ' ');
  }

  get stats(): { attribute: AttributeType; value: number }[] {
    return Object.entries(this.selected?.stats ?? {}).map(([attribute, value]) => ({ attribute: attribute as AttributeType, value }));
  }

  select(event: Event): void {
    this.selected = this.choice?.options.find(x => x.definitionId === (event.target as HTMLSelectElement).value) ?? null;
    this.operationId = crypto.randomUUID();
    this.error = '';
  }

  apply(): void {
    if (!this.choice || !this.selected || this.saving) return;
    this.saving = true;
    this.error = '';
    this.api.chooseMigratedSpecialization(this.choice.migrationId, this.operationId, this.selected.definitionId)
      .pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
        next: () => { this.equipment.load(true); this.inventory.load(true); this.completed.emit(); },
        error: () => { this.saving = false; this.error = 'The choice could not be applied. Retry or reopen the item if it changed.'; },
      });
  }
}
