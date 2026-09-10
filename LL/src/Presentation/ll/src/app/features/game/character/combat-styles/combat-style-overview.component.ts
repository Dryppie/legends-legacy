import { DecimalPipe } from '@angular/common';
import { Component, computed, effect, inject, untracked } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';

@Component({
  selector: 'app-combat-style-overview',
  imports: [RouterLink, DecimalPipe],
  template: `
    <section class="mt-4 rounded border border-light_gray bg-texture p-4">
      <div class="flex items-center justify-between gap-2">
        <h2 class="text-primary">Combat Style</h2>
        <a
          class="text-xs text-primary underline"
          routerLink="/game/character/combat-styles"
          >Manage Combat Styles</a
        >
      </div>
      @if (selected(); as entry) {
        <p class="mt-2 text-sm text-white">
          {{ entry.definition.name }} · Mastery {{ entry.level }}
        </p>
        <p class="mt-1 text-xs text-zinc-400">
          {{
            entry.level >= 10
              ? 'Mastered'
              : (entry.currentXp | number) +
                ' / ' +
                (entry.xpRequired | number) +
                ' XP'
          }}
          · {{ state.data()?.selection?.refinementId ?? 'Base form' }}
        </p>
      } @else if (state.data()) {
        <p class="mt-2 text-sm text-zinc-300">No Combat Style selected.</p>
        <p class="mt-1 text-xs text-zinc-400">
          Bastion, Conduit and Reaper are available immediately at Mastery 0.
        </p>
      } @else {
        <p class="mt-2 text-xs text-zinc-400">
          {{ state.error() ?? 'Loading Combat Style…' }}
        </p>
      }
    </section>
  `,
})
export class CombatStyleOverviewComponent {
  readonly state = inject(CombatStyleStateService);
  readonly selected = computed(
    () =>
      this.state
        .data()
        ?.styles.find(
          (entry) =>
            entry.definition.id === this.state.data()?.selection.combatStyleId,
        ) ?? null,
  );
  constructor() {
    effect(() => {
      if (this.state.dirty()) untracked(() => this.state.refreshIfDirty());
    });
  }
}
