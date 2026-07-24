import { NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { CraftingService } from '../../../../../core/services/api/crafting/crafting.service';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import {
  DropdownComponent,
  DropdownOption,
  DropdownSelection,
} from '../../../custom-components/dropdown/dropdown.component';
import { ItemComponent } from '../../../item/item.component';

@Component({
  selector: 'app-inventory-item-modal',
  imports: [NgIf, DropdownComponent, ItemComponent],
  templateUrl: './inventory-item-modal.component.html',
})
export class InventoryItemModalComponent implements OnInit {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Output() close = new EventEmitter<void>();
  readonly isLearning = signal(false);
  readonly isLoadingRecipes = signal(false);
  readonly hasLoadedRecipes = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedRecipeId = signal('');
  private readonly availableRecipeIds = signal<ReadonlySet<string>>(new Set());

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

  get blueprint() {
    return this.inventoryItem.itemInstance.itemBase.blueprint ?? null;
  }

  get compatibleRecipeOptions(): readonly DropdownOption<string>[] {
    const availableRecipeIds = this.availableRecipeIds();
    return (this.blueprint?.compatibleRecipes ?? [])
      .filter((recipe) => availableRecipeIds.has(recipe.id))
      .map((recipe) => ({
        label: recipe.name,
        value: recipe.id,
      }));
  }

  ngOnInit(): void {
    this.loadAvailableRecipes();
  }

  selectRecipe(selection: DropdownSelection<string>): void {
    this.selectedRecipeId.set(selection.main);
  }

  learnBlueprint(): void {
    const recipeId = this.selectedRecipeId();
    if (!this.blueprint || !recipeId || this.isLearning()) return;
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

  private loadAvailableRecipes(): void {
    const blueprint = this.blueprint;
    if (!blueprint) {
      this.hasLoadedRecipes.set(true);
      return;
    }

    const compatibleRecipeIds = new Set(
      blueprint.compatibleRecipes.map((recipe) => recipe.id),
    );
    this.isLoadingRecipes.set(true);
    this.craftingService.getRecipes().subscribe({
      next: (recipes) => {
        const availableRecipeIds = new Set(
          recipes
            .filter(
              (recipe) =>
                compatibleRecipeIds.has(recipe.id) &&
                recipe.blueprints.some(
                  (candidate) =>
                    candidate.id === blueprint.blueprintId &&
                    !candidate.isLearned,
                ),
            )
            .map((recipe) => recipe.id),
        );

        this.availableRecipeIds.set(availableRecipeIds);
        this.selectedRecipeId.set(
          blueprint.compatibleRecipes.find((recipe) =>
            availableRecipeIds.has(recipe.id),
          )?.id ?? '',
        );
        this.isLoadingRecipes.set(false);
        this.hasLoadedRecipes.set(true);
      },
      error: (err) => {
        this.error.set(err.message ?? 'Failed to load available base recipes.');
        this.isLoadingRecipes.set(false);
        this.hasLoadedRecipes.set(true);
      },
    });
  }
}
