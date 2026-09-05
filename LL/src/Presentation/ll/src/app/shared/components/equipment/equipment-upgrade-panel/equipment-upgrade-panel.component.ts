import { NgClass, NgFor, NgIf } from '@angular/common';
import {
  Component,
  DestroyRef,
  EventEmitter,
  Input,
  OnInit,
  Output,
  inject,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  EquipmentService,
  EquipmentUpgradeMutation,
  EquipmentUpgradeQuote,
  EquipmentBlueprintOption,
} from '../../../../core/services/api/equipment/equipment.service';
import { EquipmentStateService } from '../../../../core/services/api/equipment/equipment-state.service';
import { InventoryStateService } from '../../../../core/services/api/inventory/inventory-state.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { EquipmentInstance } from '../../../models/item';
import { AttributeType } from '../../../models/enums/attributeType';
import { AttributeTypeFormatPipe } from '../../../pipes/attributes/attribute-type-format/attribute-type-format.pipe';
import { AttributeValueFormatPipe } from '../../../pipes/attributes/attribute-value-format/attribute-value-format.pipe';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../custom-components/dropdown/dropdown.component';

interface ReinforcementStatChange {
  attributeType: AttributeType;
  before: number;
  after: number;
  difference: number;
}

type EquipmentManagementSection = 'reinforce' | 'variant';

@Component({
  selector: 'app-equipment-upgrade-panel',
  imports: [
    NgIf,
    NgFor,
    NgClass,
    DropdownComponent,
    AttributeTypeFormatPipe,
    AttributeValueFormatPipe,
  ],
  templateUrl: './equipment-upgrade-panel.component.html',
})
export class EquipmentUpgradePanelComponent implements OnInit {
  private readonly destroyRef = inject(DestroyRef);

  @Input({ required: true }) equipmentInstance!: EquipmentInstance;
  @Input() tabbed = false;
  @Output() completed = new EventEmitter<void>();

  activeSection: EquipmentManagementSection = 'reinforce';

  reinforceQuote: EquipmentUpgradeQuote | null = null;
  loading = false;
  mutating = false;
  error: string | null = null;
  blueprints: EquipmentBlueprintOption[] = [];
  selectedBlueprint: EquipmentBlueprintOption | null = null;
  variantQuote: EquipmentUpgradeQuote | null = null;
  variantLoading = false;
  blueprintError: string | null = null;

  constructor(
    private readonly equipmentApi: EquipmentService,
    private readonly inventoryState: InventoryStateService,
    private readonly equipmentState: EquipmentStateService,
    private readonly characterState: CharacterStateService,
  ) {}

  ngOnInit(): void {
    this.loadQuotes();
    this.equipmentApi
      .getBlueprints(this.equipmentInstance.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (options) => {
          this.blueprints = options;
        },
        error: () => {
          this.blueprintError =
            'Blueprint options could not be loaded. Reopen this item to retry.';
        },
      });
  }

  selectSection(section: EquipmentManagementSection): void {
    this.activeSection = section;
  }

  selectBlueprint(styleId: string): void {
    if (this.mutating) return;
    this.selectedBlueprint =
      this.blueprints.find((x) => x.styleId === styleId) ?? null;
    this.variantQuote = null;
    this.variantLoading = false;
    this.blueprintError = null;
    if (!this.selectedBlueprint) return;
    this.variantLoading = true;
    this.equipmentApi
      .previewUpgrade(this.equipmentInstance.id, 'ApplyVariant', false, styleId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (quote) => {
          if (this.selectedBlueprint?.styleId !== styleId) return;
          this.variantQuote = quote;
          this.variantLoading = false;
        },
        error: () => {
          if (this.selectedBlueprint?.styleId !== styleId) return;
          this.variantLoading = false;
          this.blueprintError =
            'Conversion preview failed. Select the blueprint again to retry.';
        },
      });
  }

  get blueprintDropdownOptions(): DropdownOption<string>[] {
    return this.blueprints.map((blueprint) => ({
      label: `${blueprint.name} — ${blueprint.held} held${blueprint.isCurrent ? ' (current)' : ''}`,
      value: blueprint.styleId,
    }));
  }

  selectBlueprintFromDropdown(selection: DropdownSelection<unknown>): void {
    this.selectBlueprint(String(selection.main));
  }

  get variantStatChanges(): ReinforcementStatChange[] {
    const before = this.variantQuote?.before?.stats;
    const after = this.variantQuote?.after?.stats;
    if (!before || !after) return [];
    return [...new Set([...Object.keys(before), ...Object.keys(after)])].map(
      (attributeType) => ({
        attributeType: attributeType as AttributeType,
        before: before[attributeType] ?? 0,
        after: after[attributeType] ?? 0,
        difference: (after[attributeType] ?? 0) - (before[attributeType] ?? 0),
      }),
    );
  }

  applyVariant(): void {
    if (!this.variantQuote?.canExecute || this.mutating || this.variantLoading)
      return;
    this.mutating = true;
    this.error = null;
    this.equipmentApi
      .applyVariant(this.variantQuote)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.finishMutation(result),
        error: (error) => {
          this.handleMutationError(error);
          if (this.selectedBlueprint)
            this.selectBlueprint(this.selectedBlueprint.styleId);
        },
      });
  }

  get reinforcementStatChanges(): ReinforcementStatChange[] {
    const before = this.reinforceQuote?.before?.stats;
    const after = this.reinforceQuote?.after?.stats;
    if (!before || !after) return [];

    return Object.entries(after).map(([attributeType, amount]) => ({
      attributeType: attributeType as AttributeType,
      before: before[attributeType] ?? 0,
      after: amount,
      difference: amount - (before[attributeType] ?? 0),
    }));
  }

  reinforce(): void {
    if (!this.reinforceQuote?.canExecute || this.mutating) return;

    this.mutating = true;
    this.error = null;
    this.equipmentApi
      .reinforce(this.reinforceQuote)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.finishMutation(result),
        error: (error) => this.handleMutationError(error),
      });
  }

  private loadQuotes(resetError = true): void {
    this.loading = true;
    if (resetError) this.error = null;
    const reinforce = this.equipmentApi.previewUpgrade(
      this.equipmentInstance.id,
      'Reinforce',
    );

    reinforce.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (quote) => {
        this.reinforceQuote = quote;
        this.loading = false;
      },
      error: (error: unknown) => {
        this.error = this.errorMessage(error);
        this.loading = false;
      },
    });
  }

  private finishMutation(result: EquipmentUpgradeMutation): void {
    if (!result.outcome) {
      if (result.freshQuote?.request.kind === 'ApplyVariant')
        this.variantQuote = result.freshQuote;
      this.error =
        result.freshQuote?.unavailableReason ??
        'The equipment changed. Review the new quote.';
      this.reinforceQuote =
        result.freshQuote?.request.kind === 'Reinforce'
          ? result.freshQuote
          : this.reinforceQuote;
      this.mutating = false;
      return;
    }

    this.inventoryState.load(true);
    this.equipmentState.load(true);
    this.characterState.refreshCurrentCharacter();
    this.completed.emit();
  }

  private handleMutationError(error: unknown): void {
    this.error = this.errorMessage(error);
    this.mutating = false;
    this.loadQuotes(false);
  }

  private errorMessage(error: unknown): string {
    if (typeof error === 'object' && error && 'errorMessage' in error) {
      return String((error as { errorMessage: unknown }).errorMessage);
    }
    return 'The equipment-upgrade request failed.';
  }
}
