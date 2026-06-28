import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, signal } from '@angular/core';
import { DefaultHeaderComponent } from '../../../../shared/components/default-header/default-header.component';
import { CombatStyleStateService } from '../../../../core/services/api/combat-styles/combat-style-state.service';
import {
  CombatStyleDto,
  CombatStyleFocusDto,
  CombatStyleSkillTreeBranchDto,
  CombatStyleSkillTreeNodeDto,
} from '../../../../shared/models/combat-style';

@Component({
  selector: 'app-combat-styles',
  standalone: true,
  imports: [CommonModule, DefaultHeaderComponent],
  templateUrl: './combat-styles.component.html',
  styleUrls: ['./combat-styles.component.scss'],
})
export class CombatStylesComponent implements OnInit {
  private readonly selectedStyleId = signal<string | null>(null);
  private readonly selectedNodeId = signal<string | null>(null);

  readonly selectedStyle = computed<CombatStyleDto | null>(() => {
    const styles = this.combatStyles.styles();
    if (styles.length === 0) return null;

    const selectedId = this.selectedStyleId();
    return (
      styles.find((style) => style.id === selectedId) ??
      styles.find((style) => style.isActive) ??
      styles[0]
    );
  });

  readonly selectedNode = computed<CombatStyleSkillTreeNodeDto | null>(() => {
    const style = this.selectedStyle();
    if (!style) return null;

    const nodes = style.skillTree.branches.flatMap((branch) => branch.nodes);
    const selectedId = this.selectedNodeId();
    return (
      nodes.find((node) => node.id === selectedId) ??
      nodes.find((node) => node.rank > 0) ??
      nodes[0] ??
      null
    );
  });

  constructor(public readonly combatStyles: CombatStyleStateService) {}

  ngOnInit(): void {
    this.combatStyles.refresh();
  }

  xpProgress(style: CombatStyleDto): number {
    const span = style.experienceForNextLevel - style.experienceForCurrentLevel;
    if (span <= 0) return 100;

    return Math.max(
      0,
      Math.min(
        100,
        ((style.experience - style.experienceForCurrentLevel) / span) * 100,
      ),
    );
  }

  focusLabel(style: CombatStyleDto): string {
    return (
      style.focuses.find((focus) => focus.isSelected)?.name ??
      'No Focus selected'
    );
  }

  canSelectFocus(focus: CombatStyleFocusDto): boolean {
    return focus.isUnlocked && !focus.isSelected && !this.combatStyles.saving();
  }

  selectStyle(styleId: string): void {
    this.selectedStyleId.set(styleId);
    this.selectedNodeId.set(null);
    this.combatStyles.clearMessages();
  }

  selectNode(nodeId: string): void {
    this.selectedNodeId.set(nodeId);
  }

  isSelectedStyle(style: CombatStyleDto): boolean {
    return this.selectedStyle()?.id === style.id;
  }

  isSelectedNode(node: CombatStyleSkillTreeNodeDto): boolean {
    return this.selectedNode()?.id === node.id;
  }

  styleInitial(style: CombatStyleDto): string {
    return style.name.slice(0, 1).toUpperCase();
  }

  nodeGridColumn(node: CombatStyleSkillTreeNodeDto): string {
    return `${node.x + 2}`;
  }

  nodeGridRow(node: CombatStyleSkillTreeNodeDto): string {
    return `${node.y + 1}`;
  }

  treeNodes(style: CombatStyleDto): CombatStyleSkillTreeNodeDto[] {
    return style.skillTree.branches
      .flatMap((branch) => branch.nodes)
      .sort((left, right) => {
        const rowCompare = left.row - right.row;
        if (rowCompare !== 0) return rowCompare;

        const typeCompare =
          this.nodeTypeOrder(left.nodeType) - this.nodeTypeOrder(right.nodeType);
        if (typeCompare !== 0) return typeCompare;

        const yCompare = left.y - right.y;
        if (yCompare !== 0) return yCompare;

        return this.laneOrder(left.lane) - this.laneOrder(right.lane);
      });
  }

  usesTreeMap(style: CombatStyleDto): boolean {
    return this.treeNodes(style).some((node) => node.row > 0 && !!node.lane);
  }

  treeMapColumn(node: CombatStyleSkillTreeNodeDto): string {
    const lane = node.lane?.toLowerCase();
    const baseColumn =
      lane === 'left' ? 2 : lane === 'right' ? 6 : lane === 'middle' ? 4 : 4;

    if (node.nodeType?.toLowerCase() === 'major') {
      return `${baseColumn}`;
    }

    const rowStartY = Math.max(0, (node.row - 1) * 3);
    const rowOffset = node.y - rowStartY;

    if (lane === 'left') return '1';
    if (lane === 'right') return '7';
    if (rowOffset >= 2) return '5';

    return `${baseColumn}`;
  }

  treeMapRow(node: CombatStyleSkillTreeNodeDto): string {
    if (node.row <= 0) return this.nodeGridRow(node);

    const rowBase = (node.row - 1) * 4 + 1;
    if (node.nodeType?.toLowerCase() === 'major') {
      return `${rowBase}`;
    }

    const rowStartY = Math.max(0, (node.row - 1) * 3);
    const rowOffset = Math.max(1, node.y - rowStartY);
    return `${rowBase + Math.min(rowOffset, 3)}`;
  }

  nodeClasses(node: CombatStyleSkillTreeNodeDto): Record<string, boolean> {
    const nodeType = node.nodeType?.toLowerCase();

    return {
      'tree-node--ranked': node.rank > 0,
      'tree-node--available': node.canRankUp,
      'tree-node--locked': !node.isUnlocked,
      'tree-node--selected': this.isSelectedNode(node),
      'tree-node--maxed': node.rank >= node.maxRank,
      'tree-node--capstone': nodeType === 'capstone' || (node.row <= 0 && node.y >= 3),
      'tree-node--major': nodeType === 'major',
      'tree-node--minor': nodeType === 'minor',
    };
  }

  private laneOrder(lane: string): number {
    switch (lane?.toLowerCase()) {
      case 'left':
        return 0;
      case 'middle':
        return 1;
      case 'right':
        return 2;
      default:
        return 3;
    }
  }

  private nodeTypeOrder(nodeType: string): number {
    switch (nodeType?.toLowerCase()) {
      case 'major':
        return 0;
      case 'minor':
        return 1;
      case 'capstone':
        return 2;
      default:
        return 3;
    }
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
}
