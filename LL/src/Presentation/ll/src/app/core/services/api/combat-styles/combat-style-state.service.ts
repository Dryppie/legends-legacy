import { Injectable, computed, signal } from '@angular/core';
import { finalize } from 'rxjs/operators';
import {
  CombatStyleDto,
  CombatStylesOverviewDto,
} from '../../../../shared/models/combat-style';
import { ToastService } from '../../client-side/components/toast/toast.service';
import { CombatStyleService } from './combat-style.service';

@Injectable({
  providedIn: 'root',
})
export class CombatStyleStateService {
  private readonly _overview = signal<CombatStylesOverviewDto | null>(null);
  private readonly _loading = signal(false);
  private readonly _saving = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly overview = computed(() => this._overview());
  readonly loading = computed(() => this._loading());
  readonly saving = computed(() => this._saving());
  readonly error = computed(() => this._error());

  readonly styles = computed(() => this._overview()?.styles ?? []);
  readonly activeStyle = computed<CombatStyleDto | null>(() => {
    const overview = this._overview();
    return overview?.styles.find((style) => style.isActive) ?? null;
  });

  constructor(
    private readonly service: CombatStyleService,
    private readonly toast: ToastService,
  ) {}

  refresh(): void {
    this._loading.set(true);
    this._error.set(null);

    this.service
      .getCombatStyles()
      .pipe(finalize(() => this._loading.set(false)))
      .subscribe({
        next: (overview) => this._overview.set(overview),
        error: (error) =>
          this._error.set(error.message ?? 'Failed to load Combat Styles'),
      });
  }

  activateStyle(styleId: string): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.service
      .activateStyle(styleId)
      .pipe(finalize(() => this._saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success) {
            this._error.set(response.message ?? 'Failed to activate Combat Style');
            return;
          }

          this.toast.showToast(
            response.message ?? 'Combat Style activated.',
            '',
            true,
          );
          this.refresh();
        },
        error: (error) =>
          this._error.set(error.message ?? 'Failed to activate Combat Style'),
      });
  }

  selectFocus(styleId: string, focusId: string): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.service
      .selectFocus(styleId, focusId)
      .pipe(finalize(() => this._saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response) {
            this._error.set('Failed to select Focus Path');
            return;
          }

          this.toast.showToast('Focus Path selected.', '', true);
          this.refresh();
        },
        error: (error) =>
          this._error.set(error.message ?? 'Failed to select Focus Path'),
      });
  }

  rankUpNode(styleId: string, nodeId: string): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.service
      .rankUpNode(styleId, nodeId)
      .pipe(finalize(() => this._saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success || !response.style) {
            this._error.set(response.message ?? 'Failed to rank up skill node');
            return;
          }

          this.replaceStyle(response.style);
          this.toast.showToast(response.message ?? 'Skill node ranked up.', '', true);
        },
        error: (error) =>
          this._error.set(error.message ?? 'Failed to rank up skill node'),
      });
  }

  resetTree(styleId: string): void {
    if (this._saving()) return;

    this._saving.set(true);
    this._error.set(null);

    this.service
      .resetTree(styleId)
      .pipe(finalize(() => this._saving.set(false)))
      .subscribe({
        next: (response) => {
          if (!response.success || !response.style) {
            this._error.set(response.message ?? 'Failed to reset skill tree');
            return;
          }

          this.replaceStyle(response.style);
          this.toast.showToast(response.message ?? 'Skill tree reset.', '', true);
        },
        error: (error) =>
          this._error.set(error.message ?? 'Failed to reset skill tree'),
      });
  }

  clearMessages(): void {
    this._error.set(null);
  }

  private replaceStyle(style: CombatStyleDto): void {
    const overview = this._overview();
    if (!overview) return;

    this._overview.set({
      ...overview,
      activeStyleId: style.isActive ? style.id : overview.activeStyleId,
      styles: overview.styles.map((existing) =>
        existing.id === style.id ? style : existing,
      ),
    });
  }
}
