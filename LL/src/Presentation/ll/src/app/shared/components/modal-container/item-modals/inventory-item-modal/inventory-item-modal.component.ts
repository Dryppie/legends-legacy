import { itemDescription } from '../../../../utils/inventory/item-description';
import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  OnInit,
  Output,
  signal,
} from '@angular/core';
import { InventoryStateService } from '../../../../../core/services/api/inventory/inventory-state.service';
import { InventoryItem } from '../../../../models/inventoryItem';
import {
  DropdownOption,
  DropdownSelection,
} from '../../../custom-components/dropdown/dropdown.component';
import { ItemComponent } from '../../../item/item.component';
import { InventoryService } from '../../../../../core/services/api/inventory/inventory.service';
import { SelectionCrateOption } from '../../../../models/item';
import {
  initialSelectionContainerOptionId,
  selectionContainerMetadata,
} from '../../../../utils/inventory/selection-container.utils';
import { InventoryTransferComponent } from '../../../inventory-transfer/inventory-transfer.component';

@Component({
  selector: 'app-inventory-item-modal',
  imports: [
    NgFor,
    NgIf,
    ItemComponent,
    InventoryTransferComponent,
  ],
  templateUrl: './inventory-item-modal.component.html',
})
export class InventoryItemModalComponent implements OnInit {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Output() close = new EventEmitter<void>();
  readonly isOpeningCrate = signal(false);
  readonly error = signal<string | null>(null);
  readonly selectedCrateOptionId = signal('');

  readonly itemDescription = itemDescription;


  constructor(
    private readonly inventoryService: InventoryService,
    private readonly inventoryState: InventoryStateService,
  ) {}

  get itemName(): string {
    return (
      this.inventoryItem.itemInstance.displayName ||
      this.inventoryItem.itemInstance.itemBase.name
    );
  }

  get selectionCrate() {
    return selectionContainerMetadata(
      this.inventoryItem.itemInstance.itemBase,
    );
  }


  ngOnInit(): void {
    this.selectedCrateOptionId.set(
      initialSelectionContainerOptionId(
        this.inventoryItem.itemInstance.itemBase,
      ),
    );
  }

  selectCrateOption(option: SelectionCrateOption): void {
    this.selectedCrateOptionId.set(option.id);
  }

  openSelectionCrate(): void {
    const optionId = this.selectedCrateOptionId();
    if (!this.selectionCrate || !optionId || this.isOpeningCrate()) return;

    this.isOpeningCrate.set(true);
    this.error.set(null);
    this.inventoryService
      .openSelectionContainer(this.inventoryItem.itemInstance.id, optionId)
      .subscribe({
        next: (response) => {
          this.inventoryState.applyVersionedInventory(
            response,
            response.data.grantId,
          );
          this.close.emit();
        },
        error: (err) => {
          this.error.set(err.message ?? 'Failed to open the selection crate.');
          this.isOpeningCrate.set(false);
        },
      });
  }



}
