import { ConnectedPosition, OverlayModule } from '@angular/cdk/overlay';
import { NgFor, NgIf } from '@angular/common';
import {
  Component,
  EventEmitter,
  HostBinding,
  Input,
  OnChanges,
  OnDestroy,
  Output,
  SimpleChanges,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  catchError,
  distinctUntilChanged,
  finalize,
  map,
  of,
  Subject,
  switchMap,
  takeUntil,
  timer,
} from 'rxjs';
import { CharacterService } from '../../../core/services/api/character/character.service';
import { InventoryStateService } from '../../../core/services/api/inventory/inventory-state.service';
import { InventoryService } from '../../../core/services/api/inventory/inventory.service';
import { EquipmentInstance } from '../../models/item';
import { InventoryItem } from '../../models/inventoryItem';

@Component({
  selector: 'app-inventory-transfer',
  imports: [FormsModule, NgFor, NgIf, OverlayModule],
  templateUrl: './inventory-transfer.component.html',
})
export class InventoryTransferComponent implements OnChanges, OnDestroy {
  @Input({ required: true }) inventoryItem!: InventoryItem;
  @Input() compact = false;
  @Output() transferred = new EventEmitter<void>();

  readonly isFormOpen = signal(false);
  readonly isTransferring = signal(false);
  readonly error = signal<string | null>(null);
  readonly recipientSuggestions = signal<string[]>([]);
  readonly isSearchingRecipients = signal(false);
  readonly recipientSearchCompleted = signal(false);
  readonly recipientSuggestionPanelOpen = signal(false);
  readonly activeRecipientSuggestion = signal(-1);
  readonly hasSelectedRecipient = signal(false);
  readonly recipientSuggestionPositions: ConnectedPosition[] = [
    {
      originX: 'start',
      originY: 'top',
      overlayX: 'start',
      overlayY: 'bottom',
      offsetY: -4,
    },
    {
      originX: 'start',
      originY: 'bottom',
      overlayX: 'start',
      overlayY: 'top',
      offsetY: 4,
    },
  ];
  recipientName = '';
  quantity = 1;
  private readonly recipientSearch = new Subject<string>();
  private readonly destroy = new Subject<void>();

  @HostBinding('class.inventory-transfer-compact')
  get compactClass(): boolean {
    return this.compact;
  }

  @HostBinding('class.inventory-transfer-form-open')
  get formOpenClass(): boolean {
    return this.compact && this.isFormOpen();
  }

  constructor(
    private readonly inventoryService: InventoryService,
    private readonly inventoryState: InventoryStateService,
    private readonly characterService: CharacterService,
  ) {
    this.recipientSearch
      .pipe(
        map((prefix) => prefix.trim()),
        distinctUntilChanged(),
        switchMap((prefix) => {
          if (prefix.length < 2) {
            this.isSearchingRecipients.set(false);
            return of([] as string[]);
          }

          this.isSearchingRecipients.set(true);
          return timer(200).pipe(
            switchMap(() =>
              this.characterService
                .suggestCharacterNames(prefix)
                .pipe(catchError(() => of([] as string[]))),
            ),
          );
        }),
        takeUntil(this.destroy),
      )
      .subscribe((suggestions) => {
        this.isSearchingRecipients.set(false);
        this.recipientSearchCompleted.set(true);
        this.recipientSuggestions.set(suggestions);
        this.activeRecipientSuggestion.set(suggestions.length ? 0 : -1);
      });
  }

  ngOnChanges(changes: SimpleChanges): void {
    const itemChange = changes['inventoryItem'];
    if (!itemChange || itemChange.firstChange) return;

    const previous = itemChange.previousValue as InventoryItem | undefined;
    const current = itemChange.currentValue as InventoryItem | undefined;
    if (previous?.itemInstance.id !== current?.itemInstance.id) {
      this.resetForm();
      return;
    }

    if (current && this.quantity > current.quantity) {
      this.quantity = Math.max(1, current.quantity);
    }
  }

  ngOnDestroy(): void {
    this.destroy.next();
    this.destroy.complete();
  }

  get isStackable(): boolean {
    return this.inventoryItem.itemInstance.itemBase.stackable;
  }

  get inventoryRefreshing(): boolean {
    return this.inventoryState.loading();
  }

  get transferRestriction(): string | null {
    if (this.inventoryItem.itemInstance.itemBase.isBound) {
      return 'Bound items cannot be transferred.';
    }

    const equipment = this.inventoryItem
      .itemInstance as Partial<EquipmentInstance>;
    if (equipment.isGuildBorrowed) {
      return 'Borrowed guild-vault items cannot be transferred.';
    }

    return null;
  }

  openForm(): void {
    if (this.transferRestriction) return;
    this.error.set(null);
    this.recipientName = '';
    this.hasSelectedRecipient.set(false);
    this.recipientSuggestions.set([]);
    this.recipientSearchCompleted.set(false);
    this.closeRecipientSuggestions();
    this.quantity = 1;
    this.isFormOpen.set(true);
  }

  cancel(): void {
    if (this.isTransferring()) return;
    this.resetForm();
  }

  private resetForm(): void {
    this.error.set(null);
    this.recipientName = '';
    this.hasSelectedRecipient.set(false);
    this.recipientSuggestions.set([]);
    this.recipientSearchCompleted.set(false);
    this.isSearchingRecipients.set(false);
    this.closeRecipientSuggestions();
    this.isFormOpen.set(false);
    this.recipientSearch.next('');
  }

  onRecipientNameChange(value: string): void {
    this.recipientName = value;
    this.error.set(null);
    this.recipientSearchCompleted.set(false);
    this.activeRecipientSuggestion.set(-1);
    this.hasSelectedRecipient.set(false);

    const prefix = value.trim();
    if (prefix.length < 2) {
      this.recipientSuggestions.set([]);
      this.isSearchingRecipients.set(false);
      this.recipientSuggestionPanelOpen.set(false);
    } else {
      this.recipientSuggestionPanelOpen.set(true);
    }

    this.recipientSearch.next(value);
  }

  openRecipientSuggestions(): void {
    if (this.recipientName.trim().length >= 2) {
      this.recipientSuggestionPanelOpen.set(true);
    }
  }

  closeRecipientSuggestions(): void {
    this.recipientSuggestionPanelOpen.set(false);
    this.activeRecipientSuggestion.set(-1);
  }

  selectRecipient(event: Event, name: string): void {
    event.preventDefault();
    this.recipientName = name;
    this.hasSelectedRecipient.set(true);
    this.recipientSuggestions.set([]);
    this.recipientSearchCompleted.set(false);
    this.closeRecipientSuggestions();
  }

  handleRecipientKeydown(event: KeyboardEvent): void {
    const suggestions = this.recipientSuggestions();
    if (event.key === 'Escape') {
      this.closeRecipientSuggestions();
      return;
    }

    if (!this.recipientSuggestionPanelOpen() || !suggestions.length) return;

    if (event.key === 'ArrowDown' || event.key === 'ArrowUp') {
      event.preventDefault();
      const direction = event.key === 'ArrowDown' ? 1 : -1;
      const nextIndex =
        (this.activeRecipientSuggestion() + direction + suggestions.length) %
        suggestions.length;
      this.activeRecipientSuggestion.set(nextIndex);
      return;
    }

    if (event.key === 'Enter' && this.activeRecipientSuggestion() >= 0) {
      event.preventDefault();
      this.selectRecipient(
        event,
        suggestions[this.activeRecipientSuggestion()],
      );
    }
  }

  showRecipientSuggestionPanel(): boolean {
    return (
      this.recipientSuggestionPanelOpen() &&
      this.recipientName.trim().length >= 2
    );
  }

  transfer(): void {
    if (this.inventoryRefreshing) return;

    const recipientName = this.recipientName.trim();
    const quantity = Math.floor(Number(this.quantity));
    if (!recipientName || !this.hasSelectedRecipient()) {
      this.error.set('Choose a player from the suggestions.');
      return;
    }
    if (!Number.isFinite(quantity) || quantity < 1) {
      this.error.set('Enter a valid quantity.');
      return;
    }
    if (quantity > this.inventoryItem.quantity) {
      this.error.set('You do not have enough of this item.');
      return;
    }

    this.isTransferring.set(true);
    this.error.set(null);
    this.inventoryService
      .transferItem(this.inventoryItem.itemInstance.id, recipientName, quantity)
      .pipe(finalize(() => this.isTransferring.set(false)))
      .subscribe({
        next: () => {
          this.inventoryState.decrementItem(
            this.inventoryItem.itemInstance.id,
            quantity,
          );
          this.transferred.emit();
        },
        error: (err) => {
          this.error.set(
            err.errorMessage ?? err.message ?? 'Failed to transfer the item.',
          );
          this.inventoryState.load(true);
        },
      });
  }
}
