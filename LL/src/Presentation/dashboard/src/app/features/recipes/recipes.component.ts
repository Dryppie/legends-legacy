import { Component, OnInit } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { CraftType, Recipe } from '../../shared/models/recipes';
import { ItemBase } from '../../shared/models/item';
import { ItemService } from '../../core/services/api/items/item.service';
import { RecipesService } from '../../core/services/api/recipes/recipes.service';
import { ItemType } from '../../shared/models/enums/itemType';
import { CommonModule, NgFor, NgIf } from '@angular/common';

@Component({
  selector: 'app-recipes',
  standalone: true,
  imports: [NgFor, NgIf, FormsModule, ReactiveFormsModule, CommonModule],
  templateUrl: './recipes.component.html',
  styleUrl: './recipes.component.css',
})
export class RecipesComponent implements OnInit {
  /* DATA */
  recipes: Recipe[] = [];
  items: ItemBase[] = [];
  craftableItems: ItemBase[] = [];

  /* UI STATE */
  selectedRecipe: Recipe | null = null;
  recipeForm!: FormGroup;

  /* ENUM-HELPERS */
  craftTypes = Object.values(CraftType);
  itemTypes = Object.values(ItemType);

  /* GETTERS */
  get materials(): FormArray {
    return this.recipeForm.get('materials') as FormArray;
  }
  get isCreating(): boolean {
    return !this.selectedRecipe;
  }

  constructor(
    private fb: FormBuilder,
    private recipeService: RecipesService,
    private itemService: ItemService,
  ) {}

  /* LIFE-CYCLE ---------------------------------------------------------- */
  ngOnInit(): void {
    this.loadItems();
  }

  /* FORM ---------------------------------------------------------------- */
  private buildForm(): void {
    this.recipeForm = this.fb.group({
      id: [''],
      name: ['', Validators.required],
      /* the finished product */
      item: [null, Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      craftType: [null, Validators.required],
      levelRequirement: [1, [Validators.required, Validators.min(1)]],
      /* derived from “item”, but editable if you prefer */
      itemType: [{ value: null, disabled: true }, Validators.required],
      /* materials FormArray */
      materials: this.fb.array([]),
    });

    /* whenever the output item changes, keep itemType in sync */
    this.recipeForm
      .get('item')!
      .valueChanges.subscribe((it: ItemBase | null) => {
        this.recipeForm.get('itemType')!.setValue(it ? it.itemType : null);

        /* auto-fill or clear the recipe name */
        const nameCtrl = this.recipeForm.get('name')!;
        if (it) {
          nameCtrl.setValue(it.name, { emitEvent: false });
        } else {
          nameCtrl.reset('', { emitEvent: false });
        }
      });
  }

  /* DATA LOAD ----------------------------------------------------------- */
  private loadItems(): void {
    this.itemService.getItems().subscribe((data) => {
      this.items = data;
      this.loadRecipes();
    });
  }

  private loadRecipes(): void {
    this.recipeService.getRecipes().subscribe((data) => {
      this.recipes = data;
      const outputItemIds = this.recipes.map((recipe) => recipe.item.id);
      this.craftableItems = this.items.filter(
        (i) =>
          i.itemType != ItemType.Material &&
          i.itemType != ItemType.Essence &&
          !outputItemIds.includes(i.id),
      );
      this.buildForm();
    });
  }

  /* CRUD ---------------------------------------------------------------- */
  newRecipe(): void {
    this.selectedRecipe = null;
    this.recipeForm.reset({
      id: '',
      name: '',
      item: this.craftableItems[0],
      quantity: 1,
      craftType: null,
      levelRequirement: 1,
      itemType: this.craftableItems[0].itemType,
    });
    this.clearMaterials();
  }

  get craftableOutputItems() {
    let items: ItemBase[] = [];
    if (this.selectedRecipe != null) {
      items = [this.selectedRecipe.item];
    }
    items = [...items, ...this.craftableItems];
    return items;
  }

  selectRecipe(recipe: Recipe): void {
    this.selectedRecipe = recipe;
    this.recipeForm.patchValue({
      ...recipe,
      /* itemType is disabled – patch it separately */
      itemType: recipe.itemType,
    });

    /* refresh materials list */
    this.clearMaterials();
    recipe.materials.forEach((m) =>
      this.materials.push(
        this.fb.group({
          item: [m.item, Validators.required],
          quantity: [m.quantity, [Validators.required, Validators.min(1)]],
        }),
      ),
    );
  }

  saveRecipe(): void {
    if (this.recipeForm.invalid) {
      this.recipeForm.markAllAsTouched();
      return;
    }

    const raw: Recipe = {
      ...this.recipeForm.getRawValue(),
      /* ensure disabled control value is carried over */
      name: this.recipeForm.get('item')!.value.name,
      itemType: this.recipeForm.get('itemType')!.value,
    };

    if (this.isCreating) {
      raw.id = crypto.randomUUID();
      this.recipeService.updateRecipe(raw).subscribe((created) => {
        this.recipes.push(created);
        this.selectRecipe(created);
        this.craftableItems = this.craftableItems.filter(
          (c) => c.id != created.item.id,
        );
      });
    } else {
      this.recipeService.updateRecipe(raw).subscribe((updated) => {
        const idx = this.recipes.findIndex((r) => r.id === updated.id);
        if (idx !== -1) this.recipes[idx] = updated;
        this.selectRecipe(updated);
      });
    }
  }

  /* MATERIALS ----------------------------------------------------------- */
  addMaterial(): void {
    this.materials.push(
      this.fb.group({
        item: [null, Validators.required],
        quantity: [1, [Validators.required, Validators.min(1)]],
      }),
    );
  }

  removeMaterial(i: number): void {
    this.materials.removeAt(i);
  }

  clearMaterials(): void {
    while (this.materials.length) this.materials.removeAt(0);
  }

  trackByIndex(i: number): number {
    return i;
  }

  compareItems = (a: ItemBase | null, b: ItemBase | null): boolean =>
    a && b ? a.id === b.id : a === b;
}
