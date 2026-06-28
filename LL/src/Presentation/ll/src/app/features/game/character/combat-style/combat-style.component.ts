import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { RegularButtonComponent } from '../../../../shared/components/custom-components/buttons/regular-button/regular-button.component';
import {
  CombatStyleDto,
  CombatStyleRuleSummaryDto,
  CombatStyleSkillTreeNodeDto,
} from '../../../../shared/models/combat-style';

type CombatTreeLane = 'Left' | 'Middle' | 'Right';

@Component({
  selector: 'app-combat-style',
  standalone: true,
  imports: [CommonModule, DefaultHeaderComponent, RegularButtonComponent],
  templateUrl: './combat-style.component.html',
  styleUrl: './combat-style.component.css',
})
export class CombatStyleComponent implements OnInit {
  readonly treeLanes: CombatTreeLane[] = ['Left', 'Middle', 'Right'];
  private readonly nextRowLaneUnlocks: Record<CombatTreeLane, CombatTreeLane[]> =
    {
      Left: ['Left', 'Middle'],
      Middle: ['Left', 'Middle', 'Right'],
      Right: ['Middle', 'Right'],
    };

  readonly selectedStyleId = signal<string | null>(null);
  readonly selectedNodeId = signal<string | null>(null);

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

  readonly selectedNode = computed<CombatStyleSkillTreeNodeDto | null>(() => {
    const style = this.selectedStyle();
    if (!style) return null;

    const nodes = style.skillTree.branches.flatMap((branch) => branch.nodes);
    const selectedId = this.selectedNodeId();

    return (
      nodes.find((node) => node.id === selectedId) ??
      nodes.find((node) => node.canRankUp) ??
      nodes[0] ??
      null
    );
  });

  constructor(public readonly combatStyleState: CombatStyleStateService) {}

  ngOnInit(): void {
    this.combatStyleState.refresh();
  }

  selectStyle(style: CombatStyleDto): void {
    this.selectedStyleId.set(style.id);
    this.selectedNodeId.set(null);
  }

  selectNode(node: CombatStyleSkillTreeNodeDto): void {
    this.selectedNodeId.set(node.id);
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

  treeRows(style: CombatStyleDto): number[] {
    return [
      ...new Set(
        this.treeNodes(style)
          .filter((node) => node.nodeType === 'Major')
          .map((node) => node.row),
      ),
    ].sort((left, right) => left - right);
  }

  laneNodes(
    style: CombatStyleDto,
    row: number,
    lane: CombatTreeLane,
  ): CombatStyleSkillTreeNodeDto[] {
    const nodes = this.treeNodes(style)
      .filter((node) => node.row === row && node.lane === lane)
      .sort((left, right) => left.y - right.y || left.name.localeCompare(right.name));

    const major = nodes.find((node) => node.nodeType === 'Major');
    if (!major) {
      return nodes;
    }

    const minors = nodes.filter((node) => node.nodeType !== 'Major');
    if (lane === 'Left') {
      return [...minors, major];
    }

    if (minors.length < 2) {
      return [major, ...minors];
    }

    return [minors[0], major, ...minors.slice(1)];
  }

  lanePointsSpent(style: CombatStyleDto, lane: CombatTreeLane): number {
    return this.treeNodes(style)
      .filter((node) => node.lane === lane)
      .reduce((total, node) => total + node.rank, 0);
  }

  laneDescription(style: CombatStyleDto, lane: CombatTreeLane): string {
    const branch = style.skillTree.branches.find(
      (item) => item.name === lane || item.id === lane.toLowerCase(),
    );

    return branch?.description || `${lane} lane.`;
  }

  laneCellClass(lane: CombatTreeLane): string {
    return `combat-tree-cell-${lane.toLowerCase()}`;
  }

  corePassiveName(style: CombatStyleDto): string {
    return this.titleCase(style.resourceId);
  }

  coreActiveName(style: CombatStyleDto): string {
    if (style.id === 'defensive') {
      return 'Aegis';
    }

    return 'Style Equipped';
  }

  coreActiveDescription(style: CombatStyleDto): string {
    if (style.id === 'defensive') {
      return 'Granted automatically while Defensive is equipped; spends Guard for Barrier, control resistance, and interruption protection.';
    }

    return 'Granted automatically while the style is equipped; unlocks Row 1 major choices and row minor passives.';
  }

  rowTitle(style: CombatStyleDto, row: number): string {
    if (style.id === 'defensive') {
      switch (row) {
        case 1:
          return 'Foundation';
        case 2:
          return 'Defensive Expression';
        case 3:
          return 'Aegis Upgrade';
      }
    }

    switch (row) {
      case 1:
        return 'Foundation';
      case 2:
        return 'Conversion';
      case 3:
        return 'Active Upgrade';
      default:
        return `Row ${row}`;
    }
  }

  rowDescription(style: CombatStyleDto, row: number): string {
    if (style.id === 'defensive') {
      switch (row) {
        case 1:
          return 'Choose the Guard foundation that opens your path.';
        case 2:
          return 'Choose how Guard and Aegis interact with Essence combat.';
        case 3:
          return 'Choose one mutually exclusive Aegis identity.';
      }
    }

    switch (row) {
      case 1:
        return 'Choose a foundation major node.';
      case 2:
        return 'Choose an Essence, equipment, or combat-rule mutator.';
      case 3:
        return 'Choose the build identity and active upgrade.';
      default:
        return 'Choose one major node for this row.';
    }
  }

  nodeLockReasons(
    style: CombatStyleDto,
    node: CombatStyleSkillTreeNodeDto,
  ): string[] {
    const reasons: string[] = [];

    if (style.level < node.requiredLevel) {
      reasons.push(`Requires ${style.name} Style Level ${node.requiredLevel}.`);
    }

    if (node.row > 1) {
      const previousMajor = this.selectedMajorForRow(style, node.row - 1);

      if (!previousMajor) {
        reasons.push(`Requires a Row ${node.row - 1} major choice.`);
      } else if (node.nodeType === 'Major') {
        const allowedLanes = this.allowedNextLanes(previousMajor.lane);
        if (!allowedLanes.includes(node.lane as CombatTreeLane)) {
          reasons.push(
            `${previousMajor.name} unlocks Row ${node.row} ${this.formatLaneList(allowedLanes)} only.`,
          );
        }
      }
    }

    if (node.nodeType === 'Major' && node.rank <= 0) {
      const selectedMajor = this.selectedMajorForRow(style, node.row);
      if (selectedMajor && selectedMajor.id !== node.id) {
        reasons.push(`Row ${node.row} already has ${selectedMajor.name} selected.`);
      }
    }

    if (
      node.isUnlocked &&
      node.rank < node.maxRank &&
      !node.canRankUp &&
      style.skillPointsAvailable <= 0
    ) {
      reasons.push('No skill points available.');
    }

    return reasons;
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

  nodeIcon(node: CombatStyleSkillTreeNodeDto): string {
    if (node.nodeType === 'Major') {
      return node.name.slice(0, 1);
    }

    return '+';
  }

  nodeRankLabel(node: CombatStyleSkillTreeNodeDto): string {
    return `${node.rank}/${node.maxRank}`;
  }

  nodeStatusLabel(node: CombatStyleSkillTreeNodeDto): string {
    if (node.rank >= node.maxRank) return 'Maxed';
    if (node.canRankUp) return 'Ready';
    if (node.isUnlocked) return 'Unlocked';
    return `Level ${node.requiredLevel}`;
  }

  isSelectedNode(node: CombatStyleSkillTreeNodeDto): boolean {
    return this.selectedNode()?.id === node.id;
  }

  trackStyle(_: number, style: CombatStyleDto): string {
    return style.id;
  }

  trackNode(_: number, node: CombatStyleSkillTreeNodeDto): string {
    return node.id;
  }

  trackRow(_: number, row: number): number {
    return row;
  }

  trackRule(_: number, rule: CombatStyleRuleSummaryDto): string {
    return rule.id;
  }

  trackText(_: number, value: string): string {
    return value;
  }

  private treeNodes(style: CombatStyleDto): CombatStyleSkillTreeNodeDto[] {
    return style.skillTree.branches.flatMap((branch) => branch.nodes);
  }

  private selectedMajorForRow(
    style: CombatStyleDto,
    row: number,
  ): CombatStyleSkillTreeNodeDto | null {
    return (
      this.treeNodes(style).find(
        (node) => node.nodeType === 'Major' && node.row === row && node.rank > 0,
      ) ?? null
    );
  }

  private allowedNextLanes(lane: string): CombatTreeLane[] {
    return this.nextRowLaneUnlocks[lane as CombatTreeLane] ?? [];
  }

  private formatLaneList(lanes: CombatTreeLane[]): string {
    if (lanes.length <= 1) {
      return lanes[0] ?? 'no lanes';
    }

    if (lanes.length === 2) {
      return `${lanes[0]} or ${lanes[1]}`;
    }

    return `${lanes.slice(0, -1).join(', ')}, or ${lanes[lanes.length - 1]}`;
  }

  private laneIndex(lane: string): number {
    const index = this.treeLanes.indexOf(lane as CombatTreeLane);
    return index === -1 ? this.treeLanes.length : index;
  }

  private titleCase(value: string): string {
    return value
      .replace(/[_-]+/g, ' ')
      .replace(/\b\w/g, (letter) => letter.toUpperCase());
  }
}
