import { DecimalPipe, formatNumber, NgTemplateOutlet } from '@angular/common';
import {
  Component,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import {
  NavigationTab,
  NavigationTabsComponent,
} from '../../../../shared/components/custom-components/tabs/navigation-tabs/navigation-tabs.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import { EssenceDescriptionComponent } from '../../../../shared/components/essences/essence-description/essence-description.component';

@Component({
  selector: 'app-combat-styles',
  host: { class: 'block h-full min-h-0' },
  imports: [
    DecimalPipe,
    NgTemplateOutlet,
    DefaultHeaderComponent,
    NavigationTabsComponent,
    RegularButtonComponent,
    EssenceDescriptionComponent,
  ],
  templateUrl: './combat-styles.component.html',
  styleUrl: './combat-styles.component.css',
})
export class CombatStylesComponent {
  readonly state = inject(CombatStyleStateService);
  readonly refinementChoice = signal<string | null>(null);
  readonly equippedStyleName = computed(() => {
    const data = this.state.data();
    if (!data) return null;
    const id = data.selection.combatStyleId;
    if (!id) return 'None';
    return (
      data.styles.find((entry) => entry.definition.id === id)?.definition
        .name ?? id
    );
  });
  readonly previewOnly = computed(
    () =>
      !!this.state.data() &&
      this.state.draft().combatStyleId !==
        this.state.data()?.selection.combatStyleId,
  );
  readonly battleActionLabel = computed(() => {
    if (this.state.busy()) return 'Saving…';
    return `Take ${this.state.selected()?.definition.name} into battle`;
  });

  openRefinements(dialog: HTMLDialogElement) {
    if (this.state.busy() || (this.state.selected()?.level ?? 0) < 3) return;
    this.refinementChoice.set(this.state.draft().refinementId);
    dialog.showModal();
  }

  chooseRefinement(id: string | null, mobile: boolean) {
    if (this.state.busy() || (this.state.selected()?.level ?? 0) < 3) return;
    if (mobile) this.refinementChoice.set(id);
    else this.state.changeRefinement(id);
  }

  applyRefinement(dialog: HTMLDialogElement) {
    if (this.state.busy() || (this.state.selected()?.level ?? 0) < 3) return;
    this.state.changeRefinement(this.refinementChoice());
    dialog.close();
  }

  clearUpgrades() {
    if (this.state.busy()) return;
    for (const id of this.state.draft().upgradeIds)
      this.state.toggleUpgrade(id);
  }
  readonly styleTabs = computed<NavigationTab[]>(() => {
    const disabled =
      this.state.busy() || this.state.loading() || !this.state.data();
    return [
      ...(this.state.data()?.styles ?? []).map((entry) => ({
        key: entry.definition.id,
        label: entry.definition.name,
        disabled,
      })),
    ];
  });
  readonly upgradeSlots = [5, 8];
  readonly milestones = [
    { level: 0, label: 'Full mechanic', kind: 'unlock' },
    { level: 1, label: 'Mastery bonus', kind: 'bonus' },
    { level: 2, label: 'Mastery bonus', kind: 'bonus' },
    { level: 3, label: 'Mastery bonus and refinements', kind: 'unlock' },
    { level: 4, label: 'Mastery bonus', kind: 'bonus' },
    { level: 5, label: 'Mastery bonus and upgrade slot 1', kind: 'unlock' },
    { level: 6, label: 'Mastery bonus', kind: 'bonus' },
    { level: 7, label: 'Mastery bonus and Opening Technique', kind: 'unlock' },
    { level: 8, label: 'Mastery bonus and upgrade slot 2', kind: 'unlock' },
    { level: 9, label: 'Mastery bonus and Upgrade Mastery', kind: 'unlock' },
    { level: 10, label: 'Maximum mastery bonus and level cap', kind: 'unlock' },
  ];
  readonly mechanicName = computed(() =>
    this.state.selected()?.definition.id === 'bastion'
      ? 'Fortification'
      : this.state.selected()?.definition.id === 'reaper' ? 'Harvest'
        : this.state.selected()?.definition.id === 'duelist' ? 'Read the Opponent' : 'Circuit',
  );
  readonly mechanicDescription = computed(() => {
    const entry = this.state.selected();
    if (!entry) return '';
    const tuning = entry.definition.kind === 'Reaper'
      ? entry.definition.tuning?.reaper
      : entry.definition.kind === 'Duelist'
        ? entry.definition.tuning?.duelist
        : null;
    if (!tuning) return entry.definition.description;
    const baseMultiplier = 'baseMultiplier' in tuning
      ? tuning.baseMultiplier
      : tuning.openingMultiplier;
    const level = Math.max(0, Math.min(10, entry.level));
    const percentage = formatNumber(
      100 * (baseMultiplier + level * tuning.perMasteryLevel),
      'en-US',
      '1.0-2',
    );
    return entry.definition.description.replace(/\d+(?:\.\d+)?%/, `${percentage}%`);
  });
  readonly previewFacts = computed(
    () => this.state.preview()?.previewFacts ?? [],
  );
  readonly selectedRefinement = computed(() =>
    this.state
      .selected()
      ?.definition.refinements.find(
        (refinement) => refinement.id === this.state.draft().refinementId,
      ),
  );
  readonly masteryBenefit = computed(() => {
    const entry = this.state.selected();
    const tuning =
      this.selectedRefinement()?.tuning ?? entry?.definition.tuning;
    if (!entry || !tuning) return null;
    const level = Math.max(0, Math.min(10, entry.level));
    const nextLevel = level + 1;
    const number = (value: number) => formatNumber(value, 'en-US', '1.0-2');
    if (entry.definition.kind === 'Duelist' && tuning.duelist) {
      const duelist = tuning.duelist;
      const multiplier = (rank: number) => number(100 * (duelist.openingMultiplier + rank * duelist.perMasteryLevel));
      return {
        perLevel: `Each mastery level adds a flat +${number(duelist.perMasteryLevel * 100)}% to Opening damage.`,
        bonus: `+${number(level * duelist.perMasteryLevel * 100)}% flat increase`,
        current: `Mastery ${level}: ${multiplier(level)}% Opening damage`,
        example: `At ${duelist.readRequired} Read, your next damaging Essence spends an Opening. A direct hit normally worth 100 deals ${multiplier(level)} to that opponent before upgrades and defenses. Other attacks keep their normal damage.`,
        next: level < 10 ? `Level ${nextLevel}: ${multiplier(nextLevel)}% Opening damage` : null,
      };
    }
    if (entry.definition.kind === 'Reaper' && tuning.reaper) {
      const reaper = tuning.reaper;
      const refinementId = this.state.draft().refinementId;
      const formBonus = refinementId === 'death-sentence' ? (reaper.deathSentenceBonus ?? 0) : 0;
      const multiplier = (rank: number) => number(100 * (reaper.baseMultiplier + rank * reaper.perMasteryLevel + formBonus));
      const payout = refinementId === 'death-sentence'
        ? `store ${multiplier(level)} Magical Damage as Doom, dealt after 15 seconds`
        : refinementId === 'soul-siphon'
          ? `restore up to ${multiplier(level)} Health`
          : `deal ${multiplier(level)} damage now`;
      return {
        perLevel: `Each mastery level adds a flat +${number(reaper.perMasteryLevel * 100)}% to Harvest.`,
        bonus: `+${number(level * reaper.perMasteryLevel * 100)}% flat increase`,
        current: `Mastery ${level}: ${multiplier(level)}% Harvest`,
        example: `Harvest 100 damage from your conditions to ${payout}. Upgrades, bonuses, defenses and missing Health can change the final amount.`,
        next: level < 10 ? `Level ${nextLevel}: ${multiplier(nextLevel)}% Harvest` : null,
      };
    }
    if (entry.definition.kind === 'Bastion') {
      const perLevel = tuning.barrierPerMasteryLevel * 100;
      const baseBarrier = 200 * tuning.barrierFraction;
      return {
        perLevel: `+${number(perLevel)}% of base converted Barrier per mastery level.`,
        bonus: `+${number(perLevel * level)}% Barrier`,
        current: `Mastery ${level}: +${number(perLevel * level)}% converted Barrier`,
        example: `200 healing received: ${number(baseBarrier)} → ${number(baseBarrier * (1 + level * tuning.barrierPerMasteryLevel))} Barrier before upgrades, sharing and caps.`,
        next:
          level < 10
            ? `Level ${nextLevel}: +${number(perLevel * nextLevel)}% converted Barrier`
            : null,
      };
    }
    const perLevel = tuning.channeledPerMasteryLevel * 100;
    const baseEffect =
      (tuning.channeledBaseMultiplier + tuning.channeledPerCharge) * 100;
    return {
      perLevel: `Each mastery level adds a flat +${number(perLevel)}% to your Channeled Essence's damage, healing and Barrier when it spends at least 1 Charge.`,
      bonus: `+${number(perLevel * level)}% flat increase`,
      current: `Mastery ${level}: +${number(perLevel * level)}% flat increase to your Channeled Essence`,
      example: `1 Charge: ${number(baseEffect)}% → ${number(baseEffect + perLevel * level)}% of normal strength before upgrades. The mastery bonus applies once when your Channeled Essence spends at least 1 Charge.`,
      next:
        level < 10
          ? `Level ${nextLevel}: +${number(perLevel * nextLevel)}% flat increase to your Channeled Essence`
          : null,
    };
  });
  readonly masteredUpgrade = computed(() =>
    this.state
      .selected()
      ?.definition.upgrades.find(
        (upgrade) => upgrade.id === this.state.draft().masteredUpgradeId,
      ),
  );

  upgradeForSlot(index: number) {
    const id = this.state.draft().upgradeIds[index];
    return this.state
      .selected()
      ?.definition.upgrades.find((upgrade) => upgrade.id === id);
  }

  selectStyleTab(key: string) {
    if (!this.styleTabs().some((tab) => tab.key === key && !tab.disabled))
      return;
    this.state.chooseStyle(key);
  }

  refinementStatus(id: string | null): string {
    const saved = this.state.data()?.selection;
    return saved?.combatStyleId === this.state.draft().combatStyleId &&
      saved?.refinementId === id
      ? 'Equipped'
      : 'Selected';
  }

  constructor() {
    effect(() => {
      if (this.state.dirty()) untracked(() => this.state.refreshIfDirty());
    });
  }
}
