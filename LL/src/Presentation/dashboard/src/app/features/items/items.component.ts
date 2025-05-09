import { NgFor, NgIf } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormArray,
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { SplitCamelCasePipe } from '../../shared/pipes/attributes/split-camel-case/split-camel-case.pipe';
import { Equipment, EssenceItem, ItemBase } from '../../shared/models/item';
import { ItemType } from '../../shared/models/enums/itemType';
import { Rarity } from '../../shared/models/enums/rarity';
import { EquipmentType } from '../../shared/models/Dtos/equipmentSlot';
import { ItemService } from '../../core/services/api/items/item.service';

@Component({
  selector: 'app-items',
  standalone: true,
  imports: [FormsModule, NgIf, ReactiveFormsModule, SplitCamelCasePipe, NgFor],
  templateUrl: './items.component.html',
  styleUrl: './items.component.css',
})
export class ItemsComponent implements OnInit {
  items: ItemBase[] = [];
  selectedItem: ItemBase | null = null;
  itemForm!: FormGroup;

  rarities = Object.values(Rarity);
  itemTypes = Object.values(ItemType);
  equipmentTypes = Object.values(EquipmentType);
  // essences = Object.values(Essence);

  get attributeModifiers(): FormArray {
    return this.itemForm.get('attributeModifiers') as FormArray;
  }

  get isCreating(): boolean {
    return !this.selectedItem;
  }

  constructor(
    private fb: FormBuilder,
    private itemService: ItemService,
  ) {}

  ngOnInit(): void {
    this.loadItems();
    this.buildForm();
  }

  /** Build an empty form; later patched with selected item */
  private buildForm(): void {
    this.itemForm = this.fb.group({
      id: [''],
      name: ['', Validators.required],
      rarity: [null, Validators.required],
      itemType: [null, Validators.required],
      description: [''],
      // Equipment
      equipmentType: [null],
      attributeModifiers: this.fb.array([]),
      // Essence
      essence: [null],
    });

    // whenever itemType changes we reset sub‑type specific controls
    this.itemForm.get('itemType')!.valueChanges.subscribe((type: ItemType) => {
      if (type === 'Equipment') {
        this.itemForm.get('essence')!.reset();
      } else if (type === 'Essence') {
        this.itemForm.get('equipmentType')!.reset();
        // clear modifiers
        while (this.attributeModifiers.length) {
          this.attributeModifiers.removeAt(0);
        }
      }
    });
  }

  /** Load items from API */
  private loadItems(): void {
    this.itemService.getItems().subscribe((data) => (this.items = data));
  }

  /** Create a fresh item */
  newItem(): void {
    this.selectedItem = null;
    this.itemForm.reset({
      id: '',
      name: '',
      rarity: null,
      itemType: null,
      description: '',
    });
    // clear modifiers array
    while (this.attributeModifiers.length) {
      this.attributeModifiers.removeAt(0);
    }
  }

  /** Select existing item for editing */
  selectItem(item: ItemBase): void {
    this.selectedItem = item;
    this.itemForm.patchValue(item);
    // handle subtype specifics
    if (item.itemType === 'Equipment') {
      const eq = item as Equipment;
      // patch equipmentType
      this.itemForm.get('equipmentType')!.setValue(eq.equipmentType);
      // clear and repopulate attribute modifiers
      while (this.attributeModifiers.length) {
        this.attributeModifiers.removeAt(0);
      }
      eq.attributeModifiers.forEach((m) =>
        this.attributeModifiers.push(
          this.fb.group({
            attributeType: [m.attributeType, Validators.required],
            amount: [m.amount, Validators.required],
          }),
        ),
      );
    } else if (item.itemType === 'Essence') {
      const es = item as EssenceItem;
      this.itemForm.get('essence')!.setValue(es.essence);
    }
  }

  /** Add empty attribute modifier row */
  addModifier(): void {
    this.attributeModifiers.push(
      this.fb.group({
        attributeType: ['', Validators.required],
        amount: [0, Validators.required],
      }),
    );
  }

  /** Remove modifier row by index */
  removeModifier(index: number): void {
    this.attributeModifiers.removeAt(index);
  }

  /** trackBy for ngFor FormArray controls */
  trackByIndex(index: number): number {
    return index;
  }

  /** Persist changes or create new item */
  saveItem(): void {
    if (this.itemForm.invalid) {
      this.itemForm.markAllAsTouched();
      return;
    }

    const formValue = this.itemForm.getRawValue();
    if (this.isCreating) {
      // generate temporary id if backend handles else
      formValue.id = this.generateId(formValue.name);
      this.itemService
        .updateItem(formValue as ItemBase)
        .subscribe((created) => {
          this.items.push(created);
          this.selectItem(created);
        });
    } else {
      this.itemService
        .updateItem(formValue as ItemBase)
        .subscribe((updated) => {
          const idx = this.items.findIndex((i) => i.id === updated.id);
          if (idx !== -1) this.items[idx] = updated;
          this.selectItem(updated);
        });
    }
  }

  generateId(name: string): string {
    return name.toLowerCase().replace(/[^a-z0-9]/g, '_');
  }
}
