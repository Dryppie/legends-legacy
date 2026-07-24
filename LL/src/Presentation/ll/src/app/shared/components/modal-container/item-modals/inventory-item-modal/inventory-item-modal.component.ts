import { NgFor, NgIf } from '@angular/common';
import { Component, EventEmitter, Input, OnInit, Output, signal } from '@angular/core';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { BlueprintLearningOption } from '../../../../models/crafting-v2';
import { InventoryItem } from '../../../../models/inventoryItem';
import { ItemComponent } from '../../../item/item.component';

@Component({
    selector: 'app-inventory-item-modal',
    imports: [NgFor, NgIf, ItemComponent],
    templateUrl: './inventory-item-modal.component.html'
})
export class InventoryItemModalComponent implements OnInit {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Output() close = new EventEmitter<void>();

  readonly isLearning = signal(false);
  readonly isLoadingOptions = signal(false);
  readonly error = signal<string | null>(null);
  readonly learningOptions = signal<BlueprintLearningOption[]>([]);
  readonly selectedRecipeId = signal<string | null>(null);

  constructor(
    private readonly craftingService: CraftingService,
    private readonly inventoryState: InventoryStateService,
  ) {}

  get itemName(): string {
    return (
      this.inventoryItem.itemInstance.displayName ||
      this.inventoryItem.itemInstance.itemBase.name
    );
  }

  get isBlueprint(): boolean {
    const itemBase = this.inventoryItem.itemInstance.itemBase;
    return (
      itemBase.id.toLowerCase().startsWith('blueprint_') ||
      itemBase.name.toLowerCase().startsWith('blueprint:')
    );
  }

  ngOnInit(): void {
    if (this.isBlueprint) {
      this.loadBlueprintLearningOptions();
    }
  }

  selectRecipe(recipeId: string): void {
    this.selectedRecipeId.set(recipeId);
  }

  learnBlueprint(): void {
    if (!this.isBlueprint || this.isLearning()) return;

    const recipeId = this.selectedRecipeId();
    if (!recipeId) {
      this.error.set('Select a recipe for this blueprint.');
      return;
    }

    this.isLearning.set(true);
    this.error.set(null);

    this.craftingService
      .learnBlueprint(this.inventoryItem.itemInstance.id, recipeId)
      .subscribe({
        next: () => {
          this.inventoryState.decrementItem(
            this.inventoryItem.itemInstance.id,
            1,
          );
          this.close.emit();
        },
        error: (err) => {
          this.error.set(err.message ?? 'Failed to learn blueprint.');
          this.isLearning.set(false);
        },
      });
  }

  trackByRecipeId(_: number, option: BlueprintLearningOption): string {
    return option.recipeId;
  }

  private loadBlueprintLearningOptions(): void {
    this.isLoadingOptions.set(true);
    this.error.set(null);

    this.craftingService
      .getBlueprintLearningOptions(this.inventoryItem.itemInstance.id)
      .subscribe({
        next: (options) => {
          this.learningOptions.set(options);
          this.selectedRecipeId.set(options[0]?.recipeId ?? null);
          this.isLoadingOptions.set(false);
        },
        error: (err) => {
          this.error.set(
            err.message ?? 'Failed to load blueprint learning options.',
          );
          this.isLoadingOptions.set(false);
        },
      });
  }
}
