import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  CombatStyleDto,
  CombatStyleFocusDto,
  CombatStyleRuleSummaryDto,
  CombatStyleSkillTreeBranchDto,
  CombatStyleSkillTreeNodeDto,
} from '../../../../shared/models/combat-style';

@Component({
  selector: 'app-combat-style',
  standalone: true,
  imports: [CommonModule, DefaultHeaderComponent, RegularButtonComponent],
  templateUrl: './combat-style.component.html',
})
export class CombatStyleComponent implements OnInit {
  readonly selectedStyleId = signal<string | null>(null);

  readonly selectedStyle = computed<CombatStyleDto | null>(() => {
    const styles = this.combatStyleState.styles();
    const selectedId = this.selectedStyleId();

    return (
      styles.find((style) => style.id === selectedId) ??
      this.combatStyleState.activeStyle() ??
      styles[0] ??
      null
    );
  });

  constructor(public readonly combatStyleState: CombatStyleStateService) {}

  ngOnInit(): void {
    this.combatStyleState.refresh();
  }

  selectStyle(style: CombatStyleDto): void {
    this.selectedStyleId.set(style.id);
  }

  experiencePercent(style: CombatStyleDto): number {
    const totalForLevel =
      style.experienceForNextLevel - style.experienceForCurrentLevel;

    if (totalForLevel <= 0) {
      return 100;
    }

    const gainedThisLevel = style.experience - style.experienceForCurrentLevel;
    return Math.max(0, Math.min(100, (gainedThisLevel / totalForLevel) * 100));
  }

  activateStyle(style: CombatStyleDto): void {
    if (!style.isActive) {
      this.combatStyleState.activateStyle(style.id);
    }
  }

  selectFocus(style: CombatStyleDto, focus: CombatStyleFocusDto): void {
    if (focus.isUnlocked && !focus.isSelected) {
      this.combatStyleState.selectFocus(style.id, focus.id);
    }
  }

  rankUpNode(style: CombatStyleDto, node: CombatStyleSkillTreeNodeDto): void {
    if (node.canRankUp) {
      this.combatStyleState.rankUpNode(style.id, node.id);
    }
  }

  resetTree(style: CombatStyleDto): void {
    if (style.skillPointsSpent > 0) {
      this.combatStyleState.resetTree(style.id);
    }
  }

  branchDescription(branch: CombatStyleSkillTreeBranchDto): string {
    return branch.description || `${branch.name} path`;
  }

  focusDescription(focus: CombatStyleFocusDto): string {
    return focus.description || `Unlocks at level ${focus.unlockLevel}.`;
  }

  branchTags(branch: CombatStyleSkillTreeBranchDto): string[] {
    return branch.recommendedTags ?? [];
  }

  branchStats(branch: CombatStyleSkillTreeBranchDto): string[] {
    return branch.recommendedStats ?? [];
  }

  nodeTags(node: CombatStyleSkillTreeNodeDto): string[] {
    return node.tags ?? [];
  }

  nodeEffects(node: CombatStyleSkillTreeNodeDto): string[] {
    return node.effects ?? [];
  }

  nodeTooltipChanges(node: CombatStyleSkillTreeNodeDto): string[] {
    return node.tooltip?.changes ?? [];
  }

  nodeTooltipAffects(node: CombatStyleSkillTreeNodeDto): string[] {
    return node.tooltip?.affects ?? [];
  }

  hasNodeDetails(node: CombatStyleSkillTreeNodeDto): boolean {
    return (
      !!node.description ||
      this.nodeTags(node).length > 0 ||
      this.nodeEffects(node).length > 0 ||
      this.nodeTooltipChanges(node).length > 0 ||
      this.nodeTooltipAffects(node).length > 0
    );
  }

  trackStyle(_: number, style: CombatStyleDto): string {
    return style.id;
  }

  trackFocus(_: number, focus: CombatStyleFocusDto): string {
    return focus.id;
  }

  trackBranch(_: number, branch: CombatStyleSkillTreeBranchDto): string {
    return branch.id;
  }

  trackNode(_: number, node: CombatStyleSkillTreeNodeDto): string {
    return node.id;
  }

  trackRule(_: number, rule: CombatStyleRuleSummaryDto): string {
    return rule.id;
  }

  trackText(_: number, value: string): string {
    return value;
  }
}
