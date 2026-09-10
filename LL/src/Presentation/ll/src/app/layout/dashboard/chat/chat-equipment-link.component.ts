import { NgClass, NgIf } from '@angular/common';
import {
  Component,
  computed,
  DestroyRef,
  inject,
  input,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { EquipmentService } from '../../../core/services/api/equipment/equipment.service';
import { EquipmentInstance } from '../../../shared/models/item';
import { PopoverComponent } from '../../../shared/components/custom-components/popover/popover.component';
import { EquipmentDisplayComponent } from '../../../shared/components/equipment/equipment-display/equipment-display.component';
import { Rarity } from '../../../shared/models/enums/rarity';

const RARITY_CLASSES: Record<Rarity, string> = {
  [Rarity.Common]: 'll-rarity-common',
  [Rarity.Uncommon]: 'll-rarity-uncommon',
  [Rarity.Rare]: 'll-rarity-rare',
  [Rarity.Epic]: 'll-rarity-epic',
  [Rarity.Unique]: 'll-rarity-unique',
  [Rarity.Legendary]: 'll-rarity-legendary',
  [Rarity.Legacy]: 'll-rarity-legacy',
};

@Component({
  selector: 'app-chat-equipment-link',
  imports: [NgClass, NgIf, PopoverComponent, EquipmentDisplayComponent],
  template: `<app-popover
      [template]="details"
      trigger="hover"
      [originFocusable]="focusable()"
      (opened)="load()"
    >
      <span class="cursor-help" [ngClass]="rarityClass()">[{{ name() }}]</span>
    </app-popover>
    <ng-template #details>
      <app-equipment-display
        *ngIf="equipment() as item; else status"
        [item]="item"
      ></app-equipment-display>
      <ng-template #status
        ><p class="max-w-xs text-zinc-300" role="status">
          {{ error() || 'Loading equipment...' }}
        </p></ng-template
      >
    </ng-template>`,
  host: { '(keydown)': '$event.stopPropagation()' },
})
export class ChatEquipmentLinkComponent {
  readonly equipmentId = input.required<string>();
  readonly name = input.required<string>();
  readonly rarity = input<Rarity>();
  readonly focusable = input(true);
  readonly equipment = signal<EquipmentInstance | null>(null);
  readonly error = signal('');
  private readonly resolvedRarity = signal<Rarity | null>(null);
  readonly rarityClass = computed(() => {
    const rarity = this.resolvedRarity() ?? this.rarity();
    return rarity
      ? (RARITY_CLASSES[rarity] ?? 'll-text-muted')
      : 'll-text-muted';
  });
  private readonly api = inject(EquipmentService);
  private readonly destroyRef = inject(DestroyRef);
  private loading = false;
  private loadedAt = 0;

  load(): void {
    if (
      this.loading ||
      (this.equipment() && Date.now() - this.loadedAt < 60_000)
    )
      return;
    this.loading = true;
    this.equipment.set(null);
    this.error.set('');
    this.api
      .getLinkedEquipment(this.equipmentId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (item) => {
          this.equipment.set(item);
          this.resolvedRarity.set(item.rarity);
          this.loadedAt = Date.now();
          this.loading = false;
        },
        error: (error) => {
          this.error.set(
            error?.status === 404
              ? 'This piece is no longer available.'
              : 'Unable to load this piece. Hover or tap again to retry.',
          );
          this.loading = false;
        },
      });
  }
}
