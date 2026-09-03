import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom, forkJoin, of } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { EquipmentProgressionService } from '../../../../core/services/api/equipment/equipment-progression.service';
import { InventoryService } from '../../../../core/services/api/inventory/inventory.service';
import { EquipmentService } from '../../../../core/services/api/equipment/equipment.service';
import { CharacterStateService } from '../../../../core/services/api/character/character-state.service';
import { EquipmentInstance } from '../../../../shared/models/item';
import { InventoryItem } from '../../../../shared/models/inventoryItem';
import {
  EquipmentAccess,
  ForgeMutation,
  ForgeQuote,
  ForgeRequest,
  ForgeStyle,
  CombatAcquisition,
  EquipmentProtectionPool,
  EquipmentProgressionRecoveryOption,
  PlainEquipmentRecoveryOption,
  StarterEquipmentKind,
  StarterEquipmentOption,
  hasEquipmentProgressionAccess,
} from '../../../../shared/models/equipment-progression';

interface PendingAction {
  path: string;
  body: object;
  label: string;
  quote: ForgeQuote | null;
}

@Injectable()
export class ForgeStateService {
  private readonly api = inject(EquipmentProgressionService);
  private readonly inventoryApi = inject(InventoryService);
  private readonly equipmentApi = inject(EquipmentService);
  private readonly character = inject(CharacterStateService);
  readonly access = signal<EquipmentAccess | null>(null);
  readonly enabled = computed(() =>
    hasEquipmentProgressionAccess(this.access()),
  );
  readonly loading = signal(false);
  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly message = signal<string | null>(null);
  readonly starters = signal<StarterEquipmentOption[]>([]);
  readonly ordinaryPools = signal<CombatAcquisition[]>([]);
  readonly selectedOrdinaryPoolId = signal('');
  readonly ordinary = computed(
    () =>
      this.ordinaryPools().find(
        (p) => p.poolId === this.selectedOrdinaryPoolId(),
      ) ?? null,
  );
  readonly pools = signal<EquipmentProtectionPool[]>([]);
  readonly recovery = signal<EquipmentProgressionRecoveryOption[]>([]);
  readonly plainRecovery = signal<PlainEquipmentRecoveryOption[]>([]);
  readonly inventory = signal<InventoryItem[]>([]);
  readonly equipment = signal<
    { item: EquipmentInstance; equipped: boolean; favorite: boolean }[]
  >([]);
  readonly styles = signal<ForgeStyle[]>([]);
  readonly selectedItemId = signal('');
  readonly selectedItem = computed(() =>
    this.equipment().find((x) => x.item.id === this.selectedItemId()),
  );
  readonly quote = signal<ForgeQuote | null>(null);
  readonly pending = signal<PendingAction | null>(null);
  readonly locked = computed(
    () => this.loading() || this.busy() || !!this.pending(),
  );
  readonly cinders = computed(
    () => this.character.currentCharacter()?.cinders ?? 0,
  );
  readonly scrap = computed(() =>
    this.inventory()
      .filter((x) => x.itemInstance.itemBase.id === 'tempered_scrap')
      .reduce((sum, x) => sum + x.quantity, 0),
  );
  readonly books = computed(() =>
    this.styles().flatMap((style) => {
      const book = this.inventory().find(
        (x) =>
          x.itemInstance.itemBase.id === style.itemBaseId && x.quantity > 0,
      );
      return book ? [{ style, book }] : [];
    }),
  );
  private storageKey = '';
  private selectionEpoch = 0;

  async initialize(): Promise<void> {
    this.storageKey = `ll.forge.pending.${this.character.currentCharacterId()}`;
    try {
      const stored = sessionStorage.getItem(this.storageKey);
      if (stored) {
        const pending = JSON.parse(stored) as PendingAction;
        if (pending.path && pending.body && pending.label) {
          this.pending.set(pending);
          this.quote.set(pending.quote);
          this.message.set(
            'An earlier request has an unknown result. Retry it to retrieve the saved outcome.',
          );
        }
      }
    } catch {
      /* Storage may be unavailable; the in-memory request still supports retries. */
    }
    await this.reload();
  }

  async reload(): Promise<void> {
    if (this.loading() || this.busy()) return;
    this.loading.set(true);
    this.error.set(null);
    if (!this.pending()) this.quote.set(null);
    try {
      const access = await firstValueFrom(this.api.access());
      this.access.set(access);
      if (!hasEquipmentProgressionAccess(access)) return;
      const data = await firstValueFrom(
        forkJoin({
          starters: access.starterAcquisitionEnabled
            ? this.api.starters()
            : of([]),
          ordinary: access.ordinaryAcquisitionEnabled
            ? this.api.ordinary()
            : of([]),
          pools: access.protectedAcquisitionEnabled
            ? this.api.sources()
            : of([]),
          recovery: access.baselineRecoveryEnabled
            ? this.api.recovery()
            : of([]),
          plainRecovery: access.baselineRecoveryEnabled
            ? this.api.plainRecovery()
            : of([]),
          inventory: this.inventoryApi.getInventory(),
          slots: this.equipmentApi.getEquipment(),
        }),
      );
      this.starters.set(data.starters);
      this.ordinaryPools.set(data.ordinary);
      if (
        !data.ordinary.some((p) => p.poolId === this.selectedOrdinaryPoolId())
      ) {
        this.selectedOrdinaryPoolId.set(
          [...data.ordinary].reverse().find((p) => p.hasEnteredRegion)
            ?.poolId ??
            data.ordinary[0]?.poolId ??
            '',
        );
      }
      this.pools.set(data.pools);
      this.recovery.set(data.recovery);
      this.plainRecovery.set(data.plainRecovery);
      this.inventory.set(data.inventory.inventoryItems);
      const equipment = new Map<
        string,
        { item: EquipmentInstance; equipped: boolean; favorite: boolean }
      >();
      for (const row of data.inventory.inventoryItems) {
        const item = row.itemInstance as EquipmentInstance;
        if (item.progression)
          equipment.set(item.id, {
            item,
            equipped: false,
            favorite: !!(row.isFavorite || item.isFavorite),
          });
      }
      for (const slot of data.slots) {
        const item = slot.equipmentInstance;
        if (item?.progression)
          equipment.set(item.id, {
            item,
            equipped: true,
            favorite: !!item.isFavorite,
          });
      }
      this.equipment.set([...equipment.values()]);
      const selected = equipment.has(this.selectedItemId())
        ? this.selectedItemId()
        : (equipment.keys().next().value ?? '');
      await this.loadStyles(selected);
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.loading.set(false);
    }
  }

  async selectItem(id: string): Promise<void> {
    if (this.locked()) return;
    this.quote.set(null);
    await this.loadStyles(id);
  }

  private async loadStyles(id: string): Promise<void> {
    const epoch = ++this.selectionEpoch;
    this.selectedItemId.set(id);
    this.styles.set([]);
    if (!this.access()?.forgeEnabled) return;
    const contextId = id || this.inventory()[0]?.itemInstance.id;
    if (!contextId) return;
    try {
      const styles = await firstValueFrom(this.api.styles(contextId));
      if (epoch === this.selectionEpoch) this.styles.set(styles);
    } catch (error) {
      if (epoch === this.selectionEpoch) this.error.set(errorMessage(error));
    }
  }

  async preview(request: ForgeRequest): Promise<void> {
    if (this.locked()) return;
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    this.quote.set(null);
    try {
      this.quote.set(await firstValueFrom(this.api.preview(request)));
    } catch (error) {
      this.error.set(errorMessage(error));
    } finally {
      this.busy.set(false);
    }
  }

  async confirmQuote(): Promise<void> {
    const quote = this.quote();
    if (!quote?.canExecute || this.locked()) return;
    const paths = {
      ImproveRank: 'rank',
      ChangeStyle: 'style',
      Salvage: 'salvage',
      LearnStyle: 'learn',
    };
    await this.start({
      path: `forge/${paths[quote.request.kind]}`,
      body: {
        operationId: quote.operationId,
        itemInstanceId: quote.request.itemInstanceId,
        styleId: quote.request.styleId ?? null,
        allowFavoriteSalvage: quote.request.allowFavoriteSalvage ?? false,
        quoteToken: quote.token,
      },
      label: 'Forge operation completed.',
      quote,
    });
  }

  async claim(
    kind: StarterEquipmentKind,
    definitionIds: string[],
  ): Promise<void> {
    await this.start({
      path: 'equipment/starter-claim',
      body: { kind, definitionIds },
      label: 'Starter equipment claimed. Equip it from Inventory.',
      quote: null,
    });
  }

  async selectOrdinary(
    definitionId: string,
    sigilFamilyId: string,
  ): Promise<void> {
    const pool = this.ordinary();
    if (!pool) return;
    await this.start({
      path: 'equipmentacquisition/ordinary',
      body: {
        operationId: crypto.randomUUID(),
        definitionId: definitionId || null,
        sigilFamilyId: sigilFamilyId || null,
        poolId: pool.poolId,
      },
      label: `${pool.regionName} choices saved. Earned combat used your previous choices.`,
      quote: null,
    });
  }

  async selectTarget(poolId: string, definitionId: string): Promise<void> {
    await this.start({
      path: 'equipmentacquisition/target',
      body: { poolId, definitionId: definitionId || null },
      label:
        'Dungeon target saved for new runs. Existing runs keep their original target.',
      quote: null,
    });
  }

  async recover(kind: StarterEquipmentKind): Promise<void> {
    await this.start({
      path: 'equipmentacquisition/recovery',
      body: { operationId: crypto.randomUUID(), kind },
      label: 'Missing starter equipment restored at rank 0, plain and bound.',
      quote: null,
    });
  }

  async recoverPlain(definitionId: string, tier: number): Promise<void> {
    await this.start({
      path: 'equipmentacquisition/plain-recovery',
      body: { operationId: crypto.randomUUID(), definitionId, tier },
      label: 'Missing earned equipment restored at rank 0, plain and bound.',
      quote: null,
    });
  }

  private async start(action: PendingAction): Promise<void> {
    if (this.locked()) return;
    this.setPending(action);
    await this.retry();
  }

  async retry(): Promise<void> {
    const action = this.pending();
    if (!action || this.busy() || this.loading()) return;
    this.busy.set(true);
    this.error.set(null);
    this.message.set(null);
    let succeeded = false;
    try {
      const result = await firstValueFrom(
        this.api.mutate<ForgeMutation>(action.path, action.body),
      );
      this.setPending(null);
      this.quote.set(null);
      this.message.set(
        result?.outcome
          ? `${action.label} Spent ${result.outcome.scrapSpent} Scrap and ${result.outcome.cindersSpent} Cinders; returned ${result.outcome.scrapReturned} Scrap.`
          : action.label,
      );
      succeeded = true;
    } catch (error) {
      this.error.set(errorMessage(error));
      const http = error instanceof HttpErrorResponse ? error : null;
      if (
        http &&
        http.status >= 400 &&
        http.status < 500 &&
        http.status !== 408 &&
        http.status !== 429
      ) {
        this.setPending(null);
        this.quote.set(http.error?.freshQuote ?? null);
        if (http.error?.freshQuote)
          this.message.set(
            'The quote changed. Review it and confirm again. Nothing was applied by this request.',
          );
      } else {
        this.message.set(
          'The result is unknown. Retry this same request before starting another action.',
        );
      }
    } finally {
      this.busy.set(false);
    }
    if (succeeded) await this.reload();
  }

  private setPending(action: PendingAction | null): void {
    this.pending.set(action);
    try {
      if (action)
        sessionStorage.setItem(this.storageKey, JSON.stringify(action));
      else sessionStorage.removeItem(this.storageKey);
    } catch {
      /* Keep the exact request in memory if browser storage is disabled. */
    }
  }
}

function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse)
    return error.error?.detail || error.error?.errorMessage || error.message;
  return error instanceof Error
    ? error.message
    : 'The equipment request failed. Please try again.';
}
